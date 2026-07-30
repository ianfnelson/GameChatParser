using GameChatParser.Core.Games;
using GameChatParser.Core.Games.Zanagrams;
using GameChatParser.Core.Reporting;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Tests.Games;

public class ZanagramsGameTests
{
    private readonly ZanagramsOriginalGame _game = new();
    private readonly ZanagramsMasterGame _master = new();

    [Fact]
    public void Is_ranked_on_the_lowest_average()
    {
        Assert.Equal("Zanagrams", _game.Name);
        Assert.Equal("Zanagrams Master", _master.Name);
        Assert.Equal(RankingDirection.LowerIsBetter, _game.RankingDirection);
        Assert.Equal(RankingDirection.LowerIsBetter, _master.RankingDirection);
    }

    [Fact]
    public void Reads_the_solve_time_as_seconds()
    {
        var score = Parse(
            """
            Zanagrams #12

            🎉 Solved in 01:22

            🚀 02:02 faster than global average

            💡 0 hints used

            https://zanagrams.com/
            """);

        Assert.NotNull(score);
        Assert.Equal(82d, score.Value);
    }

    [Fact]
    public void Reads_the_older_wording_the_game_shipped_with()
    {
        var solved = Parse("Zanagrams #12\n\n🎉 Solved in 05:50");
        var complete = Parse("Zanagrams #4\n\n🎉 Complete in 05:50");

        Assert.NotNull(solved);
        Assert.NotNull(complete);
        Assert.Equal(350d, complete.Value);
        Assert.Equal(solved.Value, complete.Value);
    }

    [Theory]
    [InlineData("💡 0 hints used", 0)]
    [InlineData("💡 1 hint used", 1)]
    [InlineData("💡 2 hints used", 2)]
    public void Charges_twenty_seconds_for_every_hint(string hintLine, int hints)
    {
        var score = Parse($"Zanagrams #12\n\n🎉 Solved in 01:22\n\n{hintLine}");

        Assert.NotNull(score);
        Assert.Equal(82d + (ZanagramsGame.SecondsPerHint * hints), score.Value);
    }

    [Fact]
    public void Treats_a_share_with_no_hint_line_as_a_share_with_no_hints()
    {
        // The earliest shares in the export carry no hint line at all, and those games
        // were played before the line existed rather than played badly.
        var silent = Parse("Zanagrams #4\n\n🎉 Complete in 01:22");
        var explicitly = Parse("Zanagrams #12\n\n🎉 Solved in 01:22\n\n💡 0 hints used");

        Assert.NotNull(silent);
        Assert.NotNull(explicitly);
        Assert.Equal(explicitly.Value, silent.Value);
    }

    [Fact]
    public void Ignores_the_line_boasting_about_the_global_average()
    {
        // The line only appears when the player beat the global average, so it is missing
        // from precisely the slower results, and the figure it carries moves through the
        // day as more people play. Its time must never be read as a solve time.
        var boasted = Parse("Zanagrams #12\n\n🎉 Solved in 01:22\n\n🚀 02:02 faster than global average\n\n💡 0 hints used");
        var quiet = Parse("Zanagrams #12\n\n🎉 Solved in 01:22\n\n💡 0 hints used");

        Assert.NotNull(boasted);
        Assert.NotNull(quiet);
        Assert.Equal(82d, boasted.Value);
        Assert.Equal(quiet.Value, boasted.Value);
    }

    [Fact]
    public void Reads_the_master_puzzle_under_its_own_heading()
    {
        var score = _master.TryParseScore(TestChat.Message(
            "Joe Whelan",
            "Zanagrams Master #12\n\n🎉 Solved in 02:29\n\n💡 0 hints used"));

        Assert.NotNull(score);
        Assert.Equal(12, score.PuzzleNumber);
        Assert.Equal(149d, score.Value);
    }

    [Fact]
    public void Keeps_the_two_puzzles_of_a_day_apart()
    {
        var original = TestChat.Message("Joe Whelan", "Zanagrams #12\n\n🎉 Solved in 01:22");
        var master = TestChat.Message("Joe Whelan", "Zanagrams Master #12\n\n🎉 Solved in 02:29");

        Assert.NotNull(_game.TryParseScore(original));
        Assert.Null(_master.TryParseScore(original));
        Assert.NotNull(_master.TryParseScore(master));
        Assert.Null(_game.TryParseScore(master));
    }

