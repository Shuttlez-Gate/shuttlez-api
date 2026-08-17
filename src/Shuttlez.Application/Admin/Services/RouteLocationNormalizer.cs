using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Shuttlez.Application.Admin.Services;

public static partial class RouteLocationNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        trimmed = CollapseSpaces().Replace(trimmed, " ");
        trimmed = trimmed.ToLowerInvariant();

        return trimmed.Normalize(NormalizationForm.FormC);
    }

    public static string BuildRouteKey(string fromNormalized, string toNormalized)
    {
        if (string.IsNullOrWhiteSpace(fromNormalized) || string.IsNullOrWhiteSpace(toNormalized))
        {
            return string.Empty;
        }

        var ordered = new[] { fromNormalized, toNormalized }.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return string.Join('|', ordered);
    }

    public static string BuildDisplayLabel(string fromLabel, string toLabel)
    {
        return $"{fromLabel.Trim()} ↔ {toLabel.Trim()}";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseSpaces();
}
