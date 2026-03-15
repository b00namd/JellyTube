using System;

namespace Jellyfin.Plugin.JellyTube.Models;

/// <summary>
/// Internal representation of YouTube video metadata, decoupled from YoutubeDLSharp types.
/// </summary>
public class VideoMetadata
{
    /// <summary>Gets or sets the YouTube video ID (e.g. "dQw4w9WgXcQ").</summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>Gets or sets the video title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the video description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the channel/uploader name.</summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>Gets or sets the channel ID.</summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the uploader URL.</summary>
    public string UploaderUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the upload date.</summary>
    public DateTime? UploadDate { get; set; }

    /// <summary>Gets or sets the duration in seconds.</summary>
    public double? DurationSeconds { get; set; }

    /// <summary>Gets or sets the view count.</summary>
    public long? ViewCount { get; set; }

    /// <summary>Gets or sets the like count.</summary>
    public long? LikeCount { get; set; }

    /// <summary>Gets or sets the thumbnail URL.</summary>
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the YouTube watch URL.</summary>
    public string WebpageUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the video tags.</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the video categories.</summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the playlist title, if the video is part of a playlist.</summary>
    public string? PlaylistTitle { get; set; }

    /// <summary>Gets or sets the index within a playlist.</summary>
    public int? PlaylistIndex { get; set; }
}
