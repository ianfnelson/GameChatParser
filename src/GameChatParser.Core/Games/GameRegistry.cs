using GameChatParser.Core.Games.Connections;
using GameChatParser.Core.Games.Strands;
using GameChatParser.Core.Games.Wordle;
using GameChatParser.Core.Games.Zanagrams;

namespace GameChatParser.Core.Games;

/// <summary>
/// The games the parser knows about. To support another game, implement
/// <see cref="IGame"/> and add it to <see cref="Default"/>; the order here is the order
/// the games appear against each period in the report.
/// </summary>
public static class GameRegistry
{
    public static IReadOnlyList<IGame> Default { get; } =
    [
        new WordleGame(),
        new ConnectionsGame(),
        new StrandsGame(),
        new ZanagramsOriginalGame(),
        new ZanagramsMasterGame()
    ];
}
