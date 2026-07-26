namespace GameChatParser.Core.Chat;

/// <summary>
/// A single message from a group chat export. A message may span several lines,
/// because a shared game result carries its grid of coloured squares beneath the
/// summary line.
/// </summary>
public sealed record ChatMessage
{
    /// <summary>
    /// When the message was sent, where the export's stamp could be understood.
    /// Nothing in the ranking depends on this: puzzle dates are derived from the
    /// puzzle number instead, so a result pasted days late still counts for the
    /// day it belongs to.
    /// </summary>
    public required DateTime? Timestamp { get; init; }

    /// <summary>The display name of whoever sent the message.</summary>
    public required string Sender { get; init; }

    /// <summary>
    /// The body of the message, split by line. The first entry is the text that
    /// followed the sender's name; the rest are continuation lines.
    /// </summary>
    public required IReadOnlyList<string> Lines { get; init; }

    /// <summary>The body of the message as a single newline-separated string.</summary>
    public string Text => string.Join('\n', Lines);
}
