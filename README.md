# Jellyfin Custom Web Plugins

Two Jellyfin server plugins in one repository — install both from the same plugin catalog URL.

| Plugin | What it does |
|--------|----------------|
| **Custom Menu Links** | Manage custom side menu links in Jellyfin Web (writes `config.json`) |
| **Custom Login Buttons** | Add a sign-up button and custom forgot-password link on the login page |

---

## Custom Menu Links

A dashboard UI for managing custom side menu links in Jellyfin Web — no more hand-editing `config.json`.

### What it does

Jellyfin Web supports custom navigation links via the `menuLinks` array in `config.json`. This plugin:

- Lets you add, edit, remove, and reorder links from **Dashboard → Custom Menu Links**
- Writes changes to the web client's `config.json` automatically
- Imports existing `menuLinks` on first install if the plugin config is empty

Each link has:

| Field | Required | Description |
|-------|----------|-------------|
| Name  | Yes      | Label shown in the side menu |
| URL   | Yes      | Destination (opens in a new tab) |
| Icon  | No       | [Material Design Icon](https://jossef.github.io/material-design-icons-iconfont/) name (defaults to `link`) |

---

## Custom Login Buttons

A simple settings page for two login-page buttons — no JavaScript bundle editing required.

### What it does

- **Sign up button** — link to your registration page (jfa-go, Authelia, etc.)
- **Forgot password button** — replace Jellyfin's built-in reset flow with your own URL

Settings are synced to **Dashboard → General → Branding** (login disclaimer + a small CSS block). The plugin merges its CSS with any existing custom CSS using markers, so your other branding CSS is preserved.

Configure under **Dashboard → Custom Login Buttons**:

| Field | Description |
|-------|-------------|
| Enable sign-up button | Show/hide the sign-up link |
| Sign-up URL | e.g. `https://accounts.example.com/signup` |
| Reset password URL | When set, hides Jellyfin's default forgot-password button |
| Optional text | Message shown above the buttons |

After saving, hard-refresh the **login page** (Ctrl+Shift+R).

**Note:** If you also edit **Dashboard → General → Branding → Login disclaimer** manually, avoid removing the `<!-- BEGIN Jellyfin.Plugin.LoginButtons -->` block — the plugin manages that section.

---

## Requirements

- Jellyfin Server **10.10+** (10.10 uses .NET 8, 10.11+ uses .NET 9)
- Jellyfin Web **10.8+** (menu links; login disclaimer support)

Check your Jellyfin version on Ubuntu:

```bash
dpkg -l jellyfin 2>/dev/null | grep jellyfin || jellyfin --version
```

If a plugin shows **NotSupported**, install the build matching your server version (**v1.0.0.3** or later for Jellyfin 10.11.x).

## Build

```bash
dotnet build Jellyfin.Plugin.MenuLinks.sln --configuration Release
```

Output DLLs:

```
Jellyfin.Plugin.MenuLinks/bin/Release/net9.0/Jellyfin.Plugin.MenuLinks.dll
Jellyfin.Plugin.LoginButtons/bin/Release/net9.0/Jellyfin.Plugin.LoginButtons.dll
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

The settings page is in the **admin Dashboard** (not the regular Jellyfin home screen).

### Open the settings page

**Option A — Dashboard sidebar (v1.0.0.4+)**

1. Open the **Dashboard** (wrench icon, admin account required)
2. Look in the left sidebar for **Custom Menu Links**
3. Click it to open the settings page

**Option B — Installed plugins list**

1. **Dashboard → Plugins → Installed**
2. Click **Custom Menu Links**
3. Open the configuration / settings section on that page

### Add your links

1. Click **Add Link** and fill in **Name**, **URL**, and optional **Icon**
2. Click **Save**
3. Refresh the Jellyfin Web client (Ctrl+Shift+R)

Your custom links then appear in the **regular Jellyfin side menu** below **Home** (not in the Dashboard).

## Links saved but not showing in the sidebar?

The plugin stores links in its own config, then writes them into Jellyfin Web's `config.json`. If the sidebar stays empty, the write step usually failed.

### 1. Check you are looking in the right place

Custom links appear on the **Jellyfin home page** (movies/libraries view), **not** in the admin Dashboard sidebar.

### 2. Fix file permissions (most common on Ubuntu)

On apt installs, `config.json` is often owned by root and the `jellyfin` user cannot write to it:

```bash
sudo chown jellyfin:jellyfin /usr/share/jellyfin/web/config.json
sudo chmod 664 /usr/share/jellyfin/web/config.json
sudo systemctl restart jellyfin
```

Then open **Custom Menu Links** settings, click **Save** again, and hard-refresh the Jellyfin home page (Ctrl+Shift+R).

### 3. Confirm the links were written

```bash
grep -A6 menuLinks /usr/share/jellyfin/web/config.json
```

You should see your link names and URLs in that section.

### 4. Check the Jellyfin log

```bash
sudo journalctl -u jellyfin -n 80 --no-pager | grep -iE "menu link|config.json|Custom Menu"
```

Look for `Synced` (success) or `denied` / `not found` (failure).

### 5. Docker or custom web path

If Jellyfin runs in Docker, set **Custom config.json path** in the plugin's Advanced settings to the path **inside the container**, for example `/jellyfin/jellyfin-web/config.json`, and ensure that file is writable by the jellyfin user.

**v1.0.0.6+** shows the last sync result directly on the settings page after you save.

## Install failed ("An error occurred while installing the plugin")?

The release zip and checksums are correct — this error is almost always on the server side. Check the Jellyfin log for the real cause:

```bash
sudo journalctl -u jellyfin -n 80 --no-pager | grep -iE "install|checksum|denied|plugin|Custom Menu"
```

Common causes and fixes:

| Log message | Fix |
|-------------|-----|
| `checksum ... doesn't match` | Jellyfin has a stale manifest. Remove the repo under **Plugins → Repositories**, re-add with the jsDelivr URL below, restart Jellyfin, try again. |
| `Access to the path ... is denied` | Fix plugin folder permissions (see below). |
| `Directory does not exist to extract to` | Delete leftover plugin folders, restart, install again. |

**Clean up old installs (Ubuntu):**

```bash
sudo systemctl stop jellyfin
sudo ls /var/lib/jellyfin/plugins/
# Remove any folder starting with "Custom Menu Links"
sudo rm -rf "/var/lib/jellyfin/plugins/Custom Menu Links"*
sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins
sudo systemctl start jellyfin
```

**Manual install (bypasses the catalog):**

```bash
sudo systemctl stop jellyfin
sudo mkdir -p "/var/lib/jellyfin/plugins/Custom Menu Links"
cd /tmp
curl -fL -o plugin.zip "https://github.com/pcmodder2001/jellyfin-plugin-menulinks/releases/download/v1.0.0.5/custom-menu-links_1.0.0.5_10.11.zip"
sudo unzip -o plugin.zip -d "/var/lib/jellyfin/plugins/Custom Menu Links"
sudo chown -R jellyfin:jellyfin "/var/lib/jellyfin/plugins/Custom Menu Links"
sudo systemctl start jellyfin
```

Use the `_10.10.zip` build only if your server is Jellyfin 10.10.x.

## Server won't start after installing?

A bad plugin can prevent Jellyfin from starting (502 Bad Gateway). To recover:

1. **Stop** Jellyfin
2. **Delete** the plugin folder (catalog installs use a version suffix, e.g. `Custom Menu Links_1.0.0.5`):
   - Windows: `%AppData%\jellyfin\plugins\Custom Menu Links*\`
   - Docker: `/config/plugins/Custom Menu Links*/`
   - Linux: `/var/lib/jellyfin/plugins/Custom Menu Links*/`
3. **Start** Jellyfin again

Then install **v1.0.0.1** or later, which fixes a startup crash caused by dependency injection.

## Plugin repository (install from Jellyfin catalog)

To install from **Dashboard → Plugins → Catalog**, host this project on GitHub and add your repository URL in Jellyfin.

### 1. Push to GitHub

```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/pcmodder2001/jellyfin-plugin-menulinks.git
git push -u origin main
```

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

### 3. Add the repository in Jellyfin (one-time setup)

1. Open **Dashboard → Plugins → Repositories**
2. Click **+**
3. Enter:
   - **Name:** `Jellyfin Custom Web Plugins`
   - **URL:** `https://github.com/pcmodder2001/jellyfin-plugin-menulinks/releases/latest/download/manifest.json`
4. Click **Save** and confirm the third-party plugin warning

**Use the GitHub Releases URL above** — it always points at the latest manifest and avoids CDN cache issues with `raw.githubusercontent.com` and jsDelivr `@main`.

Do **not** use:
- `https://raw.githubusercontent.com/.../manifest.json` (often stale)
- `https://cdn.jsdelivr.net/gh/.../manifest.json` (can cache old `@main` for hours at your CDN edge)

**If Jellyfin still shows an old version:** remove the repository entry, re-add it with the releases URL above, restart Jellyfin, then open **Plugins → Catalog** again.

### Install and update from the dashboard

Once the repository is added, Jellyfin handles everything from the UI:

1. **Install:** **Dashboard → Plugins → Catalog** → find **Custom Menu Links** → **Install**
2. **Update:** **Dashboard → Plugins → Installed** → select the plugin → **Update** (when a newer version is in the manifest)
3. **Auto-update (optional):** **Dashboard → Plugins → Installed** → enable auto-update on the plugin, or turn on automatic plugin updates in server settings

Jellyfin checks your configured repositories for new versions automatically. When a new release is published to GitHub, you should see an update available in **Installed** without doing anything manually.

If you have a broken install showing **NotSupported**, uninstall it from **Installed** first, then install again from **Catalog**.

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
"# jellyfin-plugin-menulinks" 
