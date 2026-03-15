using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyTube.Services;

/// <summary>
/// Downloads video thumbnails and channel poster images to disk.
/// </summary>
public class ThumbnailService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ThumbnailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailService"/> class.
    /// </summary>
    public ThumbnailService(IHttpClientFactory httpClientFactory, ILogger<ThumbnailService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Downloads an image from <paramref name="url"/> and saves it to <paramref name="destPath"/>.
    /// Failures are logged as warnings and do not throw.
    /// </summary>
    public async Task DownloadThumbnailAsync(string url, string destPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("thumbnail");
            var bytes = await client.GetByteArrayAsync(url, ct);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            await File.WriteAllBytesAsync(destPath, bytes, ct);
            _logger.LogInformation("Thumbnail saved to {Path}", destPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download thumbnail from {Url}", url);
        }
    }

    /// <summary>
    /// Downloads <paramref name="thumbnailUrl"/> as <c>poster.jpg</c> inside <paramref name="channelDir"/>
    /// only if no poster already exists.
    /// </summary>
    public async Task EnsureChannelPosterAsync(string channelDir, string thumbnailUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            return;
        }

        var posterPath = Path.Combine(channelDir, "poster.jpg");
        if (!File.Exists(posterPath))
        {
            await DownloadThumbnailAsync(thumbnailUrl, posterPath, ct);
        }
    }
}
