using GameChatParser.Core.Chat;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Games;

/// <summary>
/// Base class for the daily puzzle games, whose shared results carry a sequence number
/// rather than a date. The number is what dates the result, so a score posted late, or
/// caught up on at the weekend, still lands in the month it was played for.
/// </summary>
public abstract class PuzzleGame : IGame
{
    public abstract string Name { get; }

    public abstract RankingDirection RankingDirection { get; }

    /// <summary>
    /// The day that puzzle number zero would have fallen on, so that puzzle number one
    /// is the day after. Games number their first puzzle 1, and some have renumbered
    /// since launch, so this is calibrated against real shared results rather than the
    /// game's advertised launch date.
    /// </summary>
    protected abstract DateOnly PuzzleZeroDate { get; }

    /// <summary>The day a given puzzle number belongs to.</summary>
    public DateOnly DateOfPuzzle(int puzzleNumber) => PuzzleZeroDate.AddDays(puzzleNumber);

    public abstract GameScore? TryParseScore(ChatMessage message);
}
