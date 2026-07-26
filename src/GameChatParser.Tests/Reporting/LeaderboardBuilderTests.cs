using GameChatParser.Core.Games;
using GameChatParser.Core.Reporting;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Tests.Reporting;

public class LeaderboardBuilderTests
{
    private readonly LeaderboardBuilder _builder = new();

    [Fact]
    public void Reports_the_two_most_recent_years_and_months()
    {
        var scores = new[]
        {
            Score("Bea", new DateOnly(2024, 5, 1), 4),
            Score("Bea", new DateOnly(2025, 5, 1), 4),
            Score("Bea", new DateOnly(2026, 6, 1), 4),
            Score("Bea", new DateOnly(2026, 7, 1), 4)
        };

        var leaderboards = _builder.Build(new FakeGame(), scores);

        Assert.Equal(
            [("2026", PeriodKind.Year), ("2025", PeriodKind.Year), ("July", PeriodKind.Month), ("June", PeriodKind.Month)],
            leaderboards.Select(board => (board.PeriodName, board.PeriodKind)));
    }

    [Fact]
    public void Numbers_the_periods_from_the_most_recent()
    {
        var scores = new[]
        {
            Score("Bea", new DateOnly(2025, 5, 1), 4),
            Score("Bea", new DateOnly(2026, 7, 1), 4)
        };

        var leaderboards = _builder.Build(new FakeGame(), scores);

        Assert.Equal([0, 1, 0, 1], leaderboards.Select(board => board.PeriodIndex));
    }

    [Fact]
    public void Skips_periods_the_chat_holds_no_results_for()
    {
        var scores = new[] { Score("Bea", new DateOnly(2026, 7, 1), 4) };

        var leaderboards = _builder.Build(new FakeGame(), scores);

        Assert.Equal(["2026", "July"], leaderboards.Select(board => board.PeriodName));
    }

    [Fact]
    public void Reports_nothing_when_there_are_no_scores()
    {
        Assert.Empty(_builder.Build(new FakeGame(), []));
    }

    [Fact]
    public void Names_the_game_on_every_leaderboard()
    {
        var leaderboards = _builder.Build(
            new FakeGame("Quordle"),
            [Score("Bea", new DateOnly(2026, 7, 1), 4)]);

        Assert.All(leaderboards, board => Assert.Equal("Quordle", board.GameName));
        Assert.Equal("Quordle — July", leaderboards[1].Title);
    }

    [Fact]
    public void Puts_the_lowest_average_first_when_a_lower_score_is_better()
    {
        var scores = new[]
        {
            Score("Ana", new DateOnly(2026, 7, 1), 5),
            Score("Bea", new DateOnly(2026, 7, 1), 3),
            Score("Ari", new DateOnly(2026, 7, 1), 4)
        };

        var leaderboard = YearFrom(new FakeGame(rankingDirection: RankingDirection.LowerIsBetter), scores);

        Assert.Equal(["Bea", "Ari", "Ana"], leaderboard.Entries.Select(entry => entry.Player));
        Assert.Equal([1, 2, 3], leaderboard.Entries.Select(entry => entry.Position));
    }

    [Fact]
    public void Puts_the_highest_average_first_when_a_higher_score_is_better()
    {
        var scores = new[]
        {
            Score("Ana", new DateOnly(2026, 7, 1), 5),
            Score("Bea", new DateOnly(2026, 7, 1), 3),
            Score("Ari", new DateOnly(2026, 7, 1), 4)
        };

        var leaderboard = YearFrom(new FakeGame(rankingDirection: RankingDirection.HigherIsBetter), scores);

        Assert.Equal(["Ana", "Ari", "Bea"], leaderboard.Entries.Select(entry => entry.Player));
    }

    [Fact]
    public void Averages_a_players_scores_and_counts_what_they_played()
    {
        var scores = new[]
        {
            Score("Bea", new DateOnly(2026, 7, 1), 3),
            Score("Bea", new DateOnly(2026, 7, 2), 4),
            Score("Bea", new DateOnly(2026, 7, 3), 5),
            Score("Ana", new DateOnly(2026, 7, 1), 6)
        };

        var entry = Assert.Single(YearFrom(new FakeGame(), scores).Entries, entry => entry.Player == "Bea");

        Assert.Equal(3, entry.Played);
        Assert.Equal(4d, entry.Average);
    }

    [Fact]
    public void Counts_a_puzzle_once_however_often_a_player_posts_it()
    {
        var scores = new[]
        {
            Score("Bea", new DateOnly(2026, 7, 1), 3),
            Score("Bea", new DateOnly(2026, 7, 1), 6)
        };

        var entry = Assert.Single(YearFrom(new FakeGame(), scores).Entries);

        Assert.Equal(1, entry.Played);
        Assert.Equal(3d, entry.Average);
    }

