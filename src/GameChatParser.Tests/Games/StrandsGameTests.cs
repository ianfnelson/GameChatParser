using GameChatParser.Core.Games;
using GameChatParser.Core.Games.Strands;

namespace GameChatParser.Tests.Games;

public class StrandsGameTests
{
    private readonly StrandsGame _game = new();

    [Fact]
    public void Is_ranked_on_the_lowest_average()
    {
        Assert.Equal("Strands", _game.Name);
        Assert.Equal(RankingDirection.LowerIsBetter, _game.RankingDirection);
    }

    [Fact]
    public void Scores_a_hint_free_game_with_the_spangram_found_first_at_nothing()
    {
        var score = Parse(
            """
            Strands #832
            “Track event”
            🟡🔵🔵🔵
            🔵🔵🔵🔵
            """);

        Assert.NotNull(score);
        Assert.Equal(0d, score.Value);
    }

    [Fact]
    public void Charges_the_whole_bonus_where_the_spangram_was_found_last()
    {
        var score = Parse(
            """
            Strands #832
            “Track event”
            🔵🔵🔵🔵
            🔵🔵🟡
            """);

        Assert.NotNull(score);
        Assert.Equal(StrandsGame.SpangramWeight, score.Value);
        Assert.Equal(0.5d, score.Value);
    }

    [Fact]
    public void Spreads_the_bonus_evenly_across_the_places_between()
    {
        var score = Parse(
            """
            Strands #832
            “Track event”
            🔵🔵🟡🔵
            🔵
            """);

        // Third of five, so half way from first to last, and half the bonus.
        Assert.NotNull(score);
        Assert.Equal(0.25d, score.Value, 10);
    }

    [Fact]
    public void Charges_a_whole_point_for_every_hint()
    {
        var score = Parse(
            """
            Strands #832
            “Track event”
            🟡🔵💡🔵
            🔵💡🔵🔵
            💡🔵
            """);

        Assert.NotNull(score);
        Assert.Equal(3d, score.Value);
    }

    [Theory]
    [InlineData("🟡🔵🔵🔵\n🔵🔵🔵🔵", 0d)]
    [InlineData("🔵🔵🔵🔵\n🔵🔵🟡", 0.5d)]
    [InlineData("💡🔵💡🔵\n💡🔵🔵🟡\n🔵", 3.4d)]
    [InlineData("💡🔵💡🔵\n💡🔵🟡🔵\n💡🔵", 4.3d)]
    public void Adds_the_spangram_bonus_to_the_hints_taken(string grid, double expected)
    {
        var score = Parse($"Strands #832\n“Track event”\n{grid}");

        Assert.NotNull(score);
        Assert.Equal(expected, score.Value, 10);
    }

    [Fact]
    public void Never_lets_the_spangram_bonus_outweigh_a_single_hint()
    {
        var cleanButLate = Parse("Strands #832\n“Track event”\n🔵🔵🔵🔵\n🔵🔵🟡");
        var hintedButEarly = Parse("Strands #832\n“Track event”\n🟡🔵🔵🔵\n🔵🔵💡🔵");

        Assert.NotNull(cleanButLate);
        Assert.NotNull(hintedButEarly);
        Assert.True(cleanButLate.Value < hintedButEarly.Value);
    }

    [Fact]
    public void Leaves_the_spangram_where_it_was_found_however_many_hints_precede_it()
    {
        // Both games found the spangram second, so both carry the same bonus and differ
        // only by the hint.
        var withoutHint = Parse("Strands #832\n“Track event”\n🔵🟡🔵🔵\n🔵🔵");
        var withHint = Parse("Strands #832\n“Track event”\n💡🔵🟡🔵\n🔵🔵🔵");

        Assert.NotNull(withoutHint);
        Assert.NotNull(withHint);
        Assert.Equal(1d, withHint.Value - withoutHint.Value, 10);
    }

    [Fact]
    public void Reads_a_holiday_grid_by_how_often_its_items_appear()
    {
        // The New York Times swapped the usual squares for fireworks and a flag on
        // 4 July, and the flag is a pair of regional indicators, so a parser counting
        // runes would see two items where the share holds one.
        var score = Parse(
            """
            Strands #853
            “Happy 4th of July!”
            🎆🎆🎆🎆
            🎆🇺🇸
            """);

        Assert.NotNull(score);
        Assert.Equal(StrandsGame.SpangramWeight, score.Value);
    }

    [Fact]
    public void Scores_a_holiday_grid_exactly_as_it_would_the_usual_squares()
    {
        var holiday = Parse("Strands #853\n“Happy 4th of July!”\n🎆🎆💡🎆\n🎆🇺🇸🎆");
        var usual = Parse("Strands #853\n“Happy 4th of July!”\n🔵🔵💡🔵\n🔵🟡🔵");

        Assert.NotNull(holiday);
        Assert.NotNull(usual);
        Assert.Equal(usual.Value, holiday.Value);
    }

