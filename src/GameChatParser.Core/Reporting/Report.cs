namespace GameChatParser.Core.Reporting;

/// <summary>Every leaderboard produced from one chat export, in the order they are shown.</summary>
public sealed record Report
{
    public required IReadOnlyList<Leaderboard> Leaderboards { get; init; }
}
