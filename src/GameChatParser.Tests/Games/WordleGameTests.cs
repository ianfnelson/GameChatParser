using GameChatParser.Core.Games;
using GameChatParser.Core.Games.Wordle;

namespace GameChatParser.Tests.Games;

public class WordleGameTests
{
    private readonly WordleGame _game = new();

    [Fact]
    public void Is_ranked_on_the_lowest_average()
    {
        Assert.Equal("Wordle", _game.Name);
        Assert.Equal(RankingDirection.LowerIsBetter, _game.RankingDirection);
    }

    [Theory]
    [InlineData("1/6", 1)]
    [InlineData("2/6", 2)]
    [InlineData("3/6", 3)]
    [InlineData("4/6", 4)]
    [InlineData("5/6", 5)]
    [InlineData("6/6", 6)]
    public void Scores_a_solved_puzzle_as_the_number_of_guesses(string result, int expected)
    {
        var score = _game.TryParseScore(TestChat.Message("Joe Whelan", $"Wordle 1,281 {result}"));

        Assert.NotNull(score);
        Assert.Equal(expected, score.Value);
    }

    [Fact]
    public void Scores_an_unsolved_puzzle_one_worse_than_the_maximum()
    {
        var score = _game.TryParseScore(TestChat.Message("Carol Whelan", "Wordle 1,284 X/6"));

        Assert.NotNull(score);
        Assert.Equal(WordleGame.FailureScore, score.Value);
        Assert.Equal(7, WordleGame.FailureScore);
    }

    [Theory]
    [InlineData("Wordle 1,281 4/6", 1281)]
    [InlineData("Wordle 1281 4/6", 1281)]
    [InlineData("Wordle 281 4/6", 281)]
    [InlineData("Wordle 12,345 4/6", 12345)]
    [InlineData("Wordle 123456 4/6", 123456)]
    public void Reads_the_puzzle_number_however_it_is_grouped(string line, int expected)
    {
        var score = _game.TryParseScore(TestChat.Message("Joe Whelan", line));

        Assert.NotNull(score);
        Assert.Equal(expected, score.PuzzleNumber);
    }

    [Fact]
    public void Dates_the_result_from_the_puzzle_number()
    {
        var score = _game.TryParseScore(TestChat.Message("Joe Whelan", "Wordle 1,281 6/6"));

        Assert.NotNull(score);
        Assert.Equal(new DateOnly(2024, 12, 21), score.Date);
    }

    [Fact]
    public void Credits_the_result_to_the_sender()
    {
        var score = _game.TryParseScore(TestChat.Message("Rosalind Ferrer", "Wordle 1,341 6/6"));

        Assert.NotNull(score);
        Assert.Equal("Rosalind Ferrer", score.Player);
    }

    [Fact]
    public void Reads_the_score_from_a_message_that_carries_its_grid()
    {
        var score = _game.TryParseScore(TestChat.Message(
            "Gregory Whelan",
            """
            Wordle 1,281 4/6

            🟨🟨⬛⬛⬛
            ⬛⬛🟨⬛⬛
            ⬛🟨⬛🟨🟨
            🟩🟩🟩🟩🟩
            """));

        Assert.NotNull(score);
        Assert.Equal(4, score.Value);
        Assert.Equal(1281, score.PuzzleNumber);
    }

    [Fact]
    public void Reads_a_score_that_appears_on_a_later_line()
    {
        var score = _game.TryParseScore(TestChat.Message(
            "Joe Whelan",
            """
            Catching up on yesterday
            Wordle 1,281 3/6
            """));

        Assert.NotNull(score);
        Assert.Equal(3, score.Value);
    }

    [Fact]
    public void Tolerates_a_hard_mode_marker_after_the_score()
    {
        var score = _game.TryParseScore(TestChat.Message("Joe Whelan", "Wordle 1,281 3/6*"));

        Assert.NotNull(score);
        Assert.Equal(3, score.Value);
    }

    [Fact]
    public void Tolerates_decoration_between_the_number_and_the_score()
    {
        var score = _game.TryParseScore(TestChat.Message("Joe Whelan", "Wordle 1,281 — 3/6"));

        Assert.NotNull(score);
        Assert.Equal(3, score.Value);
    }

    [Theory]
    [InlineData("Morning all")]
    [InlineData("Wordle was tough today")]
    [InlineData("Connections\nPuzzle #619\n🟩🟩🟩🟩")]
    [InlineData("I got Wordle 1,281 in 3")]
    [InlineData("Wordle 1,281 7/6")]
    [InlineData("Wordle 1,281 3/5")]
    public void Ignores_a_message_holding_no_wordle_result(string body)
    {
        Assert.Null(_game.TryParseScore(TestChat.Message("Joe Whelan", body)));
    }

    [Fact]
    public void Rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() => _game.TryParseScore(null!));
    }
}
