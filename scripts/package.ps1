param(
    [string]$Version = "1.0.0.0",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $root "Jellyfin.Plugin.MenuLinks"
$distDir = Join-Path $root "dist"
$packageDir = Join-Path $distDir "custom-menu-links"
$zipName = "custom-menu-links_$Version.zip"
$zipPath = Join-Path $distDir $zipName

Push-Location $root
try {
    dotnet build (Join-Path $root "Jellyfin.Plugin.MenuLinks.sln") --configuration $Configuration

    if (Test-Path $packageDir) {
        Remove-Item $packageDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $packageDir | Out-Null

    $dllSource = Join-Path $projectDir "bin\$Configuration\net9.0\Jellyfin.Plugin.MenuLinks.dll"
    Copy-Item $dllSource (Join-Path $packageDir "Jellyfin.Plugin.MenuLinks.dll")

    $meta = @{
        guid = "a3f8c2e1-7b4d-4a9e-8f1c-2d6e9a0b4c5f"
        name = "Custom Menu Links"
        overview = "Manage custom Jellyfin Web side menu links from the dashboard"
        description = "Provides a simple dashboard UI for adding, editing, and reordering custom side menu links in Jellyfin Web. Changes are written to the web client config.json automatically."
        owner = "pcmodder2001"
        category = "General"
        version = $Version
        targetAbi = "10.11.0.0"
        changelog = "Initial release"
        timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        status = "Active"
        autoUpdate = $true
        assemblies = @("Jellyfin.Plugin.MenuLinks.dll")
    }

    $metaPath = Join-Path $packageDir "meta.json"
    $meta | ConvertTo-Json -Depth 5 | Set-Content -Path $metaPath -Encoding UTF8

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath

    $md5 = (Get-FileHash $zipPath -Algorithm MD5).Hash.ToLowerInvariant()
    $md5Path = "$zipPath.md5"
    Set-Content -Path $md5Path -Value $md5 -NoNewline

    Write-Host "Created $zipPath"
    Write-Host "MD5: $md5"
}
finally {
    Pop-Location
}
