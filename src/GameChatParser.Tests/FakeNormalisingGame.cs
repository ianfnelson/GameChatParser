using GameChatParser.Core.Chat;
using GameChatParser.Core.Games;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Tests;

/// <summary>
/// A stand-in for a game whose result only means something next to the rest of the field,
/// used to show where the pipeline calls the normalisation and summary hooks.
/// <see cref="FakeGame"/> deliberately implements neither, so that it goes on proving a
/// game needs nothing beyond the three required members of <see cref="IGame"/>.
/// </summary>
internal sealed class FakeNormalisingGame(
    Func<IReadOnlyList<GameScore>, IReadOnlyList<GameScore>>? normalise = null,
    Func<IReadOnlyList<GameScore>, double>? summarise = null) : IGame
{
    public string Name => "Fake";

    public RankingDirection RankingDirection => RankingDirection.LowerIsBetter;

    public GameScore? TryParseScore(ChatMessage message) => null;

    public IReadOnlyList<GameScore> Normalise(IReadOnlyList<GameScore> scores) =>
        normalise is null ? scores : normalise(scores);

    public double Summarise(IReadOnlyList<GameScore> scores) =>
        summarise is null ? scores.Average(score => score.Value) : summarise(scores);
}
