using GameChatParser.Core.Reporting;

namespace GameChatParser.Tests.Reporting;

public class PlayerNameShortenerTests
{
    [Fact]
    public void Shortens_an_unshared_forename_to_the_forename()
    {
        Assert.Equal("Joe", Shorten("Joe Whelan")["Joe Whelan"]);
    }

    [Fact]
    public void Leaves_a_player_who_has_only_a_forename_alone()
    {
        Assert.Equal("Ana", Shorten("Ana")["Ana"]);
    }

    [Fact]
    public void Adds_a_surname_initial_to_a_shared_forename()
    {
        var shortened = Shorten("Joe Whelan", "Joe Corbin", "Ana Corbin");

        Assert.Equal("Joe W.", shortened["Joe Whelan"]);
        Assert.Equal("Joe C.", shortened["Joe Corbin"]);
        Assert.Equal("Ana", shortened["Ana Corbin"]);
    }

    [Fact]
    public void Falls_back_to_whole_names_where_the_initial_is_shared_too()
    {
        var shortened = Shorten("Joe Whelan", "Joe Wright", "Joe Corbin");

        Assert.Equal("Joe Whelan", shortened["Joe Whelan"]);
        Assert.Equal("Joe Wright", shortened["Joe Wright"]);

        // Joe Corbin could keep an initial of his own, but showing him as "Joe C." beside
        // two full names would read as somebody the other two are not.
        Assert.Equal("Joe Corbin", shortened["Joe Corbin"]);
    }

    [Fact]
    public void Takes_the_last_word_as_the_surname()
    {
        var shortened = Shorten("Anna Marie Sutton", "Anna Corbin");

        Assert.Equal("Anna S.", shortened["Anna Marie Sutton"]);
        Assert.Equal("Anna C.", shortened["Anna Corbin"]);
    }

    [Fact]
    public void Treats_a_forename_shared_in_a_different_case_as_shared()
    {
        var shortened = Shorten("joe Whelan", "Joe Corbin");

        Assert.Equal("joe W.", shortened["joe Whelan"]);
        Assert.Equal("Joe C.", shortened["Joe Corbin"]);
    }

    [Fact]
    public void Keeps_a_forename_that_only_looks_shared_because_of_a_middle_name()
    {
        var shortened = Shorten("Joe Whelan", "Ana Joe Corbin");

        Assert.Equal("Joe", shortened["Joe Whelan"]);
        Assert.Equal("Ana", shortened["Ana Joe Corbin"]);
    }

    [Fact]
    public void Maps_a_repeated_name_once()
    {
        var shortened = Shorten("Joe Whelan", "Joe Whelan");

        Assert.Equal(new KeyValuePair<string, string>("Joe Whelan", "Joe"), Assert.Single(shortened));
    }

    [Fact]
    public void Shortens_nothing_when_given_nobody()
    {
        Assert.Empty(Shorten());
    }

    [Fact]
    public void Rejects_a_null_argument()
    {
        Assert.Throws<ArgumentNullException>(() => PlayerNameShortener.Shorten(null!));
    }

    private static IReadOnlyDictionary<string, string> Shorten(params string[] players) =>
        PlayerNameShortener.Shorten(players);
}
