namespace GameChatParser.Core.Scoring;

/// <summary>
/// One player's result for one puzzle.
/// </summary>
/// <param name="Player">The name the player posts under in the chat.</param>
/// <param name="PuzzleNumber">The puzzle's sequence number, as printed in the shared result.</param>
/// <param name="Date">The day the puzzle belongs to, derived from its number.</param>
/// <param name="Value">
/// The quantity the game is ranked on. Whether a larger value is better depends on
/// the game's <see cref="Games.IGame.RankingDirection"/>.
/// </param>
public sealed record GameScore(string Player, int PuzzleNumber, DateOnly Date, double Value);
