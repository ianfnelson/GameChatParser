using System.Text.RegularExpressions;

namespace GameChatParser.Core.Games.Zanagrams;

/// <summary>
/// The day's Original Zanagrams puzzle, shared under a <c>Zanagrams #12</c> heading.
/// </summary>
public sealed partial class ZanagramsOriginalGame : ZanagramsGame
{
    public override string Name => "Zanagrams";

    protected override Regex PuzzleHeading => Heading();

    /// <summary>
    /// Matches the Original's heading, anchored to the start of a line so that prose
    /// mentioning a puzzle number is not read as a share, and requiring the number to
    /// follow the game's name so that the Master's heading is left to the Master.
    /// </summary>
    [GeneratedRegex(@"^\s*Zanagrams\s*#\s*(?<puzzle>[\d,]{1,10})\b", RegexOptions.IgnoreCase)]
    private static partial Regex Heading();
}
