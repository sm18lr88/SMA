namespace SuperMemoAssistant.Plugins.LocalApi;

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Anotar.Serilog;
using Forge.Forms;
using Forge.Forms.Annotations;
using Newtonsoft.Json;
using SuperMemoAssistant.Plugins.LocalApi.Server;
using SuperMemoAssistant.Services.ToastNotifications;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Sys.ComponentModel;

/// <summary>Where a new element goes when the request does not give a parent.</summary>
public enum ParentChoice
{
  /// <summary>Under the element in the element window.</summary>
  [EnumDisplay("The current element")]
  CurrentElement,

  /// <summary>Under the collection root.</summary>
  [EnumDisplay("The collection root")]
  CollectionRoot,
}

/// <summary>The settings of the Local API plugin.</summary>
[Form(Mode = DefaultFields.None)]
[Title("Local API Settings", IsVisible = "{Env DialogHostContext}")]
[DialogAction("cancel", "Cancel", IsCancel = true)]
[DialogAction("save", "Save", IsDefault = true, Validates = true)]
public class LocalApiCfg : CfgBase<LocalApiCfg>, INotifyPropertyChangedEx
{
  private const string CopyTokenAction = "CopyLocalApiToken";
  private const string NewTokenAction  = "NewLocalApiToken";

  /// <summary>Whether the server runs. It is off by default.</summary>
  [Field(Name = "Run the local API server")]
  public bool Enabled { get; set; }

  /// <summary>The loopback port.</summary>
  [Field(Name = "Port")]
  [Value(Must.BeGreaterThanOrEqualTo, 1024, StrictValidation = true)]
  [Value(Must.BeLessThanOrEqualTo, 65535, StrictValidation = true)]
  public int Port { get; set; } = ApiOptions.DefaultPort;

  /// <summary>The parent of a new element when the request does not give one.</summary>
  [Field(Name = "Default parent of new elements")]
  [SelectFrom(typeof(ParentChoice))]
  public ParentChoice DefaultParent { get; set; } = ParentChoice.CurrentElement;

  /// <summary>The priority of a new element when the request does not give one.</summary>
  [Field(Name = "Default priority (%)")]
  [Value(Must.BeGreaterThanOrEqualTo, 0, StrictValidation = true)]
  [Value(Must.BeLessThanOrEqualTo, 100, StrictValidation = true)]
  public double DefaultPriority { get; set; } = 30;

  /// <summary>Web origins, one per line, that may call the API in addition to browser extensions.</summary>
  [Field(Name = "Extra allowed origins (one per line, for example http://localhost:3000)")]
  [MultiLine]
  public string AllowedOrigins { get; set; } = string.Empty;

  /// <summary>The bearer token. It is created when the server is first turned on.</summary>
  [Field(Name = "Access token", IsReadOnly = true)]
  [Action(CopyTokenAction, "Copy", Placement = Placement.Inline)]
  [Action(NewTokenAction, "New token", Placement = Placement.Inline)]
  public string Token { get; set; } = string.Empty;

  /// <inheritdoc />
  [JsonIgnore]
  public bool IsChanged { get; set; }

  /// <summary>The allowed origins as a list.</summary>
  public string[] AllowedOriginList() =>
    AllowedOrigins.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  /// <inheritdoc />
  public override void HandleAction(IActionContext actionContext)
  {
    switch (actionContext.Action as string)
    {
      case CopyTokenAction:
        CopyToken();
        return;

      case NewTokenAction:
        Token = TokenGenerator.Create();
        return;

      default:
        base.HandleAction(actionContext);
        return;
    }
  }

  /// <inheritdoc />
  public override string ToString() => "Local API";

  /// <summary>Copies the token to the clipboard, or says why it cannot. Call it on the UI thread: the clipboard needs STA.</summary>
  internal void CopyToken()
  {
    if (string.IsNullOrEmpty(Token))
    {
      "Local API: there is no token yet. Turn on the server and save the settings to create one.".ShowDesktopNotification();
      return;
    }

    try
    {
      Clipboard.SetText(Token);
    }
    catch (ExternalException ex)
    {
      LogTo.Warning(ex, "Local API: the token could not be copied to the clipboard");
      "Local API: the token could not be copied. Select the token and press Ctrl+C.".ShowDesktopNotification();
    }
  }

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;
}
