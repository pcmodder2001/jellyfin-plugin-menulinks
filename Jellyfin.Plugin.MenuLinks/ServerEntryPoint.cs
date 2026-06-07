using Jellyfin.Plugin.MenuLinks.Configuration;
using Jellyfin.Plugin.MenuLinks.Services;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.MenuLinks;

/// <summary>
/// Handles startup sync and configuration change events for menu links.
/// </summary>
public class ServerEntryPoint : IHostedService, IDisposable
{
    private readonly Plugin _plugin;
    private readonly WebConfigSyncService _syncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerEntryPoint"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="syncService">The web config sync service.</param>
    public ServerEntryPoint(Plugin plugin, WebConfigSyncService syncService)
    {
        _plugin = plugin;
        _syncService = syncService;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _plugin.ConfigurationChanged += OnConfigurationChanged;
        SyncOnStartup();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _plugin.ConfigurationChanged -= OnConfigurationChanged;
        return Task.CompletedTask;
    }

    private void SyncOnStartup()
    {
        var config = _plugin.Configuration;

        if (config.MenuLinks.Length == 0)
        {
            var imported = _syncService.ReadMenuLinks(config.CustomWebConfigPath);
            if (imported is { Length: > 0 })
            {
                config.MenuLinks = imported;
                _plugin.SaveConfiguration();
            }
        }
        else
        {
            _syncService.SyncMenuLinks(config.MenuLinks, config.CustomWebConfigPath);
        }
    }

    private void OnConfigurationChanged(object? sender, BasePluginConfiguration configuration)
    {
        if (configuration is not PluginConfiguration pluginConfiguration)
        {
            return;
        }

        _syncService.SyncMenuLinks(pluginConfiguration.MenuLinks, pluginConfiguration.CustomWebConfigPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _plugin.ConfigurationChanged -= OnConfigurationChanged;
        GC.SuppressFinalize(this);
    }
}
