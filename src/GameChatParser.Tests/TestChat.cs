using GameChatParser.Core.Chat;

namespace GameChatParser.Tests;

/// <summary>Helpers for building the chat input that the tests work from.</summary>
internal static class TestChat
{
    /// <summary>
    /// Splits a chat export written as a raw string literal into lines. Both line ending
    /// conventions are handled, so a checkout that normalises to CRLF still reads the
    /// same way.
    /// </summary>
    public static string[] Lines(string text) =>
        text.Split(["\r\n", "\n"], StringSplitOptions.None);

    /// <summary>Builds a single message directly, bypassing the reader.</summary>
    public static ChatMessage Message(string sender, string body, DateTime? timestamp = null) => new()
    {
        Timestamp = timestamp,
        Sender = sender,
        Lines = Lines(body)
    };
}
