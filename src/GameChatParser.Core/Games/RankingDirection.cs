namespace GameChatParser.Core.Games;

/// <summary>
/// Which way round a game's scores run, so that a single ranking algorithm can serve
/// games measured in guesses taken as well as games measured in points won.
/// </summary>
public enum RankingDirection
{
    /// <summary>A smaller average wins, as in Wordle, where the score counts guesses.</summary>
    LowerIsBetter,

    /// <summary>A larger average wins, as in Connections, where the score counts points.</summary>
    HigherIsBetter
}
