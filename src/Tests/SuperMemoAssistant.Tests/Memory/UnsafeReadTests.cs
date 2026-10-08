namespace SuperMemoAssistant.Tests.Memory;

using System.Runtime.InteropServices;
using Process.NET.Extensions;
using Xunit;

public sealed class UnsafeReadTests : IDisposable
{
  private readonly IntPtr _buffer = Marshal.AllocHGlobal(64);

  public void Dispose() => Marshal.FreeHGlobal(_buffer);

  [Fact]
  public void PrimitivesAreReadFromUnalignedAddresses()
  {
    var address = _buffer + 1;

    Marshal.WriteInt32(address, -123456);
    Assert.Equal(-123456, address.Read<int>());

    Marshal.WriteInt64(address, long.MinValue + 7);
    Assert.Equal(long.MinValue + 7, address.Read<long>());

    Marshal.Copy(BitConverter.GetBytes(3.25), 0, address, sizeof(double));
    Assert.Equal(3.25, address.Read<double>());
  }

  [Fact]
  public void EnumsUseTheirUnderlyingSize()
  {
    Marshal.WriteInt64(_buffer, -1);
    Marshal.WriteInt16(_buffer, (short)Small.Second);

    Assert.Equal(Small.Second, _buffer.Read<Small>());
  }

  [Fact]
  public void AnyNonZeroByteIsTrue()
  {
    Marshal.WriteByte(_buffer, 2);
    Assert.True(_buffer.Read<bool>());

    Marshal.WriteByte(_buffer, 0);
    Assert.False(_buffer.Read<bool>());
  }

  [Fact]
  public void StructsAreCopied()
  {
    Marshal.StructureToPtr(new Pair { A = 5, B = 9 }, _buffer, false);

    var pair = _buffer.Read<Pair>();

    Assert.Equal(5, pair.A);
    Assert.Equal(9, pair.B);
  }

  [Fact]
  public void AddressZeroIsRejected()
  {
    Assert.ThrowsAny<Exception>(() => IntPtr.Zero.Read<int>());
  }

  private enum Small : short { First = 1, Second = 513 }

  [StructLayout(LayoutKind.Sequential)]
  private struct Pair
  {
    public int A;
    public int B;
  }
}
