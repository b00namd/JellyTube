# Build script for Jellyfin.Plugin.JellyTube
# Creates the plugin ZIP and manifest.json for installation via Jellyfin dashboard.

param(
    [string]$Version = "1.0.0.0",
    [string]$BaseUrl = "https://raw.githubusercontent.com/b00namd/JellyTube/master/dist",
    [string]$RepoUrl = "https://raw.githubusercontent.com/b00namd/JellyTube/master"
)

$ErrorActionPreference = "Stop"

$ProjectDir  = Join-Path $PSScriptRoot "Jellyfin.Plugin.JellyTube"
$OutputDir   = Join-Path $PSScriptRoot "dist"
$PublishDir  = Join-Path $OutputDir "publish"
$ZipName     = "Jellyfin.Plugin.JellyTube_$($Version).zip"
$ZipPath     = Join-Path $OutputDir $ZipName
$ManifestPath = Join-Path $OutputDir "manifest.json"

# --- 1. Clean ---
Write-Host "Cleaning output directory..." -ForegroundColor Cyan
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $PublishDir | Out-Null

# --- 2. Build (Release) ---
Write-Host "Building plugin (Release)..." -ForegroundColor Cyan
dotnet publish "$ProjectDir" -c Release -o "$PublishDir" --no-self-contained
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Remove Jellyfin's own DLLs – they must NOT be in the plugin ZIP
$jellyfinDlls = @(
    "MediaBrowser.Common.dll",
    "MediaBrowser.Controller.dll",
    "MediaBrowser.Model.dll",
    "Microsoft.Extensions.*",
    "Newtonsoft.Json.dll"
)
foreach ($pattern in $jellyfinDlls) {
    Get-ChildItem $PublishDir -Filter $pattern | Remove-Item -Force
}

# --- 3. ZIP ---
Write-Host "Creating ZIP: $ZipName ..." -ForegroundColor Cyan
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force

# --- 4. Checksum (MD5) ---
$md5  = (Get-FileHash $ZipPath -Algorithm MD5).Hash.ToLower()
$size = (Get-Item $ZipPath).Length
Write-Host "MD5: $md5  ($size bytes)" -ForegroundColor Gray

# --- 5. manifest.json ---
Write-Host "Writing manifest.json..." -ForegroundColor Cyan
$timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")

$manifest = @(
    @{
        category    = "General"
        guid        = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
        name        = "JellyTube"
        description = "YouTube-Videos und Playlists direkt in die Jellyfin-Mediathek herunterladen."
        overview    = "Verwendet yt-dlp zum Herunterladen von YouTube-Inhalten und erstellt NFO-Metadaten sowie Vorschaubilder."
        owner       = "local"
        imageUrl    = "$RepoUrl/Jellyfin.Plugin.JellyTube/thumb.png"
        versions    = @(
            @{
                version    = $Version
                changelog  = "Initiale Version"
                targetAbi  = "10.9.0.0"
                sourceUrl  = "$BaseUrl/$ZipName"
                checksum   = $md5
                timestamp  = $timestamp
            }
        )
    }
)

# Jellyfin expects the manifest as a top-level JSON array
$json = ConvertTo-Json -InputObject $manifest -Depth 5
[System.IO.File]::WriteAllText($ManifestPath, $json, [System.Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host " Build completed successfully!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host " ZIP:      $ZipPath"
Write-Host " Manifest: $ManifestPath"
Write-Host ""
Write-Host " Next step: run serve.ps1 to start the local" -ForegroundColor Yellow
Write-Host " repository server, then add the URL to Jellyfin." -ForegroundColor Yellow
Write-Host ""
