using System.Globalization;
using System.Text.RegularExpressions;
using GameChatParser.Core.Chat;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Core.Games.Zanagrams;

/// <summary>
/// Zanagrams, shared as a heading naming the puzzle above the time it was solved in and
/// the hints that were taken. There are two puzzles a day, an Original and a harder
/// Master, sharing a puzzle number and a format; each is a game in its own right, and
/// this holds everything but the heading they are told apart by.
/// </summary>
/// <remarks>
/// A solve time means nothing on its own, because it is mostly a measure of how hard that
/// day's puzzle was: the export's times run from 41 seconds to nearly 22 minutes, and most
/// of that spread is the puzzle rather than the player. So parsing only gets as far as the
/// time a player took, and <see cref="Normalise"/> turns that into what they are ranked
/// on, which is how they did against everybody else who played the same puzzle.
/// </remarks>
public abstract partial class ZanagramsGame : PuzzleGame
{
    /// <summary>
    /// What a hint adds to the time a player took. A hint reveals the next few letters of
    /// a word rather than the word itself, and the family already treats it as a last
    /// resort: of the 148 shares in the export that report their hints, 137 took none.
    /// Twenty seconds is enough that buying a hint cannot buy a better placing, and little
    /// enough that one hint does not wreck a month.
    /// </summary>
    public const double SecondsPerHint = 20d;

    /// <summary>
    /// The fewest players a puzzle needs before it counts. A puzzle only one player posted
    /// has nobody to measure them against, so it is dropped rather than scored.
    /// </summary>
    public const int PlayersNeededPerPuzzle = 2;

    public override RankingDirection RankingDirection => RankingDirection.LowerIsBetter;

    /// <summary>
    /// Calibrated so that Zanagrams #1 falls on 24 June 2026: 151 of the export's 156
    /// shares were posted on the day this dates them to, and the five that were not are
    /// one player catching up on the first five puzzles in a single sitting.
    /// </summary>
    protected override DateOnly PuzzleZeroDate => new(2026, 6, 23);

    /// <summary>
    /// Matches the heading naming this game's puzzle, and only this game's, so that the
    /// Original and the Master never read each other's shares.
    /// </summary>
    protected abstract Regex PuzzleHeading { get; }

    /// <summary>
    /// Reads the time a player took, in seconds, with their hints charged for. This is not
    /// yet a score, only the raw material <see cref="Normalise"/> ranks on.
    /// </summary>
    public override GameScore? TryParseScore(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        for (var index = 0; index < message.Lines.Count; index++)
        {
            var heading = PuzzleHeading.Match(message.Lines[index]);

            if (!heading.Success)
            {
                continue;
            }

            var digits = heading.Groups["puzzle"].Value.Replace(",", string.Empty);

            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var puzzleNumber))
            {
                continue;
            }

            // A message can name a puzzle without sharing a result, and no failed game
            // appears in the export, so a share with no time is left alone rather than
            // guessed at as a loss.
            if (ReadSolveTime(message.Lines, index + 1) is not { } seconds)
            {
                return null;
            }

            var adjusted = seconds + (SecondsPerHint * ReadHints(message.Lines, index + 1));

            return new GameScore(message.Sender, puzzleNumber, DateOfPuzzle(puzzleNumber), adjusted);
        }

        return null;
    }

    /// <summary>
    /// Rewrites each player's time as how it compared with the rest of that puzzle's
    /// field, held as a logarithm, and drops the puzzles nobody can be compared on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The comparison is against the geometric mean of the <em>other</em> players' times,
    /// which is to say the mean of their logarithms. Logarithms because plain ratios are
    /// lopsided: solving in half the time would score half a point below par while taking
    /// twice as long would score a whole point above it, so bad days would count double.
    /// In log space the two are symmetric, and a hard puzzle that inflates everybody's
    /// time cancels out exactly.
    /// </para>
    /// <para>
    /// The field excludes the player because a baseline holding their own time lets their
    /// result set part of the bar they are judged against, which flattens both very good
    /// and very bad days, and flattens them more the fewer players posted. Leaving
    /// themselves out makes the figure mean the same thing whether two played or four.
    /// </para>
    /// </remarks>
    public override IReadOnlyList<GameScore> Normalise(IReadOnlyList<GameScore> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        var paced = new List<GameScore>(scores.Count);

        foreach (var puzzle in scores.GroupBy(score => score.PuzzleNumber))
        {
            var players = puzzle.ToList();

            if (players.Count < PlayersNeededPerPuzzle)
            {
                continue;
            }

            var totalLog = players.Sum(score => Math.Log(score.Value));

            paced.AddRange(players.Select(score =>
            {
                var ownLog = Math.Log(score.Value);
                var fieldLog = (totalLog - ownLog) / (players.Count - 1);

                return score with { Value = ownLog - fieldLog };
            }));
        }

        return paced;
    }

    /// <summary>
    /// Turns a player's log ratios back into the pace index the table prints, where 1.000
    /// is family par and 0.647 means they typically solve in about 65% of the time the
    /// rest of the family needs.
    /// </summary>
    public override double Summarise(IReadOnlyList<GameScore> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        return Math.Exp(scores.Average(score => score.Value));
    }

    /// <summary>
    /// Reads the solve time from the lines beneath the heading, in seconds, or
    /// <c>null</c> where there is none to read. A time of nothing at all is no more a
    /// result than a missing line is.
    /// </summary>
    private static double? ReadSolveTime(IReadOnlyList<string> lines, int firstLine)
    {
        for (var index = firstLine; index < lines.Count; index++)
        {
            var match = SolveTimeLine().Match(lines[index]);

            if (!match.Success)
            {
                continue;
            }

            var minutes = int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture);
            var seconds = int.Parse(match.Groups["seconds"].Value, CultureInfo.InvariantCulture);
            var total = (minutes * 60) + seconds;

            if (total > 0)
            {
                return total;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads how many hints were taken. The earliest shares carry no hint line at all, and
    /// those games were played before the line existed rather than played badly, so a
    /// missing line counts as no hints.
    /// </summary>
    private static int ReadHints(IReadOnlyList<string> lines, int firstLine)
    {
        for (var index = firstLine; index < lines.Count; index++)
        {
            var match = HintLine().Match(lines[index]);

            if (match.Success &&
                int.TryParse(match.Groups["hints"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hints))
            {
                return hints;
            }
        }

        return 0;
    }

    /// <summary>
    /// Matches the line reporting the solve time, which the game wrote as <c>Complete in</c>
    /// before it settled on <c>Solved in</c>; both appear in the export. The phrase is what
    /// picks the line out, which keeps the <c>🚀 02:02 faster than global average</c> line
    /// from being read as a time.
    /// </summary>
    [GeneratedRegex(@"\b(?:Solved|Complete)\s+in\s+(?<minutes>\d{1,3}):(?<seconds>\d{2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex SolveTimeLine();

    /// <summary>Matches the line reporting the hints, singular or plural.</summary>
    [GeneratedRegex(@"\b(?<hints>\d{1,3})\s+hints?\s+used\b", RegexOptions.IgnoreCase)]
    private static partial Regex HintLine();
}
