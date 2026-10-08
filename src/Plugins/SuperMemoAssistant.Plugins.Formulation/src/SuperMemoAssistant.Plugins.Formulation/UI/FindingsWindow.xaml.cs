// Non-modal window that lists the findings of a check.
namespace SuperMemoAssistant.Plugins.Formulation.UI;

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Engine;

/// <summary>Lists the findings of a formulation check, each with its explanation and a link to the rule.</summary>
public partial class FindingsWindow : Window
{
  /// <summary>Creates the window. Call it on the UI thread and show it with <see cref="Window.Show" />.</summary>
  public FindingsWindow(string summary, IReadOnlyList<FormulationFinding> findings)
  {
    Summary  = summary;
    Findings = findings;

    InitializeComponent();

    DataContext = this;
  }

  /// <summary>The summary line above the list.</summary>
  public string Summary { get; }

  /// <summary>The findings to list.</summary>
  public IReadOnlyList<FormulationFinding> Findings { get; }

  private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
  {
    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
    e.Handled = true;
  }

  private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
