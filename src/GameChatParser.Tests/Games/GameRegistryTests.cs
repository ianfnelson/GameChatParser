using GameChatParser.Core.Games;
using GameChatParser.Core.Games.Connections;
using GameChatParser.Core.Games.Wordle;

namespace GameChatParser.Tests.Games;

public class GameRegistryTests
{
    [Fact]
    public void Registers_wordle_ahead_of_connections()
    {
        Assert.Collection(
            GameRegistry.Default,
            game => Assert.IsType<WordleGame>(game),
            game => Assert.IsType<ConnectionsGame>(game));
    }

    [Fact]
    public void Gives_every_game_a_distinct_name()
    {
        var names = GameRegistry.Default.Select(game => game.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Gives_every_game_a_name()
    {
        Assert.All(GameRegistry.Default, game => Assert.False(string.IsNullOrWhiteSpace(game.Name)));
    }
}
