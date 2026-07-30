using GameChatParser.Core.Chat;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Games;

/// <summary>
/// A game whose results are shared in the chat. Adding support for another game means
/// implementing this once and registering it in <see cref="GameRegistry"/>; everything
/// downstream, the grouping, the ranking and the rendering, is game agnostic.
/// </summary>
public interface IGame
{
    /// <summary>The game's name, used as the heading on every leaderboard it produces.</summary>
    string Name { get; }

    /// <summary>Whether a higher or a lower average score is the better result.</summary>
    RankingDirection RankingDirection { get; }

    /// <summary>
    /// Extracts this game's result from a message, or returns <c>null</c> where the
    /// message holds no result for this game. Every game is offered every message, so
    /// an implementation must recognise only its own format.
    /// </summary>
    GameScore? TryParseScore(ChatMessage message);

    /// <summary>
    /// Replaces this game's scores with the ones it should be ranked on, which by default
    /// are the ones it parsed. A game whose result only means something next to the rest
    /// of that puzzle's field cannot work its score out while reading a single message,
    /// and this is where it gets to; it may rewrite scores and drop them.
    /// </summary>
    /// <remarks>
    /// Called once per game, after the repeats have been dropped and before anything is
    /// grouped into periods. Both halves of that matter: a player who posted the same
    /// result twice would otherwise appear twice in a puzzle's field, and a game rewritten
    /// period by period would give a player one figure in the yearly table and another in
    /// the monthly one.
    /// </remarks>
    IReadOnlyList<GameScore> Normalise(IReadOnlyList<GameScore> scores) => scores;

    /// <summary>
    /// Reduces one player's scores for one period to the single figure they are ranked and
    /// printed on, which by default is their mean. Ranking by mean rather than by total is
    /// what keeps somebody who misses a fortnight from being punished for it.
    /// </summary>
    double Summarise(IReadOnlyList<GameScore> scores) => scores.Average(score => score.Value);
}
