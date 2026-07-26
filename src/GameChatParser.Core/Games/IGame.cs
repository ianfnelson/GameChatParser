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
}
