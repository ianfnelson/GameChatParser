namespace GameChatParser.Core.Chat;

/// <summary>
/// Turns the raw lines of a chat export into discrete messages. Implement this to
/// support an export format other than WhatsApp's.
/// </summary>
public interface IChatReader
{
    /// <summary>
    /// Reads messages from the export. Lines appearing before the first recognised
    /// message header are ignored, as they belong to no sender.
    /// </summary>
    IEnumerable<ChatMessage> Read(IEnumerable<string> lines);
}
