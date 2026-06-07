using Jellyfin.Plugin.MenuLinks.Configuration;
using Jellyfin.Plugin.MenuLinks.Services;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MenuLinks;

/// <summary>
/// Handles startup sync and configuration change events for menu links.
/// </summary>
public class ServerEntryPoint : IHostedService, IDisposable
{
    private readonly WebConfigSyncService _syncService;
    private readonly ILogger<ServerEntryPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerEntryPoint"/> class.
    /// </summary>
    /// <param name="syncService">The web config sync service.</param>
    /// <param name="logger">The logger.</param>
    public ServerEntryPoint(WebConfigSyncService syncService, ILogger<ServerEntryPoint> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (Plugin.Instance is null)
            {
                _logger.LogWarning("Custom Menu Links plugin instance is not available during startup");
                return Task.CompletedTask;
            }

            Plugin.Instance.ConfigurationChanged += OnConfigurationChanged;
            SyncOnStartup();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom Menu Links startup sync failed");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged -= OnConfigurationChanged;
        }

        return Task.CompletedTask;
    }

    private void SyncOnStartup()
    {
        if (Plugin.Instance is null)
        {
            return;
        }

        var config = Plugin.Instance.Configuration;
        var menuLinks = config.MenuLinks ?? [];

        if (menuLinks.Length == 0)
        {
            var imported = _syncService.ReadMenuLinks(config.CustomWebConfigPath);
            if (imported is { Length: > 0 })
            {
                config.MenuLinks = imported;
                Plugin.Instance.SaveConfiguration();
            }
        }
        else
        {
            _syncService.SyncMenuLinks(menuLinks, config.CustomWebConfigPath);
        }
    }

    private void OnConfigurationChanged(object? sender, BasePluginConfiguration configuration)
    {
        try
        {
            if (configuration is not PluginConfiguration pluginConfiguration)
            {
                return;
            }

            _syncService.SyncMenuLinks(
                pluginConfiguration.MenuLinks ?? [],
                pluginConfiguration.CustomWebConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom Menu Links configuration sync failed");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged -= OnConfigurationChanged;
        }

        GC.SuppressFinalize(this);
    }
}
