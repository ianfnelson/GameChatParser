using System.Globalization;
using System.Text.RegularExpressions;

namespace GameChatParser.Core.Chat;

/// <summary>
/// Reads a WhatsApp chat export, where each message begins with a bracketed
/// timestamp and a sender name, and any further lines belong to that same message
/// until the next header appears.
/// </summary>
public sealed partial class WhatsAppChatReader : IChatReader
{
    /// <summary>
    /// WhatsApp prefixes system messages, and occasionally ordinary ones, with a
    /// left-to-right mark that would otherwise defeat the anchor on the header.
    /// </summary>
    private const string LeftToRightMark = "‎";

    /// <summary>
    /// Some exports separate the time from an am/pm designator with a narrow
    /// no-break space, which no standard date format string will match.
    /// </summary>
    private const char NarrowNoBreakSpace = ' ';

    private static readonly string[] TimestampFormats =
    [
        "dd/MM/yyyy, HH:mm:ss",
        "dd/MM/yyyy, HH:mm",
        "dd/MM/yyyy, h:mm:ss tt",
        "dd/MM/yyyy, h:mm tt",
        "yyyy-MM-dd, HH:mm:ss",
        "yyyy-MM-dd, HH:mm"
    ];

    public IEnumerable<ChatMessage> Read(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        string? sender = null;
        DateTime? timestamp = null;
        var body = new List<string>();

        foreach (var line in lines)
        {
            var header = MessageHeader().Match(line);

            if (!header.Success)
            {
                // Not a new message, so this line continues the one in progress.
                // Anything before the first header belongs to nobody and is dropped.
                if (sender is not null)
                {
                    body.Add(line);
                }

                continue;
            }

            if (sender is not null)
            {
                yield return new ChatMessage { Timestamp = timestamp, Sender = sender, Lines = body.ToArray() };
            }

            sender = header.Groups["sender"].Value.Trim();
            timestamp = ParseTimestamp(header.Groups["timestamp"].Value);
            body.Clear();
            body.Add(header.Groups["text"].Value);
        }

        if (sender is not null)
        {
            yield return new ChatMessage { Timestamp = timestamp, Sender = sender, Lines = body.ToArray() };
        }
    }

    private static DateTime? ParseTimestamp(string value)
    {
        var normalised = value.Replace(NarrowNoBreakSpace, ' ').Trim();

        return DateTime.TryParseExact(
            normalised,
            TimestampFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var parsed)
            ? parsed
            : null;
    }

    /// <summary>
    /// Matches a message header such as <c>[19/02/2025, 07:04:53] Yvonne Clarke: Connections</c>.
    /// The sender pattern excludes colons so that it stops at the first one, leaving
    /// any colons in the message text untouched.
    /// </summary>
    [GeneratedRegex($@"^{LeftToRightMark}?\[(?<timestamp>[^\]]{{1,40}})\]\s(?<sender>[^:]{{1,100}}):\s?(?<text>.*)$")]
    private static partial Regex MessageHeader();
}
