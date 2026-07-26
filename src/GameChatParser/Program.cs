using System.Text;
using GameChatParser.Core.Reporting;

if (args is not [{ Length: > 0 } filePath])
{
    Console.Error.WriteLine("Usage: GameChatParser <path-to-chat-export>");
    return 1;
}

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"Error: The file '{filePath}' does not exist.");
    return 1;
}

// Chat exports are full of emoji, and the grids are unreadable without them.
Console.OutputEncoding = Encoding.UTF8;

var report = new ReportBuilder().Build(File.ReadLines(filePath));

new MarkdownReportRenderer().Render(report, Console.Out);

return 0;
