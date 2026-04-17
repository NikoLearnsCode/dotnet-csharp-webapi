using System.Text.RegularExpressions;

namespace dotnet_backend_2.Helpers;

public static partial class StringUtils
{
    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphensRegex();
    public static string GenerateSlug(string text)
    {

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var slug = text.ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o")
            .Replace(" ", "-");

        slug = NonAlphanumericRegex().Replace(slug, "");
        slug = MultipleHyphensRegex().Replace(slug, "-");

        return slug;
    }
}
