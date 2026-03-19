using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyTubbing.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JellyTubbing.Api;

/// <summary>Request body for device poll endpoint.</summary>
public class DevicePollRequest
{
    /// <summary>Gets or sets the device code returned by oauth-device-start.</summary>
    public string DeviceCode { get; set; } = string.Empty;
}

/// <summary>
/// REST API endpoints for the JellyTubbing plugin.
/// </summary>
[ApiController]
[Route("api/jellytubbing")]
[Authorize(Policy = "RequiresElevation")]
public class JellyTubbingController : ControllerBase
{
    private readonly OAuthService _oauth;
    private readonly YouTubeApiService _youtube;
    private readonly StreamResolverService _resolver;
    private readonly ChannelSyncTask _sync;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyTubbingController"/> class.
    /// </summary>
    public JellyTubbingController(
        OAuthService oauth,
        YouTubeApiService youtube,
        StreamResolverService resolver,
        ChannelSyncTask sync)
    {
        _oauth    = oauth;
        _youtube  = youtube;
        _resolver = resolver;
        _sync     = sync;
    }

    // -----------------------------------------------------------------------
    // Config page UI
    // -----------------------------------------------------------------------

    /// <summary>Serves the embedded configuration page JavaScript.</summary>
    [HttpGet("ui")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetUiScript()
    {
        var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Jellyfin.Plugin.JellyTubbing.Configuration.configPage.js");
        if (stream is null) return NotFound();
        using var reader = new StreamReader(stream);
        return Content(reader.ReadToEnd(), "application/javascript");
    }

    // -----------------------------------------------------------------------
    // yt-dlp check
    // -----------------------------------------------------------------------

    /// <summary>Checks whether yt-dlp is available on the server.</summary>
    [HttpGet("check-tools")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckTools()
    {
        var bin = Plugin.Instance?.Configuration.YtDlpBinaryPath;
        if (string.IsNullOrWhiteSpace(bin)) bin = "yt-dlp";

        var (available, version, error) = await TryGetVersionAsync(bin, "--version");
        return Ok(new { ytDlpAvailable = available, ytDlpVersion = version, ytDlpError = error });
    }

    // -----------------------------------------------------------------------
    // OAuth2
    // -----------------------------------------------------------------------

    /// <summary>Starts the device authorization flow. Returns user_code and verification_url.</summary>
    [HttpPost("oauth-device-start")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartDeviceAuth(CancellationToken ct)
    {
        var config = Plugin.Instance?.Configuration;
        if (string.IsNullOrWhiteSpace(config?.OAuthClientId))
            return Ok(new { success = false, message = "OAuth-Client-ID nicht konfiguriert." });

        var result = await _oauth.StartDeviceAuthAsync(ct);
        if (result is null)
            return Ok(new { success = false, message = "Fehler beim Starten der Geraete-Authorisierung." });

        return Ok(new
        {
            success          = true,
            userCode         = result.UserCode,
            verificationUrl  = result.VerificationUrl,
            deviceCode       = result.DeviceCode,
            interval         = result.Interval,
            expiresIn        = result.ExpiresIn,
        });
    }

    /// <summary>Polls once for device authorization completion.</summary>
    [HttpPost("oauth-device-poll")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> PollDeviceAuth([FromBody] DevicePollRequest request, CancellationToken ct)
    {
        var status = await _oauth.PollDeviceAsync(request.DeviceCode, ct);
        return Ok(new { status });
    }

    /// <summary>Returns the current OAuth authorization status.</summary>
    [HttpGet("oauth-status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetOAuthStatus()
    {
        return Ok(new { authorized = _oauth.IsAuthorized });
    }

    /// <summary>Revokes the stored OAuth tokens.</summary>
    [HttpPost("oauth-revoke")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult RevokeOAuth()
    {
        _oauth.Revoke();
        return Ok(new { success = true });
    }

    // -----------------------------------------------------------------------
    // Subscriptions
    // -----------------------------------------------------------------------

    /// <summary>Returns the user's YouTube subscriptions (requires OAuth).</summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(CancellationToken ct)
    {
        if (!_oauth.IsAuthorized)
            return Ok(new { success = false, message = "Nicht mit Google verbunden." });

        var subs = await _youtube.GetSubscriptionsAsync(ct);
        var synced = Plugin.Instance?.Configuration.SyncedChannelIds ?? [];

        var result = subs.Select(s => new
        {
            channelId = s.Snippet.ResourceId.ChannelId,
            title     = s.Snippet.Title,
            thumbnail = s.Snippet.Thumbnails.BestUrl,
            synced    = synced.Contains(s.Snippet.ResourceId.ChannelId),
        });

        return Ok(new { success = true, subscriptions = result });
    }

    // -----------------------------------------------------------------------
    // Sync
    // -----------------------------------------------------------------------

    /// <summary>Triggers an immediate sync of all configured channels.</summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult TriggerSync(CancellationToken ct)
    {
        _ = _sync.ExecuteAsync(new Progress<double>(), ct);
        return Ok(new { success = true, message = "Synchronisation gestartet." });
    }

    // -----------------------------------------------------------------------
    // Stream redirect (STRM playback)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Resolves a YouTube video ID and streams it to the client.
    /// Combined streams (≤720p): 302 redirect (seeking supported).
    /// DASH streams (1080p+): ffmpeg pipe merging video+audio (-c copy, no re-encoding).
    /// </summary>
    [HttpGet("stream/{videoId}")]
    [AllowAnonymous]
    public async Task StreamVideo(string videoId, CancellationToken ct)
    {
        var (videoUrl, audioUrl) = await _resolver.ResolveUrlsAsync(videoId, ct);

        if (string.IsNullOrEmpty(videoUrl))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsync($"Stream fuer {videoId} konnte nicht aufgeloest werden.", ct);
            return;
        }

        // Combined stream: simple redirect – client can seek normally
        if (string.IsNullOrEmpty(audioUrl))
        {
            Response.Redirect(videoUrl);
            return;
        }

        // DASH: merge video + audio with ffmpeg stream copy (no re-encoding)
        var config  = Plugin.Instance!.Configuration;
        var ffmpeg  = string.IsNullOrWhiteSpace(config.FfmpegBinaryPath) ? "ffmpeg" : config.FfmpegBinaryPath;

        Response.ContentType = "video/x-matroska";
        Response.Headers["Content-Disposition"] = "inline; filename=\"stream.mkv\"";

        var psi = new ProcessStartInfo
        {
            FileName               = ffmpeg,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-loglevel"); psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-i");        psi.ArgumentList.Add(videoUrl);
        psi.ArgumentList.Add("-i");        psi.ArgumentList.Add(audioUrl);
        psi.ArgumentList.Add("-c");        psi.ArgumentList.Add("copy");
        psi.ArgumentList.Add("-f");        psi.ArgumentList.Add("matroska");
        psi.ArgumentList.Add("pipe:1");

        using var proc = new Process { StartInfo = psi };
        proc.Start();

        try
        {
            await proc.StandardOutput.BaseStream.CopyToAsync(Response.Body, ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected – expected
        }
        finally
        {
            if (!proc.HasExited)
            {
                proc.Kill();
                await proc.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static async Task<(bool Available, string? Version, string? Error)> TryGetVersionAsync(string binary, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(binary, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            using var proc = Process.Start(psi);
            if (proc is null) return (false, null, "Process.Start returned null");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var stdout = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            return (true, stdout.Split('\n')[0].Trim(), null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}
