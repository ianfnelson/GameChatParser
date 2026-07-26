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

        // Games keep their registration order within a period, and a game's periods are
        // paired with the matching periods of the other games, so the report reads year
        // by year and then month by month.
        var leaderboards = scoresByGame
            .SelectMany((entry, gameIndex) => leaderboardBuilder
                .Build(entry.Game, entry.Scores)
                .Select(leaderboard => (leaderboard, gameIndex)))
            .OrderBy(item => item.leaderboard.PeriodKind)
            .ThenBy(item => item.leaderboard.PeriodIndex)
            .ThenBy(item => item.gameIndex)
            .Select(item => item.leaderboard)
            .ToList();

        return new Report { Leaderboards = leaderboards };
    }
}
