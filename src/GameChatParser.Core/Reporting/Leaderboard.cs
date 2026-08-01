namespace GameChatParser.Core.Reporting;

/// <summary>The standings for one game over one period.</summary>
public sealed record Leaderboard
{
    public required string GameName { get; init; }

    /// <summary>How the period reads as a heading, such as <c>2026</c> or <c>July</c>.</summary>
    public required string PeriodName { get; init; }

    public required PeriodKind PeriodKind { get; init; }

    /// <summary>
    /// How far back the period sits among those the game has results for, where zero is
    /// the most recent. This is what orders a game's tables within its own run of them.
    /// </summary>
    public required int PeriodIndex { get; init; }

    /// <summary>The players, best first.</summary>
    public required IReadOnlyList<LeaderboardEntry> Entries { get; init; }

    /// <summary>The heading for this table, such as <c>Wordle — July</c>.</summary>
    public string Title => $"{GameName} — {PeriodName}";
}
