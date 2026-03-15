using System;
using System.Globalization;

namespace Jellyfin.Plugin.JellyTube.Helpers;

/// <summary>
/// Utility for parsing yt-dlp date strings.
/// </summary>
public static class DateParser
{
    /// <summary>
    /// Parses a yt-dlp upload date string in <c>YYYYMMDD</c> format into a nullable <see cref="DateTime"/>.
    /// Returns <c>null</c> if the input is null, empty, or cannot be parsed.
    /// </summary>
    public static DateTime? ParseYtDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTime.TryParseExact(
                raw,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        return null;
    }
}
