using System.Text.RegularExpressions;

namespace GameChatParser.Core.Games.Zanagrams;

/// <summary>
/// The day's harder Zanagrams puzzle, shared under a <c>Zanagrams Master #12</c> heading,
/// or a <c>🟠 Zanagrams Master #12</c> one since the game moved sites. It shares its number
/// and its format with the Original, but not its players, so it is ranked as a game of its
/// own rather than folded into one table.
/// </summary>
public sealed partial class ZanagramsMasterGame : ZanagramsGame
{
    public override string Name => "Zanagrams Master";

    protected override Regex PuzzleHeading => Heading();

    /// <summary>
    /// Matches the Master's heading, allowing the coloured disc the new site puts in front
    /// of it, by <see cref="ZanagramsGame.HeadingDecoration"/>.
    /// </summary>
    [GeneratedRegex(
        @"^\s*" + HeadingDecoration + @"Zanagrams\s+Master\s*#\s*(?<puzzle>[\d,]{1,10})\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex Heading();
}
