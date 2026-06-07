using Jellyfin.Plugin.AccountPortal.Configuration;
using Jellyfin.Plugin.AccountPortal.Services;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountPortal;

/// <summary>
/// Handles startup sync and configuration change events for Account Portal.
/// </summary>
public class ServerEntryPoint : IHostedService, IDisposable
{
    private static bool _suppressConfigurationSync;

    private readonly BrandingSyncService _syncService;
    private readonly ILogger<ServerEntryPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerEntryPoint"/> class.
    /// </summary>
    /// <param name="syncService">The branding sync service.</param>
    /// <param name="logger">The logger.</param>
    public ServerEntryPoint(BrandingSyncService syncService, ILogger<ServerEntryPoint> logger)
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
                _logger.LogWarning("Account Portal plugin instance is not available during startup");
                return Task.CompletedTask;
            }

            Plugin.Instance.ConfigurationChanged += OnConfigurationChanged;
            SyncOnStartup();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account Portal startup sync failed");
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
        var importedDisclaimer = _syncService.ImportDisclaimerText(config);
        if (importedDisclaimer is not null)
        {
            config.DisclaimerText = importedDisclaimer;
            Plugin.Instance.SaveConfiguration();
            return;
        }

        ApplySyncResult(config, _syncService.SyncBranding(config));
    }

    private void OnConfigurationChanged(object? sender, BasePluginConfiguration configuration)
    {
        if (_suppressConfigurationSync)
        {
            return;
        }

        try
        {
            if (configuration is not PluginConfiguration pluginConfiguration)
            {
                return;
            }

            ApplySyncResult(pluginConfiguration, _syncService.SyncBranding(pluginConfiguration));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account Portal configuration sync failed");
        }
    }

    private void ApplySyncResult(PluginConfiguration configuration, SyncResult result)
    {
        if (Plugin.Instance is null)
        {
            return;
        }

        configuration.LastSyncStatus = result.Message;

        _suppressConfigurationSync = true;
        try
        {
            Plugin.Instance.SaveConfiguration();
        }
        finally
        {
            _suppressConfigurationSync = false;
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
