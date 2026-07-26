namespace GameChatParser.Core.Reporting;

/// <summary>One player's standing on a leaderboard.</summary>
public sealed record LeaderboardEntry
{
    /// <summary>
    /// The player's position, counting from one. Positions follow standard competition
    /// ranking, so players tied for second are both second and the next player is fourth.
    /// </summary>
    public required int Position { get; init; }

    /// <summary>Whether at least one other player shares this position.</summary>
    public required bool IsTied { get; init; }

    public required string Player { get; init; }

    /// <summary>How many puzzles the player posted a result for during the period.</summary>
    public required int Played { get; init; }

    /// <summary>The mean score across those puzzles, which is what the position is based on.</summary>
    public required double Average { get; init; }
}
