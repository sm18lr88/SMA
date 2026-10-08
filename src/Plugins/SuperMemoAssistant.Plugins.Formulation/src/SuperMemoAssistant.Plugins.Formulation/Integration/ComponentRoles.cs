// Tells question components from answer components by their "display at" flags.
namespace SuperMemoAssistant.Plugins.Formulation.Integration;

using Interop.SuperMemo.Content.Models;

/// <summary>The part of an item that a component shows.</summary>
public enum ComponentRole
{
  /// <summary>A component that is never shown during a repetition, for example an editing aid.</summary>
  None,

  /// <summary>A component that is shown with the question.</summary>
  Question,

  /// <summary>A component that is hidden at the question and shown with the answer.</summary>
  Answer,
}

/// <summary>Classifies components from <see cref="Interop.SuperMemo.Content.Components.IComponent.DisplayAt" />.</summary>
public static class ComponentRoles
{
  /// <summary>
  ///   Returns <see cref="ComponentRole.Question" /> when the component is shown at the question,
  ///   <see cref="ComponentRole.Answer" /> when it is shown only from the answer on, and otherwise
  ///   <see cref="ComponentRole.None" />.
  /// </summary>
  public static ComponentRole Classify(AtFlags displayAt)
  {
    if (displayAt.HasFlag(AtFlags.Question))
      return ComponentRole.Question;

    return (displayAt & (AtFlags.Answer | AtFlags.AfterGrading)) != 0
      ? ComponentRole.Answer
      : ComponentRole.None;
  }
}
