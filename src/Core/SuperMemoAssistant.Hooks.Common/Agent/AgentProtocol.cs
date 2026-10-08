// Length-prefixed binary framing for AgentMessage: [int32 length][byte tag][payload]. Reflection-free for NativeAOT.
namespace SuperMemoAssistant.Hooks.Agent
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Text;
  using SuperMemoAssistant.SuperMemo;

  public static class AgentProtocol
  {
    /// <summary>Upper bound for one frame; WriteFile payloads are the largest messages.</summary>
    public const int MaxFrameLength = 64 * 1024 * 1024;

    private enum Tag : byte
    {
      Configure = 1, Execute, Shutdown,
      Ready = 10, Configured, Result, FileCreated, FileSeeked, FileWritten, FileClosed, Log,
    }

    public static byte[] Encode(AgentMessage message)
    {
      using var buffer = new MemoryStream();
      using (var w = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
      {
        w.Write(0); // length placeholder
        WriteBody(w, message);
      }

      var frame = buffer.ToArray();
      BitConverter.TryWriteBytes(frame.AsSpan(0, 4), frame.Length - 4);
      return frame;
    }

    /// <summary>Reads one message, or returns null when the stream ended cleanly before a frame.</summary>
    public static AgentMessage? Read(Stream stream)
    {
      var header = new byte[4];
      if (!ReadExactly(stream, header, allowEof: true))
        return null;

      var length = BitConverter.ToInt32(header, 0);
      if (length <= 0 || length > MaxFrameLength)
        throw new InvalidDataException($"Invalid agent frame length {length}.");

      var body = new byte[length];
      ReadExactly(stream, body, allowEof: false);

      using var r = new BinaryReader(new MemoryStream(body), Encoding.UTF8);
      return ReadBody(r);
    }

    private static void WriteBody(BinaryWriter w, AgentMessage message)
    {
      switch (message)
      {
        case ConfigureAgent m:
          w.Write((byte)Tag.Configure);
          w.Write(m.MainThreadId);
          w.Write(m.Functions.Count);
          foreach (var f in m.Functions)
          {
            w.Write((ushort)f.Method);
            w.Write(f.Rva);
            w.Write((byte)f.ReturnKind);
          }
          w.Write(m.ElWindComponentDataOffset);
          w.Write(m.QueueSizeOffset);
          WriteStrings(w, m.WatchedFilePaths);
          break;

        case ExecuteNative m:
          w.Write((byte)Tag.Execute);
          w.Write(m.CallId);
          w.Write((ushort)m.Method);
          w.Write(m.Args.Count);
          foreach (var a in m.Args)
          {
            w.Write(a.IsText);
            if (a.IsText) w.Write(a.Text!);
            else w.Write(a.Value);
          }
          break;

        case ShutdownAgent:
          w.Write((byte)Tag.Shutdown);
          break;

        case AgentReady m:
          w.Write((byte)Tag.Ready);
          w.Write(m.ProcessId);
          w.Write(m.ModuleBase);
          break;

        case AgentConfigured m:
          w.Write((byte)Tag.Configured);
          w.Write(m.Success);
          WriteOptional(w, m.Error);
          break;

        case NativeResult m:
          w.Write((byte)Tag.Result);
          w.Write(m.CallId);
          w.Write(m.Success);
          w.Write(m.Value);
          WriteOptional(w, m.Error);
          break;

        case FileCreated m:
          w.Write((byte)Tag.FileCreated);
          w.Write(m.Path);
          w.Write(m.Handle);
          break;

        case FileSeeked m:
          w.Write((byte)Tag.FileSeeked);
          w.Write(m.Handle);
          w.Write(m.Position);
          break;

        case FileWritten m:
          w.Write((byte)Tag.FileWritten);
          w.Write(m.Handle);
          w.Write(m.Data.Length);
          w.Write(m.Data);
          break;

        case FileClosed m:
          w.Write((byte)Tag.FileClosed);
          w.Write(m.Handle);
          break;

        case AgentLog m:
          w.Write((byte)Tag.Log);
          w.Write((byte)m.Level);
          w.Write(m.Message);
          break;

        default:
          throw new ArgumentException($"Unknown agent message {message.GetType().Name}.", nameof(message));
      }
    }

    private static AgentMessage ReadBody(BinaryReader r)
    {
      var tag = (Tag)r.ReadByte();
      switch (tag)
      {
        case Tag.Configure:
          var mainThreadId = r.ReadInt32();
          var functions = new NativeFunction[r.ReadInt32()];
          for (var i = 0; i < functions.Length; i++)
            functions[i] = new NativeFunction((NativeMethod)r.ReadUInt16(), r.ReadInt64(), (NativeReturnKind)r.ReadByte());
          return new ConfigureAgent(mainThreadId, functions, r.ReadInt32(), r.ReadInt32(), ReadStrings(r));

        case Tag.Execute:
          var callId = r.ReadUInt32();
          var method = (NativeMethod)r.ReadUInt16();
          var args   = new NativeArg[r.ReadInt32()];
          for (var i = 0; i < args.Length; i++)
            args[i] = r.ReadBoolean() ? NativeArg.Of(r.ReadString()) : NativeArg.Of(r.ReadInt64());
          return new ExecuteNative(callId, method, args);

        case Tag.Shutdown:    return new ShutdownAgent();
        case Tag.Ready:       return new AgentReady(r.ReadInt32(), r.ReadInt64());
        case Tag.Configured:  return new AgentConfigured(r.ReadBoolean(), ReadOptional(r));
        case Tag.Result:      return new NativeResult(r.ReadUInt32(), r.ReadBoolean(), r.ReadInt64(), ReadOptional(r));
        case Tag.FileCreated: return new FileCreated(r.ReadString(), r.ReadInt64());
        case Tag.FileSeeked:  return new FileSeeked(r.ReadInt64(), r.ReadInt64());
        case Tag.FileWritten: return new FileWritten(r.ReadInt64(), r.ReadBytes(r.ReadInt32()));
        case Tag.FileClosed:  return new FileClosed(r.ReadInt64());
        case Tag.Log:         return new AgentLog((AgentLogLevel)r.ReadByte(), r.ReadString());
        default:              throw new InvalidDataException($"Unknown agent message tag {(byte)tag}.");
      }
    }

    private static void WriteStrings(BinaryWriter w, IReadOnlyList<string> values)
    {
      w.Write(values.Count);
      foreach (var v in values)
        w.Write(v);
    }

    private static string[] ReadStrings(BinaryReader r)
    {
      var values = new string[r.ReadInt32()];
      for (var i = 0; i < values.Length; i++)
        values[i] = r.ReadString();
      return values;
    }

    private static void WriteOptional(BinaryWriter w, string? value)
    {
      w.Write(value is not null);
      if (value is not null) w.Write(value);
    }

    private static string? ReadOptional(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;

    private static bool ReadExactly(Stream stream, byte[] buffer, bool allowEof)
    {
      var read = 0;
      while (read < buffer.Length)
      {
        var n = stream.Read(buffer, read, buffer.Length - read);
        if (n == 0)
        {
          if (allowEof && read == 0) return false;
          throw new EndOfStreamException("The agent pipe closed in the middle of a frame.");
        }
        read += n;
      }
      return true;
    }
  }
}
