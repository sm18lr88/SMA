using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Interop.SuperMemo.Registry.Models
{
  /// <summary>
  /// Defines how to apply a template to an element.
  /// </summary>
  [Serializable]
  public enum TemplateUseMode
  {
    /// <summary>
    /// Replaces the current components with the ones from the template.
    /// </summary>
    Apply,

    /// <summary>
    /// Re-uses existing components if possible, and add those that are missing.
    /// </summary>
    Merge,

    /// <summary>
    /// Add all the components from the template, while preserving the existing ones.
    /// </summary>
    Add
  }
}
