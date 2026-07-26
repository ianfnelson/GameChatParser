using GameChatParser.Core.Chat;

namespace GameChatParser.Tests.Chat;

public class WhatsAppChatReaderTests
{
    private readonly WhatsAppChatReader _reader = new();

    [Fact]
    public void Reads_sender_and_text_from_a_single_line_message()
    {
        var messages = Read("[21/12/2024, 20:38:25] Carol Whelan: OK can see it.");

        var message = Assert.Single(messages);
        Assert.Equal("Carol Whelan", message.Sender);
        Assert.Equal("OK can see it.", message.Text);
    }

    [Fact]
    public void Parses_the_timestamp()
    {
        var messages = Read("[21/12/2024, 20:38:25] Carol Whelan: OK can see it.");

        Assert.Equal(new DateTime(2024, 12, 21, 20, 38, 25), Assert.Single(messages).Timestamp);
    }

    [Fact]
    public void Parses_a_twelve_hour_timestamp()
    {
        var messages = Read("[21/12/2024, 8:38:25 pm] Carol Whelan: OK can see it.");

        Assert.Equal(new DateTime(2024, 12, 21, 20, 38, 25), Assert.Single(messages).Timestamp);
    }

    [Fact]
    public void Leaves_the_timestamp_unset_when_it_cannot_be_understood()
    {
        var messages = Read("[whenever] Carol Whelan: OK can see it.");

        var message = Assert.Single(messages);
        Assert.Null(message.Timestamp);
        Assert.Equal("Carol Whelan", message.Sender);
    }

    [Fact]
    public void Keeps_continuation_lines_with_the_message_that_started_them()
    {
        var messages = Read(
            """
            [22/12/2024, 08:15:47] Carol Whelan: Wordle 1,282 3/6

            🟨⬜⬜⬜⬜
            ⬜🟩⬜🟨⬜
            🟩🟩🟩🟩🟩
            """);

        var message = Assert.Single(messages);
        Assert.Equal(
            ["Wordle 1,282 3/6", string.Empty, "🟨⬜⬜⬜⬜", "⬜🟩⬜🟨⬜", "🟩🟩🟩🟩🟩"],
            message.Lines);
    }

    [Fact]
    public void Starts_a_new_message_at_the_next_header()
    {
        var messages = Read(
            """
            [21/12/2024, 21:13:41] Gregory Whelan: Yep!
            [21/12/2024, 21:13:57] Carol Whelan: Bring it on!!!!
            """);

        Assert.Equal(2, messages.Count);
        Assert.Equal("Gregory Whelan", messages[0].Sender);
        Assert.Equal("Yep!", messages[0].Text);
        Assert.Equal("Carol Whelan", messages[1].Sender);
        Assert.Equal("Bring it on!!!!", messages[1].Text);
    }

    [Fact]
    public void Yields_the_final_message_at_the_end_of_the_export()
    {
        var messages = Read(
            """
            [21/12/2024, 21:13:41] Gregory Whelan: Connections
            Puzzle #619
            """);

        Assert.Equal("Connections\nPuzzle #619", Assert.Single(messages).Text);
    }

    [Fact]
    public void Ignores_lines_before_the_first_header()
    {
        var messages = Read(
            """
            stray line belonging to nobody
            [21/12/2024, 21:13:41] Gregory Whelan: Yep!
            """);

        Assert.Equal("Yep!", Assert.Single(messages).Text);
    }

    [Fact]
    public void Reads_a_header_carrying_a_left_to_right_mark()
    {
        var messages = Read("‎[21/12/2024, 20:22:03] Puzzle Pals: ‎You created this group");

        var message = Assert.Single(messages);
        Assert.Equal("Puzzle Pals", message.Sender);
        Assert.Equal("‎You created this group", message.Text);
    }

    [Fact]
    public void Splits_on_the_first_colon_so_text_may_contain_others()
    {
        var messages = Read("[21/12/2024, 20:38:25] Joe Whelan: link: https://www.nytimes.com/games/connections");

        var message = Assert.Single(messages);
        Assert.Equal("Joe Whelan", message.Sender);
        Assert.Equal("link: https://www.nytimes.com/games/connections", message.Text);
    }

    [Fact]
    public void Reads_a_message_with_an_empty_body()
    {
        var messages = Read("[21/12/2024, 20:38:25] Joe Whelan:");

        var message = Assert.Single(messages);
        Assert.Equal("Joe Whelan", message.Sender);
        Assert.Equal(string.Empty, message.Text);
    }

    [Fact]
    public void Reads_nothing_from_an_empty_export()
    {
        Assert.Empty(_reader.Read([]));
    }

    [Fact]
    public void Reads_nothing_from_an_export_with_no_headers()
    {
        Assert.Empty(Read("just some text\nand some more"));
    }

    [Fact]
    public void Rejects_a_null_source()
    {
        Assert.Throws<ArgumentNullException>(() => _reader.Read(null!).ToList());
    }

    private List<ChatMessage> Read(string text) => _reader.Read(TestChat.Lines(text)).ToList();
}
