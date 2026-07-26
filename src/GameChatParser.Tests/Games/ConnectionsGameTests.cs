using GameChatParser.Core.Games;
using GameChatParser.Core.Games.Connections;

namespace GameChatParser.Tests.Games;

public class ConnectionsGameTests
{
    private readonly ConnectionsGame _game = new();

    [Fact]
    public void Is_ranked_on_the_highest_average()
    {
        Assert.Equal("Connections", _game.Name);
        Assert.Equal(RankingDirection.HigherIsBetter, _game.RankingDirection);
    }

    [Fact]
    public void Scores_a_flawless_game_at_ten_points_a_group()
    {
        var score = Parse(
            """
            Connections
            Puzzle #619
            🟩🟩🟩🟩
            🟨🟨🟨🟨
            🟦🟦🟦🟦
            🟪🟪🟪🟪
            """);

        Assert.NotNull(score);
        Assert.Equal(40, score.Value);
    }

    [Fact]
    public void Deducts_a_point_for_a_single_mistake()
    {
        var score = Parse(
            """
            Connections
            Puzzle #619
            🟦🟦🟩🟦
            🟦🟦🟦🟦
            🟨🟨🟨🟨
            🟩🟩🟩🟩
            🟪🟪🟪🟪
            """);

        Assert.NotNull(score);
        Assert.Equal(39, score.Value);
    }

    [Fact]
    public void Deducts_a_point_for_every_mistake()
    {
        var score = Parse(
            """
            Connections
            Puzzle #620
            🟨🟨🟨🟪
            🟩🟩🟩🟦
            🟨🟨🟨🟨
            🟩🟩🟩🟩
            🟦🟦🟦🟦
            🟪🟪🟪🟪
            """);

        Assert.NotNull(score);
        Assert.Equal(38, score.Value);
    }

    [Fact]
    public void Scores_a_game_won_with_the_last_mistake_going_spare()
    {
        var score = Parse(
            """
            Connections
            Puzzle #620
            🟨🟨🟨🟪
            🟩🟩🟩🟦
            🟦🟦🟨🟦
            🟨🟨🟨🟨
            🟩🟩🟩🟩
            🟦🟦🟦🟦
            🟪🟪🟪🟪
            """);

        Assert.NotNull(score);
        Assert.Equal((4 * ConnectionsGame.PointsPerGroupSolved) - (3 * ConnectionsGame.PenaltyPerMistake), score.Value);
        Assert.Equal(37, score.Value);
    }

    [Fact]
    public void Scores_a_game_lost_after_three_groups()
    {
        var score = Parse(
            """
            Connections
            Puzzle #620
            🟨🟨🟨🟪
            🟩🟩🟩🟦
            🟦🟦🟨🟦
            🟪🟪🟨🟪
            🟨🟨🟨🟨
            🟩🟩🟩🟩
            🟦🟦🟦🟦
            """);

        Assert.NotNull(score);
        Assert.Equal((3 * ConnectionsGame.PointsPerGroupSolved) - (4 * ConnectionsGame.PenaltyPerMistake), score.Value);
        Assert.Equal(26, score.Value);
    }

    [Fact]
    public void Scores_a_game_lost_without_a_single_group()
    {
        var score = Parse(
            """
            Connections
            Puzzle #623
            🟩🟦🟨🟨
            🟩🟦🟩🟩
            🟩🟦🟩🟩
            🟩🟦🟨🟩
            """);

        Assert.NotNull(score);
        Assert.Equal(-4, score.Value);
    }

    [Theory]
    [InlineData("Puzzle #619")]
    [InlineData("puzzle #619")]
    [InlineData("PUZZLE #619")]
    [InlineData("Puzzle # 619")]
    [InlineData("Puzzle#619")]
    public void Reads_the_puzzle_number_however_it_is_written(string puzzleLine)
    {
        var score = Parse($"Connections\n{puzzleLine}\n🟩🟩🟩🟩\n🟨🟨🟨🟨\n🟦🟦🟦🟦\n🟪🟪🟪🟪");

        Assert.NotNull(score);
        Assert.Equal(619, score.PuzzleNumber);
    }

    [Fact]
    public void Dates_the_result_from_the_puzzle_number()
    {
        var score = Parse("Connections\nPuzzle #619\n🟩🟩🟩🟩\n🟨🟨🟨🟨\n🟦🟦🟦🟦\n🟪🟪🟪🟪");

        Assert.NotNull(score);
        Assert.Equal(new DateOnly(2025, 2, 19), score.Date);
    }

    [Fact]
    public void Credits_the_result_to_the_sender()
    {
        var score = _game.TryParseScore(TestChat.Message(
            "Katie Munro",
            "Connections\nPuzzle #619\n🟩🟩🟩🟩\n🟨🟨🟨🟨\n🟦🟦🟦🟦\n🟪🟪🟪🟪"));

        Assert.NotNull(score);
        Assert.Equal("Katie Munro", score.Player);
    }

    [Fact]
    public void Keeps_reading_the_grid_past_a_closing_remark()
    {
        var score = Parse(
            """
            Connections
            Puzzle #624
            🟦🟩🟦🟦
            🟦🟩🟦🟦
            🟨🟨🟦🟨
            🟪🟨🟦🟨

            Didn't like connections today!
            """);

        Assert.NotNull(score);
        Assert.Equal(-4, score.Value);
    }

    [Fact]
    public void Ignores_a_puzzle_number_shared_without_a_grid()
    {
        Assert.Null(Parse("Tough one, that Puzzle #619"));
    }

    [Fact]
    public void Ignores_rows_that_are_not_four_squares_wide()
    {
        // A Wordle grid is five squares wide and shares two of its colours with
        // Connections, so width is what tells the two apart.
        Assert.Null(Parse(
            """
            Puzzle #619
            🟨🟨🟩🟩🟩
            🟩🟩🟩🟩🟩
            """));
    }

    [Fact]
    public void Ignores_a_wordle_result_entirely()
    {
        Assert.Null(Parse("Wordle 1,341 4/6\n\n⬜⬜🟨⬜⬜\n⬜🟩⬜🟩🟩\n🟩🟩🟩🟩🟩"));
    }

    [Theory]
    [InlineData("Morning all")]
    [InlineData("Connections")]
    [InlineData("🟩🟩🟩🟩\n🟨🟨🟨🟨\n🟦🟦🟦🟦\n🟪🟪🟪🟪")]
    public void Ignores_a_message_holding_no_connections_result(string body)
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
