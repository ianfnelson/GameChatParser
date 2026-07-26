using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GameChatParser.Core.Chat;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Games.Connections;

/// <summary>
/// Connections, shared as a <c>Puzzle #619</c> line above a grid of four-square rows.
/// Unlike Wordle the shared result carries no summary of how well the player did, so
/// the score has to be reconstructed from the grid itself.
/// </summary>
public sealed partial class ConnectionsGame : PuzzleGame
{
    /// <summary>Points awarded for each of the four groups a player solves.</summary>
    public const int PointsPerGroupSolved = 10;

    /// <summary>Points deducted for each wrong guess.</summary>
    public const int PenaltyPerMistake = 1;

    /// <summary>The number of squares in a row of the shared grid, one per selected word.</summary>
    private const int SquaresPerRow = 4;

    public override string Name => "Connections";

    public override RankingDirection RankingDirection => RankingDirection.HigherIsBetter;

    /// <summary>
    /// Calibrated so that Puzzle #619 falls on 19 February 2025, the day it was shared,
    /// which puts puzzle number one on 12 June 2023 as expected.
    /// </summary>
    protected override DateOnly PuzzleZeroDate => new(2023, 6, 11);

    public override GameScore? TryParseScore(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var puzzle = PuzzleNumberLine().Match(message.Text);

        if (!puzzle.Success ||
            !int.TryParse(puzzle.Groups["number"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var puzzleNumber))
        {
            return null;
        }

        var rows = message.Lines.Select(ClassifyRow).Where(row => row != RowKind.NotAGrid).ToList();

        // A message can mention a puzzle number without sharing a result, in which case
        // there is nothing to score.
        if (rows.Count == 0)
        {
            return null;
        }

        var groupsSolved = rows.Count(row => row == RowKind.GroupSolved);
        var mistakes = rows.Count - groupsSolved;
        var score = (PointsPerGroupSolved * groupsSolved) - (PenaltyPerMistake * mistakes);

        return new GameScore(message.Sender, puzzleNumber, DateOfPuzzle(puzzleNumber), score);
    }

    /// <summary>
    /// Decides whether a line is a row of the shared grid and, if so, whether it records
    /// a solved group. A row of four identical squares means the player picked four words
    /// belonging to one category; any other mix of four squares is a wrong guess.
    /// </summary>
    private static RowKind ClassifyRow(string line)
    {
        Rune? first = null;
        var count = 0;
        var homogeneous = true;

        foreach (var rune in line.EnumerateRunes())
        {
            if (!IsConnectionsSquare(rune))
            {
                continue;
            }

            count++;

            if (first is null)
            {
                first = rune;
            }
            else if (rune != first)
            {
                homogeneous = false;
            }
        }

        if (count != SquaresPerRow)
        {
            return RowKind.NotAGrid;
        }

        return homogeneous ? RowKind.GroupSolved : RowKind.Mistake;
    }

    /// <summary>
    /// The four coloured squares Connections uses, one per category. These are outside
    /// the basic multilingual plane, so they are compared as runes rather than through a
    /// regular expression character class, which would otherwise match half of a square.
    /// </summary>
    private static bool IsConnectionsSquare(Rune rune) => rune.Value is
        0x1F7E8 or // 🟨 yellow
        0x1F7E9 or // 🟩 green
        0x1F7E6 or // 🟦 blue
        0x1F7EA;   // 🟪 purple

    [GeneratedRegex(@"\bPuzzle\s*#\s*(?<number>\d{1,10})\b", RegexOptions.IgnoreCase)]
    private static partial Regex PuzzleNumberLine();

    private enum RowKind
    {
        NotAGrid,
        GroupSolved,
        Mistake
    }
}
