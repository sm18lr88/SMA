# Process.NET

Process.NET is a Windows process library: processes, modules, threads, windows, and keyboard and mouse input. SMA uses it to work with the SuperMemo 20 (x64) process.

This copy is a fork of [Process.NET](https://github.com/supermemo/Process.NET) for .NET 10 and x64. Process.NET derives from [MemorySharp](https://github.com/ZenLulz/MemorySharp) by Jämes Ménétrey.

## Main types

- `ProcessSharp` (`IProcess`): opens a process and gives access to its modules, threads, and windows.
- `ModuleFactory`, `ThreadFactory`, `WindowFactory`: enumerate the modules, threads, and windows of the process.
- `KeyboardHook`, `MouseHook`: low-level keyboard and mouse input.

## Credits

- Jämes Ménétrey (ZenLulz), for MemorySharp.
- Apoc, for GreyMagic.
- aevitas, for BlueRain, on which the memory abstraction is based.
- aganonki, jeffora, miceiken, Zat, and Jadd, for their libraries and examples.
