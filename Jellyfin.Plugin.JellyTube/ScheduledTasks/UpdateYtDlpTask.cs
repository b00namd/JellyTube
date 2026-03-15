using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using YoutubeDLSharp;

namespace Jellyfin.Plugin.JellyTube.ScheduledTasks;

/// <summary>
/// Jellyfin scheduled task that downloads the latest yt-dlp binary.
/// Runs weekly by default.
/// </summary>
public class UpdateYtDlpTask : IScheduledTask
{
    private readonly ILogger<UpdateYtDlpTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateYtDlpTask"/> class.
    /// </summary>
    public UpdateYtDlpTask(ILogger<UpdateYtDlpTask> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Update yt-dlp Binary";

    /// <inheritdoc />
    public string Key => "JellyTubeUpdateYtDlp";

    /// <inheritdoc />
    public string Description => "Downloads the latest yt-dlp binary so videos can always be fetched.";

    /// <inheritdoc />
    public string Category => "JellyTube";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = DayOfWeek.Sunday,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken ct)
    {
        // Only auto-update when no custom binary path is configured
        var config = Plugin.Instance!.Configuration;
        if (!string.IsNullOrWhiteSpace(config.YtDlpBinaryPath))
        {
            _logger.LogInformation(
                "Custom yt-dlp binary path is set ({Path}), skipping auto-update.",
                config.YtDlpBinaryPath);
            progress.Report(100);
            return;
        }

        _logger.LogInformation("Downloading latest yt-dlp binary…");

        try
        {
            await Utils.DownloadYtDlp();
            _logger.LogInformation("yt-dlp binary updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update yt-dlp binary.");
        }

        progress.Report(100);
    }
}
