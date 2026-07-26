using System.Globalization;
using System.Text.RegularExpressions;
using GameChatParser.Core.Chat;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Games.Strands;

/// <summary>
/// Strands, shared as a <c>Strands #832</c> line above the theme's clue and a grid
/// recording the order the theme words were found in. The share only exists for a
/// completed puzzle, so there is no losing outcome to score; what separates one game
/// from another is how many hints were taken and how early the spangram was spotted.
/// </summary>
public sealed partial class StrandsGame : PuzzleGame
{
    /// <summary>
    /// How much a game's whole spangram bonus is worth, charged in full when the
    /// spangram was the last word found and not at all when it was the first. At half a
    /// hint, the entire bonus is worth less than one hint, so hint discipline always
    /// decides the order and the bonus only separates players already level on hints.
    /// </summary>
    public const double SpangramWeight = 0.5;

    /// <summary>
    /// The item marking a hint, earned by finding three words outside the theme. Unlike
    /// the theme and spangram items this one is fixed, since the holiday grids that swap
    /// the other two leave it alone.
    /// </summary>
    private const string HintItem = "💡";

    /// <summary>
    /// How many items the first row of a grid must hold. The game wraps its grid four to
    /// a line, and no puzzle holds fewer than five theme words, so every share opens with
    /// a full row. Requiring it is what keeps a stray line of punctuation from being read
    /// as somebody's result.
    /// </summary>
    private const int ItemsInAFullRow = 4;

    public override string Name => "Strands";

    public override RankingDirection RankingDirection => RankingDirection.LowerIsBetter;

    /// <summary>
    /// Calibrated so that Strands #1 falls on 4 March 2024, the day the game entered
    /// beta, which puts Strands #764 on 6 April 2026 as shared.
    /// </summary>
    protected override DateOnly PuzzleZeroDate => new(2024, 3, 3);

    public override GameScore? TryParseScore(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        for (var index = 0; index < message.Lines.Count; index++)
        {
            var match = PuzzleNumberLine().Match(message.Lines[index]);

            if (!match.Success)
            {
                continue;
            }

            var digits = match.Groups["puzzle"].Value.Replace(",", string.Empty);

            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var puzzleNumber))
            {
                continue;
            }

            // A message can name a puzzle without sharing a result, in which case there
            // is nothing to score.
            var grid = ReadGrid(message.Lines, index + 1);

            if (grid.Count == 0 || !TryScore(grid, out var score))
            {
                return null;
            }

            return new GameScore(message.Sender, puzzleNumber, DateOfPuzzle(puzzleNumber), score);
        }

        return null;
    }

    /// <summary>
    /// Collects the grid's items from the lines beneath the puzzle number. The clue sits
    /// between the two, so rows are looked for rather than expected immediately; the
    /// first full row anchors the grid, every row after it counts however short, and the
    /// first line that is not a row ends it, which keeps a player's commentary out.
    /// </summary>
    private static List<string> ReadGrid(IReadOnlyList<string> lines, int firstLine)
    {
        var grid = new List<string>();
        var started = false;

        for (var index = firstLine; index < lines.Count; index++)
        {
            var row = ReadRow(lines[index]);

            if (!started)
            {
                if (row is { Count: >= ItemsInAFullRow })
                {
                    started = true;
                    grid.AddRange(row);
                }

                continue;
            }

            if (row is null or { Count: 0 })
            {
                break;
            }

            grid.AddRange(row);
        }

        return grid;
    }

    /// <summary>
    /// Reads a line as a row of the grid, returning its items, or <c>null</c> where the
    /// line holds a letter or a digit and so is prose rather than a row. Items are text
    /// elements rather than runes, because the flag the game used on 4 July is a pair of
    /// regional indicators and counting runes would report it as two items.
    /// </summary>
    private static List<string>? ReadRow(string line)
    {
        var items = new List<string>();
        var elements = StringInfo.GetTextElementEnumerator(line);

        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();

            if (element.Any(char.IsLetterOrDigit))
            {
                return null;
            }

            if (!IsInvisible(element))
            {
                items.Add(element);
            }
        }

        return items;
    }

    /// <summary>
    /// Whether a text element takes up no room, so that the spaces and the direction
    /// marks an export sprinkles through a line are not counted as items.
    /// </summary>
    private static bool IsInvisible(string element) => element.All(character =>
        char.IsWhiteSpace(character) ||
        char.GetUnicodeCategory(character) is UnicodeCategory.Format or UnicodeCategory.Control);

    /// <summary>
    /// Scores a grid as the hints taken plus the spangram's lateness among the words
    /// found, hints not counting towards the spangram's place.
    /// </summary>
    /// <remarks>
    /// The items are classified by how often they appear rather than against a fixed set
    /// of emoji, because the New York Times swaps the set on holiday puzzles: whatever is
    /// left once the hints are set aside is theme words but for a single item, and that
    /// one is the spangram. A grid that does not divide that way is not a result this can
    /// read, and is left alone rather than guessed at.
    /// </remarks>
    private static bool TryScore(List<string> grid, out double score)
    {
        score = 0;

        var hints = grid.Count(item => string.Equals(item, HintItem, StringComparison.Ordinal));
        var wordsFound = grid.Where(item => !string.Equals(item, HintItem, StringComparison.Ordinal)).ToList();

        var distinct = wordsFound.GroupBy(item => item, StringComparer.Ordinal).ToList();
        var singletons = distinct.Where(group => group.Count() == 1).ToList();

        if (distinct.Count != 2 || singletons.Count != 1)
        {
            return false;
        }

        // Two distinct items, exactly one of them appearing once, means at least three
        // words were found, so the span below is never divided by zero.
        var place = wordsFound.IndexOf(singletons[0].Key) + 1;
        var lateness = (double)(place - 1) / (wordsFound.Count - 1);

        score = hints + (SpangramWeight * lateness);

        return true;
    }

    /// <summary>
    /// Matches the line naming the puzzle, anchored to the start of a line so that prose
    /// mentioning a puzzle number is not read as a share, and tolerating the thousands
    /// separator the number will carry once the game passes its thousandth puzzle.
    /// </summary>
    [GeneratedRegex(@"^\s*Strands\s*#\s*(?<puzzle>[\d,]{1,10})\b", RegexOptions.IgnoreCase)]
    private static partial Regex PuzzleNumberLine();
}
