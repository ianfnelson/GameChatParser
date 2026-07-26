using GameChatParser.Core.Reporting;

namespace GameChatParser.Tests.Reporting;

public class MarkdownReportRendererTests
{
    private readonly MarkdownReportRenderer _renderer = new();

    [Fact]
    public void Wraps_each_table_in_a_heading_and_a_fenced_block()
    {
        var output = Render(Board("Wordle", "July", Entry(1, false, "Joe Whelan", 26, 4.423d)));

        Assert.Equal(
            """
            *Wordle — July*
            ```
            1. Joe Whelan        26    4.423
            ```

            """,
            output);
    }

    [Fact]
    public void Lays_the_columns_out_the_way_the_family_reads_them()
    {
        var output = Render(Board(
            "Wordle",
            "2025",
            Entry(1, false, "Nadia Corbin", 33, 3.273d),
            Entry(2, false, "Carol Whelan", 365, 3.819d),
            Entry(3, false, "Rosalind Ferrer", 229, 4.445d)));

        Assert.Equal(
            """
            *Wordle — 2025*
            ```
            1. Nadia Corbin      33    3.273
            2. Carol Whelan     365    3.819
            3. Rosalind Ferrer  229    4.445
            ```

            """,
            output);
    }

    [Fact]
    public void Marks_a_shared_position_with_an_equals_sign()
    {
        var output = Render(Board(
            "Wordle",
            "July",
            Entry(1, true, "Ana", 10, 4d),
            Entry(1, true, "Bea", 10, 4d),
            Entry(3, false, "Theo", 10, 5d)));

        Assert.Equal(
            """
            *Wordle — July*
            ```
            1= Ana               10    4.000
            1= Bea               10    4.000
            3. Theo              10    5.000
            ```

            """,
            output);
    }

    [Fact]
    public void Widens_the_name_column_to_fit_a_long_name()
    {
        var output = Render(Board(
            "Wordle",
            "July",
            Entry(1, false, "Bartholomew Cuthbertson", 10, 4d)));

        Assert.Contains("1. Bartholomew Cuthbertson   10    4.000", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Widens_the_position_column_past_nine_players()
    {
        var output = Render(Board(
            "Wordle",
            "July",
            Entry(1, false, "Ana", 10, 4d),
            Entry(10, false, "Theo", 10, 5d)));

        Assert.Equal(
            """
            *Wordle — July*
            ```
            1.  Ana               10    4.000
            10. Theo              10    5.000
            ```

            """,
            output);
    }

    [Fact]
    public void Keeps_a_negative_average_aligned()
    {
        var output = Render(Board(
            "Connections",
            "July",
            Entry(1, false, "Ana", 10, 24.5d),
            Entry(2, false, "Theo", 1000, -4d)));

        Assert.Equal(
            """
            *Connections — July*
            ```
            1. Ana               10   24.500
            2. Theo            1000   -4.000
            ```

            """,
            output);
    }

    [Fact]
    public void Renders_a_leaderboard_nobody_appears_on()
    {
        Assert.Equal(
            """
            *Wordle — July*
            ```
            ```

            """,
            Render(Board("Wordle", "July")));
    }

    [Fact]
    public void Renders_every_leaderboard_in_the_order_given()
    {
        var report = new Report
        {
            Leaderboards =
            [
                Board("Wordle", "2026", Entry(1, false, "Bea", 1, 4d)),
                Board("Connections", "2026", Entry(1, false, "Bea", 1, 40d))
            ]
        };

        var writer = new StringWriter { NewLine = "\n" };
        _renderer.Render(report, writer);

        Assert.Equal(
            ["*Wordle — 2026*", "*Connections — 2026*"],
            writer.ToString().Split('\n').Where(line => line.StartsWith('*')));
    }

    [Fact]
    public void Renders_nothing_for_a_report_with_no_leaderboards()
    {
        var writer = new StringWriter { NewLine = "\n" };

        _renderer.Render(new Report { Leaderboards = [] }, writer);

        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        var writer = new StringWriter();

        Assert.Throws<ArgumentNullException>(() => _renderer.Render((Report)null!, writer));
        Assert.Throws<ArgumentNullException>(() => _renderer.Render(Board("Wordle", "July"), null!));
    }

    private string Render(Leaderboard leaderboard)
    {
        var writer = new StringWriter { NewLine = "\n" };

        _renderer.Render(leaderboard, writer);

        return writer.ToString();
    }

    private static Leaderboard Board(string game, string period, params LeaderboardEntry[] entries) => new()
    {
        GameName = game,
        PeriodName = period,
        PeriodKind = PeriodKind.Month,
        PeriodIndex = 0,
        Entries = entries
    };

    private static LeaderboardEntry Entry(int position, bool isTied, string player, int played, double average) => new()
    {
        Position = position,
        IsTied = isTied,
        Player = player,
        Played = played,
        Average = average
    };
}
