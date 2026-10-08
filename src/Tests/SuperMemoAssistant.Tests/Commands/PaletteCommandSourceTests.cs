// Global hotkeys become palette commands in their process, and plugin commands reach SMA over a real named pipe.
namespace SuperMemoAssistant.Tests.Commands;

using System.Windows.Input;
using global::Extensions.System.IO;
using PluginManager.Remoting;
using SuperMemoAssistant.Interop.SMA;
using SuperMemoAssistant.Services.Configuration;
using SuperMemoAssistant.Services.IO.HotKeys;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.SMA.Commands;
using SuperMemoAssistant.Sys.IO.Devices;
using Xunit;

public sealed class PaletteCommandSourceTests : IDisposable
{
  private static readonly HotKey CtrlAltShiftK = new(Key.K, KeyModifiers.CtrlAltShift);
  private static readonly HotKey CtrlAltShiftJ = new(Key.J, KeyModifiers.CtrlAltShift);

  private readonly DirectoryInfo _configDir = Directory.CreateTempSubdirectory("sma-palette-");
  private readonly HotKeyManager _hotKeys;
  private readonly Dictionary<string, PaletteCommand> _published = [];

  public PaletteCommandSourceTests()
  {
    _hotKeys = new HotKeyManager().Initialize(new ConfigurationService(new DirectoryPath(_configDir.FullName)), new NoKeyboardHook());
  }

  public void Dispose() => _configDir.Delete(true);

  [Fact]
  public void GlobalHotKeysArePublishedWithTheirScopeAndCurrentKeys_LocalOnesAreNot()
  {
    _hotKeys.RegisterGlobal("Import", "Import a book", HotKeyScopes.SM, CtrlAltShiftK, () => { });
    _hotKeys.RegisterLocal("Extract", "Extract in the PDF window", new HotKey(Key.X, KeyModifiers.Alt));

    Publish();
    _hotKeys.RegisterGlobal("Omni", "Show OmniMemo", HotKeyScopes.Global, new HotKey(Key.F, KeyModifiers.AltShift), () => { });

    Assert.Equal(["Import", "Omni"], _published.Keys.Order());
    Assert.Equal(HotKeyScopes.SM, _published["Import"].Scopes);
    Assert.Equal(CtrlAltShiftK.ToString(), _published["Import"].HotKey);
    Assert.Equal("Books", _published["Import"].OwnerName);
  }

  [Fact]
  public void DisablingRemovesTheCommand_EnablingAndRebindingPublishItAgain()
  {
    _hotKeys.RegisterGlobal("Import", "Import a book", HotKeyScopes.SM, CtrlAltShiftK, () => { });
    Publish();

    _hotKeys.Disable("Import");
    Assert.Empty(_published);

    _hotKeys.Enable("Import");
    _hotKeys.HotKeys.Single().ActualHotKey = CtrlAltShiftJ;

    Assert.Equal(CtrlAltShiftJ.ToString(), _published["Import"].HotKey);
  }

  [Fact]
  public void AClearedHotKeyIsRegisteredAgain_WhenItGetsANewKey()
  {
    var hook    = new RecordingKeyboardHook();
    var hotKeys = new HotKeyManager().Initialize(new ConfigurationService(new DirectoryPath(_configDir.FullName)), hook);
    hotKeys.RegisterGlobal("Palette", "Show the command palette", HotKeyScopes.Global, CtrlAltShiftK, () => { });
    var palette = hotKeys.HotKeys.Single();

    palette.ActualHotKey = null;
    Assert.DoesNotContain(CtrlAltShiftK, hook.Registered);

    palette.ActualHotKey = CtrlAltShiftJ;
    Assert.Contains(CtrlAltShiftJ, hook.Registered);
    Assert.Same(palette, hotKeys.Match(CtrlAltShiftJ));
  }

  [Fact]
  public void APluginCommandRunsInThePluginProcess_AndFailsAsRemotingExceptionOnceThePluginIsGone()
  {
    var registry = new CommandRegistry();
    var pipe     = RpcEndpoint.NewPipeName();
    var server   = RpcEndpoint.Serve(pipe, new Host(registry));
    var proxy    = RpcEndpoint.Connect<IHost>(pipe, TimeSpan.FromSeconds(10));
    using var ran = new ManualResetEventSlim();

    proxy.RegisterCommand(new PaletteCommand("SuperMemoAssistant.Plugins.Books", "Books", "Import", "Import a book", "Ctrl+Alt+Shift+K",
                                             HotKeyScopes.SM),
                          ran.Set);

    var entry = registry.Snapshot().Single();
    Assert.Equal("Books", entry.Command.OwnerName);
    Assert.Equal(HotKeyScopes.SM, entry.Command.Scopes);

    entry.Execute();
    Assert.True(ran.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

    server.Dispose();
    Assert.ThrowsAny<RemotingException>(entry.Execute);
  }

  [Fact]
  public void AnSma30CoreWithoutThePalette_AnswersWithARemoteException_WhichPluginsIgnore()
  {
    var pipe = RpcEndpoint.NewPipeName();
    using var server = RpcEndpoint.Serve(pipe, new HostWithoutPalette());
    var proxy = RpcEndpoint.Connect<IHost>(pipe, TimeSpan.FromSeconds(10));

    Assert.Throws<RemoteException>(() => proxy.RegisterCommand(
      new PaletteCommand("SuperMemoAssistant.Plugins.Books", "Books", "Import", "Import a book", null, HotKeyScopes.SM), () => { }));
  }

  private void Publish() =>
    _hotKeys.PublishCommands(hk => _published[hk.Id] = PaletteCommand.FromHotKey(hk, "SuperMemoAssistant.Plugins.Books", "Books"),
                             hk => _published.Remove(hk.Id));

  public interface IHost
  {
    void RegisterCommand(PaletteCommand command, Action execute);
  }

  private sealed class Host(CommandRegistry registry) : MarshalByRefObject, IHost
  {
    public void RegisterCommand(PaletteCommand command, Action execute) => registry.Register(command, execute);
  }

  private sealed class HostWithoutPalette : MarshalByRefObject;

  private sealed class RecordingKeyboardHook : IKeyboardHookService
  {
    public HashSet<HotKey> Registered { get; } = [];

    public event EventHandler<KeyboardHookEventArgs>? KeyboardPressed { add { } remove { } }

    public Action<HotKey>? MainCallback { get; set; }

    public void RegisterHotKey(HotKey hotkey, Action callback, HotKeyScopes scope = HotKeyScopes.SM) => Registered.Add(hotkey);

    public bool UnregisterHotKey(HotKey hotkey) => Registered.Remove(hotkey);
  }

  private sealed class NoKeyboardHook : IKeyboardHookService
  {
    public event EventHandler<KeyboardHookEventArgs>? KeyboardPressed { add { } remove { } }

    public Action<HotKey>? MainCallback { get; set; }

    public void RegisterHotKey(HotKey hotkey, Action callback, HotKeyScopes scope = HotKeyScopes.SM) { }

    public bool UnregisterHotKey(HotKey hotkey) => true;
  }
}
