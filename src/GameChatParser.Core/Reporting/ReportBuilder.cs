using GameChatParser.Core.Chat;
using GameChatParser.Core.Games;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Reporting;

/// <summary>
/// Runs the whole pipeline: reads the chat, offers every message to every game, and
/// arranges the resulting leaderboards into a report.
/// </summary>
public sealed class ReportBuilder(
    IChatReader chatReader,
    IReadOnlyList<IGame> games,
    LeaderboardBuilder leaderboardBuilder)
{
    public ReportBuilder()
        : this(new WhatsAppChatReader(), GameRegistry.Default, new LeaderboardBuilder())
    {
    }

    public Report Build(IEnumerable<string> chatLines)
    {
        ArgumentNullException.ThrowIfNull(chatLines);

        var scoresByGame = games.Select(game => (Game: game, Scores: new List<GameScore>())).ToList();

        // The chat is read once and each message offered to every game, so a message
        // holding results for two games contributes to both.
        foreach (var message in chatReader.Read(chatLines))
        {
            foreach (var (game, scores) in scoresByGame)
            {
                if (game.TryParseScore(message) is { } score)
                {
                    scores.Add(score);
                }
            }
        }

        // A game's tables are kept together and the games run in name order, so the report
        // reads game by game, and within a game the yearly tables come ahead of the
        // monthly ones, most recent period first.
        var leaderboards = scoresByGame
            .SelectMany(entry => leaderboardBuilder.Build(entry.Game, entry.Scores))
            .OrderBy(leaderboard => leaderboard.GameName, StringComparer.Ordinal)
            .ThenBy(leaderboard => leaderboard.PeriodKind)
            .ThenBy(leaderboard => leaderboard.PeriodIndex)
            .ToList();

        return new Report { Leaderboards = leaderboards };
    }
}
