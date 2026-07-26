using System.Globalization;

namespace GameChatParser.Core.Reporting;

/// <summary>
/// Renders each leaderboard as a bold heading above a fenced block, which is what
/// WhatsApp needs to keep the columns lined up when the results are pasted back into
/// the chat.
/// </summary>
public sealed class MarkdownReportRenderer : IReportRenderer
{
    private const int MinimumPositionWidth = 3;
    private const int MinimumPlayedWidth = 4;
    private const int MinimumAverageWidth = 6;
    private const string ColumnGap = "   ";

    public void Render(Report report, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(writer);

        // Names are shortened against the whole report rather than one table at a time, so
        // a player reads the same way wherever they turn up in it.
        var displayNames = PlayerNameShortener.Shorten(report.Leaderboards
            .SelectMany(leaderboard => leaderboard.Entries)
            .Select(entry => entry.Player));

        foreach (var leaderboard in report.Leaderboards)
        {
            Render(leaderboard, writer, displayNames);
        }
    }

    public void Render(Leaderboard leaderboard, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(leaderboard);
        ArgumentNullException.ThrowIfNull(writer);

        Render(
            leaderboard,
            writer,
            PlayerNameShortener.Shorten(leaderboard.Entries.Select(entry => entry.Player)));
    }

    private static void Render(
        Leaderboard leaderboard,
        TextWriter writer,
        IReadOnlyDictionary<string, string> displayNames)
    {
        var rows = leaderboard.Entries
            .Select(entry => (
                Position: FormatPosition(entry),
                Player: DisplayName(displayNames, entry.Player),
                Played: entry.Played.ToString(CultureInfo.InvariantCulture),
                Average: entry.Average.ToString("F3", CultureInfo.InvariantCulture)))
            .ToList();

        // The numeric columns never narrow past the widths the family is used to reading,
        // while the name column takes only the room the longest name on the table needs.
        var positionWidth = ColumnWidth(rows.Select(row => row.Position), MinimumPositionWidth);
        var playerWidth = ColumnWidth(rows.Select(row => row.Player), minimum: 0, padding: 0);
        var playedWidth = ColumnWidth(rows.Select(row => row.Played), MinimumPlayedWidth, padding: 0);
        var averageWidth = ColumnWidth(rows.Select(row => row.Average), MinimumAverageWidth, padding: 0);

        writer.WriteLine($"*{leaderboard.Title}*");
        writer.WriteLine("```");

        foreach (var row in rows)
        {
            writer.WriteLine(
                row.Position.PadRight(positionWidth) +
                row.Player.PadRight(playerWidth) +
                ColumnGap +
                row.Played.PadLeft(playedWidth) +
                ColumnGap +
                row.Average.PadLeft(averageWidth));
        }

        writer.WriteLine("```");
    }

    private static string DisplayName(IReadOnlyDictionary<string, string> displayNames, string player) =>
        displayNames.TryGetValue(player, out var shortened) ? shortened : player;

    /// <summary>
    /// Renders a player's position as <c>1.</c>, or as <c>1=</c> where the position is
    /// shared with someone else.
    /// </summary>
    private static string FormatPosition(LeaderboardEntry entry) =>
        $"{entry.Position.ToString(CultureInfo.InvariantCulture)}{(entry.IsTied ? '=' : '.')}";

    private static int ColumnWidth(IEnumerable<string> values, int minimum, int padding = 1)
    {
        var widest = values.Select(value => value.Length).DefaultIfEmpty(0).Max();

        return Math.Max(minimum, widest + padding);
    }
}
