using System.Text.RegularExpressions;

namespace LTKCC.Services;

public static class HtmlTemplateParameterExtractor
{
    // Captures tokens like {{APPLICATION}} and {{TZDATA_START_Australia/Sydney}}
    // Disallows whitespace inside the key, allows most other characters except braces.
    private static readonly Regex TokenRegex =
        new(@"\{\{\s*([^\{\}\s]+)\s*\}\}", RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractKeys(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();

        foreach (Match m in TokenRegex.Matches(html))
        {
            var key = m.Groups[1].Value.Trim();
            if (key.Length == 0) continue;

            if (seen.Add(key))
                ordered.Add(key);
        }

        return ordered;
    }
}
