namespace GameChatParser.Core.Reporting;

/// <summary>
/// Works out the shortest name each player can be shown under without being mistaken for
/// somebody else, so the tables spend no more horizontal room on names than they have to.
/// A forename on its own is enough for most players; where one is shared, the players
/// sharing it gain a surname initial, and where that still leaves two of them looking
/// alike, they are all shown under their whole name.
/// </summary>
public static class PlayerNameShortener
{
    /// <summary>
    /// Maps every name given to the form it should be displayed under. Names are compared
    /// against each other only, so the same player can shorten differently in a different
    /// company.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Shorten(IEnumerable<string> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        return players
            .Distinct(StringComparer.Ordinal)
            .GroupBy(Forename, StringComparer.OrdinalIgnoreCase)
            .SelectMany(ShortenGroup)
            .ToDictionary(StringComparer.Ordinal);
    }

    /// <summary>Shortens one set of players who all share a forename.</summary>
    private static IEnumerable<KeyValuePair<string, string>> ShortenGroup(IGrouping<string, string> sharingAForename)
    {
        var players = sharingAForename.ToList();

        if (players.Count == 1)
        {
            return [KeyValuePair.Create(players[0], Forename(players[0]))];
        }

        var withInitials = players
            .Select(player => KeyValuePair.Create(player, WithSurnameInitial(player)))
            .ToList();

        // Either the whole group gains an initial or none of it does: two people shown as
        // "Joe W." and "Joe Whelan" in the same table read as two spellings of one person.
        var isAmbiguous = withInitials
            .Select(shortened => shortened.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() < players.Count;

        return isAmbiguous
            ? players.Select(player => KeyValuePair.Create(player, player))
            : withInitials;
    }

    private static string WithSurnameInitial(string player)
    {
        var parts = Parts(player);

        return parts.Length > 1
            ? $"{parts[0]} {parts[^1][0]}."
            : Forename(player);
    }

    private static string Forename(string player)
    {
        var parts = Parts(player);

        return parts.Length > 0 ? parts[0] : player;
    }

    /// <summary>
    /// Splits a name into its words. The first is taken as the forename and the last as
    /// the surname, so a middle name is dropped rather than mistaken for the family name.
    /// </summary>
    private static string[] Parts(string player) =>
        player.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}
