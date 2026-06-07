using System.Text.Json;
using System.Text.Json.Nodes;
using Jellyfin.Plugin.MenuLinks.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MenuLinks.Services;

/// <summary>
/// Synchronizes menu links between plugin configuration and the web client config.json.
/// </summary>
public class WebConfigSyncService
{
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true
    };

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<WebConfigSyncService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebConfigSyncService"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public WebConfigSyncService(IApplicationPaths applicationPaths, ILogger<WebConfigSyncService> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the path to the web client config.json file.
    /// </summary>
    /// <param name="customWebConfigPath">Optional override path.</param>
    /// <returns>The resolved config.json path.</returns>
    public string ResolveConfigPath(string? customWebConfigPath)
    {
        if (!string.IsNullOrWhiteSpace(customWebConfigPath))
        {
            return customWebConfigPath.Trim();
        }

        return Path.Combine(_applicationPaths.WebPath, "config.json");
    }

    /// <summary>
    /// Writes menu links into the web client config.json file.
    /// </summary>
    /// <param name="links">The menu links to write.</param>
    /// <param name="customWebConfigPath">Optional override path to config.json.</param>
    /// <returns>True if the sync succeeded.</returns>
    public bool SyncMenuLinks(MenuLink[] links, string? customWebConfigPath)
    {
        var configPath = ResolveConfigPath(customWebConfigPath);

        try
        {
            if (!File.Exists(configPath))
            {
                _logger.LogError("Web config.json not found at {ConfigPath}", configPath);
                return false;
            }

            var jsonText = File.ReadAllText(configPath);
            var root = JsonNode.Parse(jsonText)?.AsObject()
                ?? throw new InvalidOperationException("config.json root must be a JSON object.");

            root["menuLinks"] = BuildMenuLinksArray(links);

            File.WriteAllText(configPath, root.ToJsonString(JsonWriteOptions));
            _logger.LogInformation("Synced {Count} menu link(s) to {ConfigPath}", links.Length, configPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync menu links to {ConfigPath}", configPath);
            return false;
        }
    }

    /// <summary>
    /// Reads existing menu links from the web client config.json file.
    /// </summary>
    /// <param name="customWebConfigPath">Optional override path to config.json.</param>
    /// <returns>The menu links found in config.json, or null if unavailable.</returns>
    public MenuLink[]? ReadMenuLinks(string? customWebConfigPath)
    {
        var configPath = ResolveConfigPath(customWebConfigPath);

        try
        {
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Web config.json not found at {ConfigPath}", configPath);
                return null;
            }

            var jsonText = File.ReadAllText(configPath);
            var root = JsonNode.Parse(jsonText)?.AsObject();
            if (root?["menuLinks"] is not JsonArray menuLinksNode)
            {
                return [];
            }

            var links = new List<MenuLink>();
            foreach (var item in menuLinksNode)
            {
                if (item?.AsObject() is not JsonObject linkObject)
                {
                    continue;
                }

                var name = linkObject["name"]?.GetValue<string>();
                var url = linkObject["url"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                links.Add(new MenuLink
                {
                    Name = name.Trim(),
                    Url = url.Trim(),
                    Icon = linkObject["icon"]?.GetValue<string>()?.Trim()
                });
            }

            return links.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read menu links from {ConfigPath}", configPath);
            return null;
        }
    }

    private static JsonArray BuildMenuLinksArray(IEnumerable<MenuLink> links)
    {
        var array = new JsonArray();

        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Name) || string.IsNullOrWhiteSpace(link.Url))
            {
                continue;
            }

            var obj = new JsonObject
            {
                ["name"] = link.Name.Trim(),
                ["url"] = link.Url.Trim()
            };

            if (!string.IsNullOrWhiteSpace(link.Icon))
            {
                obj["icon"] = link.Icon.Trim();
            }

            array.Add(obj);
        }

        return array;
    }
}
