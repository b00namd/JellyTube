using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyTubbing.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JellyTubbing.Api;

/// <summary>
/// REST API endpoints for the JellyTubbing plugin.
/// </summary>
[ApiController]
[Route("api/jellytubbing")]
[Authorize(Policy = "RequiresElevation")]
public class JellyTubbingController : ControllerBase
{
    private readonly InvidiousService _invidious;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyTubbingController"/> class.
    /// </summary>
    public JellyTubbingController(InvidiousService invidious)
    {
        _invidious = invidious;
    }

    /// <summary>
    /// Serves the embedded configuration page JavaScript.
    /// </summary>
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

    /// <summary>
    /// Tests whether the configured Invidious instance is reachable.
    /// </summary>
    [HttpGet("test-invidious")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestInvidious(CancellationToken ct)
    {
        var url = Plugin.Instance?.Configuration.InvidiousInstanceUrl ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            return Ok(new { reachable = false, message = "Keine Invidious-URL konfiguriert." });

        var ok = await _invidious.IsReachableAsync(ct);
        return Ok(new { reachable = ok, message = ok ? url : $"Nicht erreichbar: {url}" });
    }

    /// <summary>
    /// Checks whether yt-dlp is available on the server.
    /// </summary>
    [HttpGet("check-tools")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckTools()
    {
        var bin = Plugin.Instance?.Configuration.YtDlpBinaryPath;
        if (string.IsNullOrWhiteSpace(bin)) bin = "yt-dlp";

        var (available, version, error) = await TryGetVersionAsync(bin, "--version");
        return Ok(new { ytDlpAvailable = available, ytDlpVersion = version, ytDlpError = error });
    }

    private static async Task<(bool Available, string? Version, string? Error)> TryGetVersionAsync(string binary, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(binary, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi);
            if (proc is null) return (false, null, "Process.Start returned null");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var stdout = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);

            var version = stdout.Split('\n')[0].Trim();
            return (true, version, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}