    [Fact]
    public void Keeps_the_same_puzzle_for_each_player_separately()
    {
        var scores = new[]
        {
            Score("Bea", new DateOnly(2026, 7, 1), 3),
            Score("Ana", new DateOnly(2026, 7, 1), 6)
        };

        Assert.Equal(2, YearFrom(new FakeGame(), scores).Entries.Count);
    }

    [Fact]
    public void Shares_a_position_between_players_on_the_same_average()
    {
        var scores = new[]
        {
            Score("Ana", new DateOnly(2026, 7, 1), 3),
            Score("Ari", new DateOnly(2026, 7, 1), 4),
            Score("Bea", new DateOnly(2026, 7, 1), 4),
            Score("Theo", new DateOnly(2026, 7, 1), 5)
        };

        var entries = YearFrom(new FakeGame(), scores).Entries;

        Assert.Equal(["Ana", "Ari", "Bea", "Theo"], entries.Select(entry => entry.Player));
        Assert.Equal([1, 2, 2, 4], entries.Select(entry => entry.Position));
        Assert.Equal([false, true, true, false], entries.Select(entry => entry.IsTied));
    }

    [Fact]
    public void Shares_a_position_between_three_players()
    {
        var scores = new[]
        {
            Score("Ana", new DateOnly(2026, 7, 1), 4),
            Score("Ari", new DateOnly(2026, 7, 1), 4),
            Score("Bea", new DateOnly(2026, 7, 1), 4),
            Score("Theo", new DateOnly(2026, 7, 1), 5)
        };

        var entries = YearFrom(new FakeGame(), scores).Entries;

        Assert.Equal([1, 1, 1, 4], entries.Select(entry => entry.Position));
        Assert.Equal([true, true, true, false], entries.Select(entry => entry.IsTied));
    }

    [Fact]
    public void Breaks_a_tie_alphabetically_for_display_only()
    {
        var scores = new[]
        {
            Score("Yuri", new DateOnly(2026, 7, 1), 4),
            Score("Ana", new DateOnly(2026, 7, 1), 4)
        };

        var entries = YearFrom(new FakeGame(), scores).Entries;

        Assert.Equal(["Ana", "Yuri"], entries.Select(entry => entry.Player));
        Assert.Equal([1, 1], entries.Select(entry => entry.Position));
    }

    [Fact]
    public void Treats_averages_within_the_tolerance_as_a_tie()
    {
        var scores = new[]
        {
            Score("Ana", new DateOnly(2026, 7, 1), 4),
            Score("Bea", new DateOnly(2026, 7, 1), 4 + (LeaderboardBuilder.TieTolerance / 2))
        };

        Assert.Equal([1, 1], YearFrom(new FakeGame(), scores).Entries.Select(entry => entry.Position));
    }

    [Fact]
    public void Separates_averages_beyond_the_tolerance()
    {
        var scores = new[]
        {
            Score("Ana", new DateOnly(2026, 7, 1), 4),
            Score("Bea", new DateOnly(2026, 7, 1), 4 + (LeaderboardBuilder.TieTolerance * 2))
        };

        Assert.Equal([1, 2], YearFrom(new FakeGame(), scores).Entries.Select(entry => entry.Position));
    }

    [Fact]
    public void Reports_a_month_and_a_year_that_span_the_new_year()
    {
        var scores = new[]
        {
            Score("Bea", new DateOnly(2025, 12, 31), 4),
            Score("Bea", new DateOnly(2026, 1, 1), 5)
        };

        var leaderboards = _builder.Build(new FakeGame(), scores);

        Assert.Equal(["2026", "2025", "January", "December"], leaderboards.Select(board => board.PeriodName));
    }

    [Fact]
    public void Keeps_the_same_month_of_different_years_apart()
    {
        var scores = new[]
        {
            Score("Bea", new DateOnly(2025, 7, 1), 4),
            Score("Bea", new DateOnly(2026, 7, 1), 5)
        };

        var months = _builder.Build(new FakeGame(), scores)
            .Where(board => board.PeriodKind == PeriodKind.Month)
            .ToList();

        Assert.Equal(2, months.Count);
        Assert.All(months, month => Assert.Equal("July", month.PeriodName));
        Assert.All(months, month => Assert.Equal(1, Assert.Single(month.Entries).Played));
    }

    [Fact]
    public void Rejects_a_null_game_or_null_scores()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.Build(null!, []));
        Assert.Throws<ArgumentNullException>(() => _builder.Build(new FakeGame(), null!));
    }

    private Leaderboard YearFrom(IGame game, IEnumerable<GameScore> scores) =>
        _builder.Build(game, scores).First(board => board.PeriodKind == PeriodKind.Year);

    private static GameScore Score(string player, DateOnly date, double value) =>
        new(player, date.DayNumber, date, value);
}
