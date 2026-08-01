using GameChatParser.Core.Chat;
using GameChatParser.Core.Games;
using GameChatParser.Core.Reporting;
using GameChatParser.Core.Scoring;

namespace GameChatParser.Tests.Reporting;

public class ReportBuilderTests
{
    /// <summary>
    /// A chat spanning the new year, so that both games have two years and two months to
    /// report on. Wordle 1,640 and Connections #918 both fall on 15 December 2025;
    /// Wordle 1,666 and Connections #944 both fall on 10 January 2026.
    /// </summary>
    private const string SampleChat =
        """
        [15/12/2025, 08:01:00] Joe Whelan: Wordle 1,640 3/6

        🟨⬜⬜⬜⬜
        ⬜🟩⬜🟨⬜
        🟩🟩🟩🟩🟩
        [15/12/2025, 08:02:00] Carol Whelan: Wordle 1,640 5/6
        [15/12/2025, 08:03:00] Joe Whelan: Connections
        Puzzle #918
        🟩🟩🟩🟩
        🟨🟨🟨🟨
        🟦🟦🟦🟦
        🟪🟪🟪🟪
        [15/12/2025, 08:04:00] Carol Whelan: Connections
        Puzzle #918
        🟩🟦🟨🟨
        🟩🟩🟩🟩
        🟨🟨🟨🟨
        🟦🟦🟦🟦
        🟪🟪🟪🟪
        [16/12/2025, 08:01:00] Joe Whelan: Wordle 1,641 X/6
        [16/12/2025, 08:02:00] Carol Whelan: Wordle 1,641 3/6
        [16/12/2025, 08:05:00] Carol Whelan: Morning all, tough one today
        [10/01/2026, 08:01:00] Joe Whelan: Wordle 1,666 2/6
        [10/01/2026, 08:02:00] Carol Whelan: Wordle 1,666 4/6
        [10/01/2026, 08:03:00] Joe Whelan: Connections
        Puzzle #944
        🟩🟦🟨🟨
        🟩🟦🟩🟩
        🟩🟦🟩🟩
        🟩🟦🟨🟩
        [11/01/2026, 08:01:00] Carol Whelan: Wordle 1,667 4/6
        """;

    [Fact]
    public void Keeps_a_games_periods_together_and_runs_the_games_in_name_order()
    {
        var report = Build(SampleChat);

        Assert.Equal(
            [
                "Connections — 2026",
                "Connections — 2025",
                "Connections — January",
                "Connections — December",
                "Wordle — 2026",
                "Wordle — 2025",
                "Wordle — January",
                "Wordle — December"
            ],
            report.Leaderboards.Select(board => board.Title));
    }

    [Fact]
    public void Ranks_wordle_on_the_fewest_guesses()
    {
        var december = Find(Build(SampleChat), "Wordle — December");

        Assert.Equal(["Carol Whelan", "Joe Whelan"], december.Entries.Select(entry => entry.Player));
        Assert.Equal([4d, 5d], december.Entries.Select(entry => entry.Average));
        Assert.Equal([2, 2], december.Entries.Select(entry => entry.Played));
    }

    [Fact]
    public void Ranks_connections_on_the_fewest_faults()
    {
        var december = Find(Build(SampleChat), "Connections — December");

        Assert.Equal(["Joe Whelan", "Carol Whelan"], december.Entries.Select(entry => entry.Player));
        Assert.Equal([0d, 1d], december.Entries.Select(entry => entry.Average));
    }

    [Fact]
    public void Keeps_the_two_games_scores_apart()
    {
        var report = Build(SampleChat);

        Assert.Equal(2, Find(report, "Wordle — January").Entries.Count);
        Assert.Equal(8d, Assert.Single(Find(report, "Connections — January").Entries).Average);
    }

    [Fact]
    public void Ignores_chat_that_holds_no_results()
    {
        var report = Build(
            """
            [15/12/2025, 08:05:00] Carol Whelan: Morning all
            [15/12/2025, 08:06:00] Joe Whelan: Nothing to report
            """);

        Assert.Empty(report.Leaderboards);
    }

    [Fact]
    public void Counts_a_message_holding_two_games_for_both_of_them()
    {
        var report = Build(
            """
            [15/12/2025, 08:01:00] Joe Whelan: Wordle 1,640 3/6
            Connections
            Puzzle #918
            🟩🟩🟩🟩
            🟨🟨🟨🟨
            🟦🟦🟦🟦
            🟪🟪🟪🟪
            """);

        Assert.Equal(3d, Assert.Single(Find(report, "Wordle — December").Entries).Average);
        Assert.Equal(0d, Assert.Single(Find(report, "Connections — December").Entries).Average);
    }

    [Fact]
    public void Reports_a_game_that_was_added_without_touching_anything_else()
    {
        // Standing in for a future game: nothing but an IGame implementation is needed
        // for it to be grouped, ranked and rendered alongside the others.
        var quordle = new FakeGame(
            "Quordle",
            RankingDirection.LowerIsBetter,
            message => message.Text.StartsWith("Quordle", StringComparison.Ordinal)
                ? new GameScore(message.Sender, 1, new DateOnly(2026, 1, 10), 6)
                : null);

        var builder = new ReportBuilder(
            new WhatsAppChatReader(),
            [new Core.Games.Wordle.WordleGame(), quordle],
            new LeaderboardBuilder());

        var report = builder.Build(TestChat.Lines(
            """
            [10/01/2026, 08:01:00] Joe Whelan: Wordle 1,666 2/6
            [10/01/2026, 08:02:00] Joe Whelan: Quordle 1063
            """));

        Assert.Equal(
            ["Quordle — 2026", "Quordle — January", "Wordle — 2026", "Wordle — January"],
            report.Leaderboards.Select(board => board.Title));
        Assert.Equal(6d, Assert.Single(Find(report, "Quordle — January").Entries).Average);
    }

    [Fact]
    public void Reports_nothing_when_no_games_are_registered()
    {
        var builder = new ReportBuilder(new WhatsAppChatReader(), [], new LeaderboardBuilder());

        Assert.Empty(builder.Build(TestChat.Lines(SampleChat)).Leaderboards);
    }

    [Fact]
    public void Rejects_a_null_chat()
    {
        Assert.Throws<ArgumentNullException>(() => new ReportBuilder().Build(null!));
    }

    private static Report Build(string chat) => new ReportBuilder().Build(TestChat.Lines(chat));

    private static Leaderboard Find(Report report, string title) =>
        Assert.Single(report.Leaderboards, board => board.Title == title);
}
