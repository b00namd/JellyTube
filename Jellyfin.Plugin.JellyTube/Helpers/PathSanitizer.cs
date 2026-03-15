using System;
using System.IO;
using System.Text;

namespace Jellyfin.Plugin.JellyTube.Helpers;

/// <summary>
/// Utility for sanitizing strings to be safe for use as filesystem path segments.
/// </summary>
public static class PathSanitizer
{
    private const int MaxLength = 100;

    /// <summary>
    /// Replaces characters that are illegal on Windows and Linux filesystems with underscores
    /// and trims the result to a safe length.
    /// </summary>
    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "_unknown";
        }

        var illegalChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);

        foreach (var ch in name)
        {
            if (Array.IndexOf(illegalChars, ch) >= 0 || ch == '/')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(ch);
            }
        }

        var result = sb.ToString().Trim().Trim('.');

        if (result.Length > MaxLength)
        {
            result = result[..MaxLength];
        }

        return string.IsNullOrWhiteSpace(result) ? "_unknown" : result;
    }
}