    [Theory]
    [InlineData("Zanagrams #12")]
    [InlineData("zanagrams #12")]
    [InlineData("ZANAGRAMS #12")]
    [InlineData("Zanagrams # 12")]
    [InlineData("Zanagrams#12")]
    public void Reads_the_puzzle_number_however_it_is_written(string heading)
    {
        var score = Parse($"{heading}\n\n🎉 Solved in 01:22");

        Assert.NotNull(score);
        Assert.Equal(12, score.PuzzleNumber);
    }

    [Fact]
    public void Reads_a_puzzle_number_carrying_a_thousands_separator()
    {
        var score = Parse("Zanagrams #1,024\n\n🎉 Solved in 01:22");

        Assert.NotNull(score);
        Assert.Equal(1024, score.PuzzleNumber);
    }

    [Theory]
    [InlineData(1, 2026, 6, 24)]
    [InlineData(7, 2026, 6, 30)]
    [InlineData(12, 2026, 7, 5)]
    [InlineData(33, 2026, 7, 26)]
    public void Dates_the_result_from_the_puzzle_number(int puzzleNumber, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), _game.DateOfPuzzle(puzzleNumber));
        Assert.Equal(new DateOnly(year, month, day), _master.DateOfPuzzle(puzzleNumber));
    }

    [Fact]
    public void Credits_the_result_to_the_sender()
    {
        var score = _game.TryParseScore(TestChat.Message(
            "Katie Munro",
            "Zanagrams #12\n\n🎉 Solved in 01:22"));

        Assert.NotNull(score);
        Assert.Equal("Katie Munro", score.Player);
    }

    [Fact]
    public void Ignores_a_puzzle_named_without_a_solve_time()
    {
        // No failed game appears in the export, so the shared format for one is unknown;
        // a share with no time to read is left alone rather than guessed at as a loss.
        Assert.Null(Parse("Zanagrams #12\n\nGave up on that one in the end"));
        Assert.Null(Parse("Zanagrams #12\n\n🚀 02:02 faster than global average"));
        Assert.Null(Parse("Zanagrams #12\n\n🎉 Solved in 00:00"));
    }

    [Fact]
    public void Ignores_a_puzzle_number_mentioned_part_way_through_a_line()
    {
        Assert.Null(Parse("Anyone done Zanagrams #12 yet?\n\n🎉 Solved in 01:22"));
    }

    [Fact]
    public void Ignores_the_other_games_entirely()
    {
        Assert.Null(Parse("Wordle 1,341 4/6\n\n⬜⬜🟨⬜⬜\n⬜🟩⬜🟩🟩\n🟩🟩🟩🟩🟩"));
        Assert.Null(Parse("Connections\nPuzzle #619\n🟩🟩🟩🟩\n🟨🟨🟨🟨\n🟦🟦🟦🟦\n🟪🟪🟪🟪"));
        Assert.Null(Parse("Strands #832\n“Track event”\n🟡🔵🔵🔵\n🔵🔵🔵🔵"));
    }

    [Theory]
    [InlineData("Morning all")]
    [InlineData("Zanagrams")]
    [InlineData("🎉 Solved in 01:22")]
    public void Ignores_a_message_holding_no_zanagrams_result(string body)
    {
        Assert.Null(Parse(body));
    }

    [Fact]
    public void Measures_a_player_against_the_geometric_mean_of_the_rest_of_the_field()
    {
        // Three players on one puzzle at 60, 120 and 240 seconds. Each is measured against
        // the geometric mean of the other two, so the middle one is exactly par.
        var paced = _game.Normalise(
        [
            Time("Ana", 12, 60),
            Time("Bea", 12, 120),
            Time("Theo", 12, 240)
        ]);

        Assert.Equal(1d / (2d * Math.Sqrt(2)), Pace(paced, "Ana"), 10);
        Assert.Equal(1d, Pace(paced, "Bea"), 10);
        Assert.Equal(2d * Math.Sqrt(2), Pace(paced, "Theo"), 10);
    }

    [Fact]
    public void Reads_the_same_whether_a_player_was_twice_as_fast_or_twice_as_slow()
    {
        // In log space the two are the same distance from par, which plain ratios would
        // get wrong: they would put one half a point below and the other a whole one above.
        var paced = _game.Normalise([Time("Ana", 12, 60), Time("Bea", 12, 240)]);

        Assert.Equal(0d, paced.Sum(score => score.Value), 10);
        Assert.Equal(0.25d, Pace(paced, "Ana"), 10);
        Assert.Equal(4d, Pace(paced, "Bea"), 10);
    }

    [Fact]
    public void Cancels_out_a_day_that_was_hard_on_everybody()
    {
        var easy = _game.Normalise([Time("Ana", 12, 60), Time("Bea", 12, 240)]);
        var hard = _game.Normalise([Time("Ana", 13, 600), Time("Bea", 13, 2400)]);

        Assert.Equal(easy.Select(score => score.Value), hard.Select(score => score.Value));
    }

    [Fact]
    public void Leaves_a_players_own_time_out_of_the_bar_they_are_measured_against()
    {
        // Adding a player who is exactly as fast as the one being measured must not move
        // that player's figure, which is only true of a baseline they are not part of.
        var pair = _game.Normalise([Time("Ana", 12, 60), Time("Bea", 12, 240)]);
        var trio = _game.Normalise([Time("Ana", 12, 60), Time("Bea", 12, 240), Time("Theo", 12, 240)]);

        Assert.Equal(Pace(pair, "Ana"), Pace(trio, "Ana"), 10);
    }

    [Fact]
    public void Drops_a_puzzle_only_one_player_posted()
    {
        // Nobody to compare against, so it counts towards nobody's figure and towards
        // nobody's played count.
        var paced = _game.Normalise([Time("Ana", 12, 60), Time("Ana", 13, 90), Time("Bea", 13, 90)]);

        Assert.Equal([13, 13], paced.Select(score => score.PuzzleNumber));
        Assert.All(paced, score => Assert.Equal(0d, score.Value, 10));
    }

    [Fact]
    public void Reports_a_period_as_the_exponentiated_mean_of_the_days_that_built_it()
    {
        // Half the field's time one day and twice it the next averages out to par.
        var scores = new[]
        {
            Time("Ana", 12, 60),
            Time("Bea", 12, 240),
            Time("Ana", 13, 240),
            Time("Bea", 13, 60)
        };

        var entries = YearFrom(_game, scores).Entries;

        Assert.Equal(["Ana", "Bea"], entries.Select(entry => entry.Player));
        Assert.All(entries, entry => Assert.Equal(2, entry.Played));
        Assert.All(entries, entry => Assert.Equal(1d, entry.Average, 10));
    }

    [Fact]
    public void Reads_a_player_the_same_way_in_the_monthly_table_as_in_the_yearly_one()
    {
        // Puzzle 7 falls in June and puzzle 12 in July, so the yearly table holds both and
        // the July table only the second; the baseline behind each day is the same either
        // way, because normalisation happens before the periods are split out.
        var scores = new[]
        {
            Time("Ana", 7, 60),
            Time("Bea", 7, 240),
            Time("Ana", 12, 60),
            Time("Bea", 12, 240)
        };

        var boards = new LeaderboardBuilder().Build(_game, scores);

        var year = boards.Single(board => board.PeriodName == "2026");
        var july = boards.Single(board => board.PeriodName == "July");

        Assert.Equal(0.25d, Average(year, "Ana"), 10);
        Assert.Equal(0.25d, Average(july, "Ana"), 10);
    }

    [Fact]
    public void Leaves_every_figure_alone_when_a_player_posts_the_same_result_twice()
    {
        GameScore[] once =
        [
            Time("Ana", 12, 60),
            Time("Bea", 12, 240),
            Time("Theo", 12, 240)
        ];

        GameScore[] twice = [.. once, Time("Ana", 12, 60)];

        var before = YearFrom(_game, once);
        var after = YearFrom(_game, twice);

        Assert.Equal(
            before.Entries.Select(entry => (entry.Player, entry.Played, entry.Average)),
            after.Entries.Select(entry => (entry.Player, entry.Played, entry.Average)));
    }

    [Fact]
    public void Rejects_a_null_message_or_null_scores()
    {
        Assert.Throws<ArgumentNullException>(() => _game.TryParseScore(null!));
        Assert.Throws<ArgumentNullException>(() => _game.Normalise(null!));
        Assert.Throws<ArgumentNullException>(() => _game.Summarise(null!));
    }

    private GameScore? Parse(string body) =>
        _game.TryParseScore(TestChat.Message("Joe Whelan", body));

    private GameScore Time(string player, int puzzleNumber, double seconds) =>
        new(player, puzzleNumber, _game.DateOfPuzzle(puzzleNumber), seconds);

    /// <summary>
    /// A normalised score read back as the pace index it will be printed as, rather than
    /// as the logarithm it is carried around as.
    /// </summary>
    private static double Pace(IReadOnlyList<GameScore> scores, string player) =>
        Math.Exp(scores.Single(score => score.Player == player).Value);

    private static Leaderboard YearFrom(IGame game, IEnumerable<GameScore> scores) =>
        new LeaderboardBuilder().Build(game, scores).First(board => board.PeriodKind == PeriodKind.Year);

    private static double Average(Leaderboard leaderboard, string player) =>
        leaderboard.Entries.Single(entry => entry.Player == player).Average;
}
