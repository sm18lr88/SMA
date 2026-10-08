// Runs the card editing commands of SuperMemoAssistant.Themes from a console: sma-cards --help for the list.
using System.Text;
using SuperMemoAssistant.Themes;

Console.OutputEncoding = new UTF8Encoding(false);

return CardCommandLine.Run(args, Console.Out, Console.Error);
