# Starts a minimal HTTP server that hosts the Jellyfin plugin repository.
# Keep this running while adding the repository in Jellyfin and installing the plugin.

param(
    [int]$Port = 8888
)

$DistDir = Join-Path $PSScriptRoot "dist"

if (-not (Test-Path $DistDir)) {
    Write-Host "ERROR: 'dist' folder not found. Run build.ps1 first." -ForegroundColor Red
    exit 1
}

$ManifestPath = Join-Path $DistDir "manifest.json"
if (-not (Test-Path $ManifestPath)) {
    Write-Host "ERROR: manifest.json not found in dist\. Run build.ps1 first." -ForegroundColor Red
    exit 1
}

$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add("http://+:$Port/")

try {
    $listener.Start()
}
catch {
    Write-Host "ERROR: Could not start HTTP listener on port $Port." -ForegroundColor Red
    Write-Host "Try running as Administrator, or choose a different port with -Port <number>." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host " Plugin repository server running!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host " Manifest URL (add this to Jellyfin):" -ForegroundColor Yellow
Write-Host "   http://localhost:$Port/manifest.json" -ForegroundColor Cyan
Write-Host ""
Write-Host " In Jellyfin Dashboard:" -ForegroundColor Yellow
Write-Host "   1. Admin Dashboard -> Plugins -> Repositories"
Write-Host "   2. Add repository URL: http://localhost:$Port/manifest.json"
Write-Host "   3. Save, then go to Catalog"
Write-Host "   4. Find 'YouTube Downloader' and install"
Write-Host "   5. Restart Jellyfin server"
Write-Host ""
Write-Host " Press Ctrl+C to stop the server." -ForegroundColor Gray
Write-Host ""

# Map MIME types
$mimeTypes = @{
    ".json" = "application/json"
    ".zip"  = "application/zip"
    ".html" = "text/html"
    ".txt"  = "text/plain"
}

while ($listener.IsListening) {
    $context = $null
    try {
        $context = $listener.GetContext()
    }
    catch {
        break
    }

    $request  = $context.Request
    $response = $context.Response

    $requestedPath = $request.Url.LocalPath.TrimStart('/')
    $filePath = Join-Path $DistDir $requestedPath

    Write-Host "$(Get-Date -Format 'HH:mm:ss')  $($request.HttpMethod) /$requestedPath" -ForegroundColor DarkGray

    if (Test-Path $filePath -PathType Leaf) {
        $ext  = [System.IO.Path]::GetExtension($filePath).ToLower()
        $mime = if ($mimeTypes.ContainsKey($ext)) { $mimeTypes[$ext] } else { "application/octet-stream" }

        $bytes = [System.IO.File]::ReadAllBytes($filePath)
        $response.ContentType   = $mime
        $response.ContentLength64 = $bytes.Length
        $response.StatusCode    = 200
        $response.OutputStream.Write($bytes, 0, $bytes.Length)
    }
    else {
        $msg  = [System.Text.Encoding]::UTF8.GetBytes("404 Not Found: $requestedPath")
        $response.StatusCode = 404
        $response.ContentType = "text/plain"
        $response.ContentLength64 = $msg.Length
        $response.OutputStream.Write($msg, 0, $msg.Length)
    }

    $response.OutputStream.Close()
}

$listener.Stop()
Write-Host "Server stopped." -ForegroundColor Gray
