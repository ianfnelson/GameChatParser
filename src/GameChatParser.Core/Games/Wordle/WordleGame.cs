using System.Globalization;
using System.Text.RegularExpressions;
using GameChatParser.Core.Chat;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Games.Wordle;

/// <summary>
/// Wordle, shared as a line like <c>Wordle 1,341 4/6</c> above a grid of squares. The
/// score is simply the number of guesses taken, so fewer is better.
/// </summary>
public sealed partial class WordleGame : PuzzleGame
{
    /// <summary>
    /// The score charged for failing to solve the puzzle, which shows as <c>X/6</c>. It
    /// sits one worse than the maximum of six guesses, so a failure costs a player more
    /// than a scrape home but does not swamp the rest of their month.
    /// </summary>
    public const int FailureScore = 7;

    public override string Name => "Wordle";

    public override RankingDirection RankingDirection => RankingDirection.LowerIsBetter;

    /// <summary>
    /// Calibrated so that Wordle 1,281 falls on 21 December 2024, the day it was shared.
    /// This is a day later than Wordle's original launch date, because the New York
    /// Times shifted the numbering by one after taking the game over.
    /// </summary>
    protected override DateOnly PuzzleZeroDate => new(2021, 6, 19);

    public override GameScore? TryParseScore(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        foreach (var line in message.Lines)
        {
            var match = ScoreLine().Match(line);

            if (!match.Success)
            {
                continue;
            }

            var digits = match.Groups["puzzle"].Value.Replace(",", string.Empty);

            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var puzzleNumber))
            {
                continue;
            }

            var guesses = match.Groups["guesses"].Value;
            var score = guesses == "X" ? FailureScore : int.Parse(guesses, CultureInfo.InvariantCulture);

            return new GameScore(message.Sender, puzzleNumber, DateOfPuzzle(puzzleNumber), score);
        }

        return null;
    }

    /// <summary>
    /// Matches the summary line, tolerating the thousands separator in the puzzle number
    /// and whatever punctuation or emoji a client slips between the number and the score.
    /// </summary>
    [GeneratedRegex(@"^\s*Wordle\s*(?<puzzle>[\d,]{1,10})[^\d\n]{0,5}(?<guesses>[1-6X])/6")]
    private static partial Regex ScoreLine();
}
