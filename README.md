# Jellyfin Custom Menu Links Plugin

A Jellyfin server plugin that provides a dashboard UI for managing custom side menu links in Jellyfin Web — no more hand-editing `config.json`.

## What it does

Jellyfin Web supports custom navigation links via the `menuLinks` array in `config.json`. This plugin:

- Lets you add, edit, remove, and reorder links from **Dashboard → Plugins → Custom Menu Links**
- Writes changes to the web client's `config.json` automatically
- Imports existing `menuLinks` on first install if the plugin config is empty

Each link has:

| Field | Required | Description |
|-------|----------|-------------|
| Name  | Yes      | Label shown in the side menu |
| URL   | Yes      | Destination (opens in a new tab) |
| Icon  | No       | [Material Design Icon](https://jossef.github.io/material-design-icons-iconfont/) name (defaults to `link`) |

## Requirements

- Jellyfin Server **10.11+** (.NET 9)
- Jellyfin Web **10.8+** (for `menuLinks` support)

## Build

```bash
dotnet build Jellyfin.Plugin.MenuLinks.sln --configuration Release
```

The output DLL is at:

```
Jellyfin.Plugin.MenuLinks/bin/Release/net9.0/Jellyfin.Plugin.MenuLinks.dll
```

## Install

1. Copy `Jellyfin.Plugin.MenuLinks.dll` into your Jellyfin plugins folder, e.g.:
   - Linux: `/var/lib/jellyfin/plugins/Custom Menu Links/`
   - Windows: `%AppData%\jellyfin\plugins\Custom Menu Links\`
   - Docker: `/config/plugins/Custom Menu Links/`
2. Restart Jellyfin
3. Open **Dashboard → Plugins**, find **Custom Menu Links**, and configure

## Docker note

Some Docker images mount `config.json` from a custom location. If the plugin cannot find the default web config path, set the **Custom config.json path** field in the plugin settings (for example `/usr/share/jellyfin/web/config.json` on some images).

## Usage

1. Add your links in the plugin settings page
2. Click **Save**
3. Refresh the Jellyfin Web client (hard refresh if needed)

Links appear in the side menu below **Home**.

## Plugin repository (install from Jellyfin catalog)

To install from **Dashboard → Plugins → Catalog**, host this project on GitHub and add your repository URL in Jellyfin.

### 1. Push to GitHub

```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/jellyfin-plugin-menulinks.git
git push -u origin main
```

Replace `YOUR_USERNAME` with your GitHub username (and change the repo name if you prefer).

### 2. Publish a release

Either push a version tag:

```bash
git tag v1.0.0.0
git push origin v1.0.0.0
```

Or run the **Release Plugin** workflow manually from the GitHub Actions tab (enter version `1.0.0.0`).

The workflow will:

- Build and zip the plugin
- Create a GitHub Release with the zip + MD5 checksum
- Update `manifest.json` in your repo

### 3. Add the repository in Jellyfin

1. Open **Dashboard → Plugins → Repositories**
2. Click **+**
3. Enter:
   - **Name:** `Custom Menu Links` (or any name you like)
   - **URL:** `https://raw.githubusercontent.com/YOUR_USERNAME/jellyfin-plugin-menulinks/main/manifest.json`
4. Click **Save** and confirm the third-party plugin warning
5. Go to **Catalog**, find **Custom Menu Links**, and install

### Local packaging (optional)

To build a zip locally without GitHub Actions:

```powershell
.\scripts\package.ps1 -Version 1.0.0.0
```

Output goes to `dist/custom-menu-links_1.0.0.0.zip`.

## Development

This project follows the standard [Jellyfin plugin template](https://github.com/jellyfin/jellyfin-plugin-template) structure:

- `Plugin.cs` — plugin entry point and config page registration
- `ServerEntryPoint.cs` — syncs links on startup and when config changes
- `Services/WebConfigSyncService.cs` — reads/writes `menuLinks` in `config.json`
- `Configuration/configPage.html` — dashboard UI (embedded resource)
- `manifest.json` — plugin repository manifest for Jellyfin's catalog
- `.github/workflows/release.yml` — automated release + manifest updates