    [Fact]
    public void Counts_the_hints_carried_by_a_short_last_row()
    {
        // 106 of the 136 shares in the export end in a remainder row, and some of those
        // rows hold a hint, so dropping them would report the game as cleaner than it was.
        var withRemainder = Parse("Strands #832\n“Track event”\n🟡🔵🔵🔵\n💡🔵");
        var withoutRemainder = Parse("Strands #832\n“Track event”\n🟡🔵🔵🔵");

        Assert.NotNull(withRemainder);
        Assert.NotNull(withoutRemainder);
        Assert.Equal(1d, withRemainder.Value);
        Assert.Equal(0d, withoutRemainder.Value);
    }

    [Fact]
    public void Stops_reading_the_grid_at_a_closing_remark()
    {
        var score = Parse(
            """
            Strands #832
            “Track event”
            🟡🔵🔵🔵
            🔵🔵🔵🔵
            Got the spangram straight off today!
            🔵🔵💡🔵
            """);

        Assert.NotNull(score);
        Assert.Equal(0d, score.Value);
    }

    [Fact]
    public void Reads_the_grid_past_a_blank_line_beneath_the_clue()
    {
        var score = Parse("Strands #832\n“Track event”\n\n🔵🔵🔵🔵\n🔵🟡");

        Assert.NotNull(score);
        Assert.Equal(StrandsGame.SpangramWeight, score.Value);
    }

    [Theory]
    [InlineData("Strands #764")]
    [InlineData("strands #764")]
    [InlineData("STRANDS #764")]
    [InlineData("Strands # 764")]
    [InlineData("Strands#764")]
    public void Reads_the_puzzle_number_however_it_is_written(string puzzleLine)
    {
        var score = Parse($"{puzzleLine}\n“Track event”\n🟡🔵🔵🔵\n🔵🔵🔵🔵");

        Assert.NotNull(score);
        Assert.Equal(764, score.PuzzleNumber);
    }

    [Fact]
    public void Reads_a_puzzle_number_carrying_a_thousands_separator()
    {
        var score = Parse("Strands #1,024\n“Track event”\n🟡🔵🔵🔵\n🔵🔵🔵🔵");

        Assert.NotNull(score);
        Assert.Equal(1024, score.PuzzleNumber);
    }

    [Theory]
    [InlineData(1, 2024, 3, 4)]
    [InlineData(764, 2026, 4, 6)]
    [InlineData(834, 2026, 6, 15)]
    [InlineData(841, 2026, 6, 22)]
    [InlineData(363, 2025, 3, 1)]
    public void Dates_the_result_from_the_puzzle_number(int puzzleNumber, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), _game.DateOfPuzzle(puzzleNumber));
    }

    [Fact]
    public void Credits_the_result_to_the_sender()
    {
        var score = _game.TryParseScore(TestChat.Message(
            "Katie Munro",
            "Strands #832\n“Track event”\n🟡🔵🔵🔵\n🔵🔵🔵🔵"));

        Assert.NotNull(score);
        Assert.Equal("Katie Munro", score.Player);
    }

    [Fact]
    public void Ignores_a_puzzle_number_shared_without_a_grid()
    {
        Assert.Null(Parse("Strands #832\n“Track event”\nGave up on that one in the end"));
    }

    [Fact]
    public void Ignores_a_puzzle_number_mentioned_part_way_through_a_line()
    {
        Assert.Null(Parse("Anyone done Strands #832 yet?\n🟡🔵🔵🔵\n🔵🔵🔵🔵"));
    }

    [Fact]
    public void Ignores_a_share_holding_nothing_but_a_short_row()
    {
        // A full row is what anchors a grid, so a stray line of punctuation beneath a
        // puzzle number is not read as somebody's result.
        Assert.Null(Parse("Strands #832\n“Track event”\n🟡🔵"));
    }

    [Fact]
    public void Ignores_a_grid_holding_no_spangram()
    {
        Assert.Null(Parse("Strands #832\n“Track event”\n🔵🔵🔵🔵\n🔵🔵🔵"));
    }

    [Fact]
    public void Ignores_a_grid_holding_more_than_one_candidate_spangram()
    {
        Assert.Null(Parse("Strands #832\n“Track event”\n🟡🔵🔵🟢\n🔵🔵🔵"));
    }

    [Fact]
    public void Ignores_a_connections_result_entirely()
    {
        Assert.Null(Parse("Connections\nPuzzle #619\n🟩🟩🟩🟩\n🟨🟨🟨🟨\n🟦🟦🟦🟦\n🟪🟪🟪🟪"));
    }

    [Fact]
    public void Ignores_a_wordle_result_entirely()
    {
        Assert.Null(Parse("Wordle 1,341 4/6\n\n⬜⬜🟨⬜⬜\n⬜🟩⬜🟩🟩\n🟩🟩🟩🟩🟩"));
    }

    [Theory]
    [InlineData("Morning all")]
    [InlineData("Strands")]
    [InlineData("🟡🔵🔵🔵\n🔵🔵🔵🔵")]
    public void Ignores_a_message_holding_no_strands_result(string body)
    {
        Assert.Null(Parse(body));
    }

    [Fact]
    public void Rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() => _game.TryParseScore(null!));
    }

    private Core.Scoring.GameScore? Parse(string body) =>
        _game.TryParseScore(TestChat.Message("Joe Whelan", body));
}
