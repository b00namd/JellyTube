using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Plugin.JellyTube.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyTube.Services;

/// <summary>
/// Writes Kodi/Jellyfin-compatible <c>.nfo</c> metadata files for downloaded videos.
/// </summary>
public class NfoWriterService
{
    private readonly ILogger<NfoWriterService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NfoWriterService"/> class.
    /// </summary>
    public NfoWriterService(ILogger<NfoWriterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Writes an NFO file to <paramref name="nfoPath"/> using the provided <paramref name="meta"/>.
    /// </summary>
    public async Task WriteNfoAsync(VideoMetadata meta, string nfoPath)
    {
        try
        {
            var elements = new XElement("movie",
                new XElement("title", meta.Title),
                new XElement("originaltitle", meta.Title),
                new XElement("plot", meta.Description),
                new XElement("year", meta.UploadDate?.Year.ToString() ?? string.Empty),
                new XElement("premiered", meta.UploadDate?.ToString("yyyy-MM-dd") ?? string.Empty),
                new XElement("studio", meta.ChannelName),
                new XElement("runtime", ((int)((meta.DurationSeconds ?? 0) / 60)).ToString()),
                new XElement("uniqueid",
                    new XAttribute("type", "youtube"),
                    new XAttribute("default", "true"),
                    meta.VideoId),
                new XElement("source", meta.WebpageUrl)
            );

            // One <genre> per category
            foreach (var cat in meta.Categories)
            {
                elements.Add(new XElement("genre", cat));
            }

            // One <tag> per YouTube tag (limit to 20 to keep NFO reasonable)
            foreach (var tag in meta.Tags.Take(20))
            {
                elements.Add(new XElement("tag", tag));
            }

            // Thumbnail / poster
            if (!string.IsNullOrEmpty(meta.ThumbnailUrl))
            {
                elements.Add(new XElement("thumb",
                    new XAttribute("aspect", "poster"),
                    meta.ThumbnailUrl));

                elements.Add(new XElement("fanart",
                    new XElement("thumb", meta.ThumbnailUrl)));
            }

            var doc = new XDocument(elements);

            Directory.CreateDirectory(Path.GetDirectoryName(nfoPath)!);

            await using var writer = new StreamWriter(nfoPath, append: false, Encoding.UTF8);
            await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\" ?>");
            await writer.WriteAsync(doc.ToString());

            _logger.LogInformation("NFO written to {Path}", nfoPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write NFO to {Path}", nfoPath);
        }
    }
}
