#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion






// ReSharper disable StaticMemberInGenericType

namespace SuperMemoAssistant.Services.UI.Configuration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using Anotar.Serilog;
  using Forge.Forms;
  using Forge.Forms.Annotations;
  using Newtonsoft.Json;
  using Sys.ComponentModel;

  /// <summary>
  ///   Facilitates creating configurations by implementing common behaviour such as resetting changes on cancel
  /// </summary>
  /// <typeparam name="TCfg">The child configuration's type</typeparam>
  public abstract class CfgBase<TCfg> : IActionHandler
    where TCfg : class, new()
  {
    #region Constants & Statics

    public const string Cancel = "cancel";
    public const string Save   = "save";

    protected static bool _isInit;

    /// <summary>Copies only persisted state, polymorphic members included; collections are replaced, not appended.</summary>
    private static readonly JsonSerializerSettings CopySettings = new()
    {
      TypeNameHandling       = TypeNameHandling.Auto,
      ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    protected static HashSet<string> _saveActions   = new HashSet<string>();
    protected static HashSet<string> _cancelActions = new HashSet<string>();

    #endregion




    #region Properties & Fields - Non-Public

    protected bool UndoChangesOnCancel { get; }

    #endregion




    #region Constructors

    /// <summary>Constructor</summary>
    /// <param name="undoChangesOnCancel">
    ///   Whether using the <see cref="Cancel" /> Action resets this instance to its original
    ///   version
    /// </param>
    protected CfgBase(bool undoChangesOnCancel)
    {
      UndoChangesOnCancel = undoChangesOnCancel;

      if (_isInit == false)
        InitializeCfgBase();
    }

    /// <summary>Constructor</summary>
    protected CfgBase() : this(true) { }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public virtual void HandleAction(IActionContext actionContext)
    {
      var actionName = (string)actionContext.Action;
      var isCancel   = _cancelActions.Contains(actionName);
      var isSave     = _saveActions.Contains(actionName);

      if (isCancel && UndoChangesOnCancel)
        return;

      if (isCancel || isSave)
        ApplyChanges(actionContext.Context as TCfg);
    }

    #endregion




    #region Methods

    /// <summary>Show an option window for this instance of <typeparamref name="TCfg" /></summary>
    /// <param name="options">Optional window display options</param>
    /// <returns>Dialog result</returns>
    public Task<DialogResult<TCfg>> ShowWindowAsync(WindowOptions options = null)
    {
      options ??= new WindowOptions
      {
        CanResize = true,
      };

      return Show.Window(this, options).For<TCfg>(MapClone());
    }

    /// <summary>
    /// Creates a deep clone of this object's persisted state.
    /// </summary>
    /// <returns></returns>
    internal TCfg MapClone()
    {
      var clone = JsonConvert.DeserializeObject<TCfg>(JsonConvert.SerializeObject(this, CopySettings), CopySettings);

      if (clone is INotifyPropertyChangedEx npc)
        npc.IsChanged = false;

      return clone;
    }

    internal void ApplyChanges(TCfg original)
    {
      // ConfigurationWindow edits some configs in place (model == original); then there is nothing to copy.
      if (ReferenceEquals(this, original))
        return;

      if (original == null)
      {
        LogTo.Error("original cannot be NULL for type {FullName}", typeof(TCfg).FullName);
        throw new ArgumentNullException(nameof(original));
      }

      JsonConvert.PopulateObject(JsonConvert.SerializeObject(this, CopySettings), original, CopySettings);
    }

    private void InitializeCfgBase()
    {
      // Attributes
      var actionAttributes = typeof(TCfg).GetCustomAttributes(typeof(ActionAttribute), true)
                                         .Cast<ActionAttribute>();

      foreach (var actionAttr in actionAttributes)
        if (actionAttr.IsCancel is true)
          _cancelActions.Add(actionAttr.ActionName);

        else if (actionAttr.IsDefaultAttribute() || actionAttr.ClosesDialog is true)
          _saveActions.Add(actionAttr.ActionName);

      // Set init flag
      _isInit = true;
    }

    #endregion
  }
}
