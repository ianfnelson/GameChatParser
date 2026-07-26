using GameChatParser.Core.Chat;
using GameChatParser.Core.Games;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Tests;

/// <summary>
/// A stand-in game, used both to drive the ranking tests without depending on Wordle or
/// Connections, and to prove that a new game needs nothing beyond
/// <see cref="IGame"/> to take part in a report.
/// </summary>
internal sealed class FakeGame(
    string name = "Fake",
    RankingDirection rankingDirection = RankingDirection.LowerIsBetter,
    Func<ChatMessage, GameScore?>? parser = null) : IGame
{
    public string Name { get; } = name;

    public RankingDirection RankingDirection { get; } = rankingDirection;

    public GameScore? TryParseScore(ChatMessage message) => parser?.Invoke(message);
}
