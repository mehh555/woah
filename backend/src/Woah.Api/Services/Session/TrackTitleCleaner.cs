namespace Woah.Api.Services.Session;

public class TrackTitleCleaner : ITrackTitleCleaner
{
    private static readonly string[] ArtistSeparators =
        [" & ", ", ", " feat. ", " feat ", " ft. ", " ft ", " x ", " and ", " with "];

    public string CleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return title;

        var result = StripAllBrackets(title);

        result = CutFromEarliest(result, " feat. ", " feat ", " ft. ", " ft ", " - ");

        return result.Trim();
    }

    public string ExtractMainArtist(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist)) return artist;

        return CutFromEarliest(artist, ArtistSeparators).Trim();
    }

    private static string StripAllBrackets(string text)
    {
        var result = new char[text.Length];
        var pos = 0;
        var depth = 0;

        foreach (var ch in text)
        {
            if (ch is '(' or '[')
            {
                depth++;
                continue;
            }

            if (ch is ')' or ']')
            {
                if (depth > 0) depth--;
                continue;
            }

            if (depth == 0)
                result[pos++] = ch;
        }

        return new string(result, 0, pos);
    }

    private static string CutFromEarliest(string text, params string[] markers)
    {
        var earliest = text.Length;

        foreach (var marker in markers)
        {
            var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx > 0 && idx < earliest)
                earliest = idx;
        }

        return earliest < text.Length ? text[..earliest] : text;
    }
}