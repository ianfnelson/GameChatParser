using System.Globalization;
using GameChatParser.Core.Games;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Reporting;

/// <summary>
/// Turns a game's raw scores into the leaderboards the report is built from: the current
/// and previous year, and the current and previous month.
/// </summary>
public sealed class LeaderboardBuilder
{
    /// <summary>
    /// How many periods of each kind to report, counting back from the most recent period
    /// that has results. Two gives the current period and the one before it.
    /// </summary>
    public const int PeriodsPerKind = 2;

    /// <summary>
    /// How close two averages must be to count as a tie. Averages that ought to be equal
    /// can differ in the last bits after division, so they are not compared exactly.
    /// </summary>
    public const double TieTolerance = 0.0001d;

    /// <summary>
    /// Builds the leaderboards for a game, most recent period first within each kind, with
    /// the yearly tables ahead of the monthly ones.
    /// </summary>
    public IReadOnlyList<Leaderboard> Build(IGame game, IEnumerable<GameScore> scores)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(scores);

        // A player who posts the same result twice, or catches up and reposts, should
        // count once for that puzzle.
        var distinct = scores
            .DistinctBy(score => (score.Player, score.Date))
            .ToList();

        // A game ranked against the day's field rather than against a fixed scale settles
        // its scores here, with the repeats already gone and the periods not yet split
        // out, so that a day's field is the whole day's field.
        var ranked = game.Normalise(distinct);

        var years = ranked
            .GroupBy(score => score.Date.Year)
            .OrderByDescending(group => group.Key)
            .Take(PeriodsPerKind)
            .Select((group, index) => BuildLeaderboard(
                game,
                group.Key.ToString(CultureInfo.InvariantCulture),
                PeriodKind.Year,
                index,
                group));

        var months = ranked
            .GroupBy(score => (score.Date.Year, score.Date.Month))
            .OrderByDescending(group => group.Key.Year)
            .ThenByDescending(group => group.Key.Month)
            .Take(PeriodsPerKind)
            .Select((group, index) => BuildLeaderboard(
                game,
                MonthName(group.Key.Month),
                PeriodKind.Month,
                index,
                group));

        return [.. years, .. months];
    }

    private static Leaderboard BuildLeaderboard(
        IGame game,
        string periodName,
        PeriodKind periodKind,
        int periodIndex,
        IEnumerable<GameScore> scores)
    {
        var players = scores
            .GroupBy(score => score.Player)
            .Select(player => new
            {
                Player = player.Key,
                Played = player.Count(),
                Average = game.Summarise([.. player])
            })
            .ToList();

        var ordered = (game.RankingDirection == RankingDirection.LowerIsBetter
                ? players.OrderBy(player => player.Average)
                : players.OrderByDescending(player => player.Average))
            .ThenBy(player => player.Player, StringComparer.Ordinal)
            .ToList();

        var entries = new List<LeaderboardEntry>(ordered.Count);

        // Standard competition ranking: everyone in a run of equal averages shares the
        // position of the first of them, and the run's length is skipped afterwards.
        var start = 0;

        while (start < ordered.Count)
        {
            var end = start + 1;

            while (end < ordered.Count && AreTied(ordered[end - 1].Average, ordered[end].Average))
            {
                end++;
            }

            var isTied = end - start > 1;

            for (var index = start; index < end; index++)
            {
                entries.Add(new LeaderboardEntry
                {
                    Position = start + 1,
                    IsTied = isTied,
                    Player = ordered[index].Player,
                    Played = ordered[index].Played,
                    Average = ordered[index].Average
                });
            }

            start = end;
        }

        return new Leaderboard
        {
            GameName = game.Name,
            PeriodName = periodName,
            PeriodKind = periodKind,
            PeriodIndex = periodIndex,
            Entries = entries
        };
    }

    private static bool AreTied(double left, double right) => Math.Abs(left - right) < TieTolerance;

    private static string MonthName(int month) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
}
