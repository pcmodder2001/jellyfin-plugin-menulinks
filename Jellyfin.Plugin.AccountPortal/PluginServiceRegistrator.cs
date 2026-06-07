using Jellyfin.Plugin.AccountPortal.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AccountPortal;

/// <summary>
/// Registers plugin services with the Jellyfin dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<AccountDataStore>();
        serviceCollection.AddSingleton<EmailService>();
        serviceCollection.AddSingleton<UserAccountService>();
        serviceCollection.AddSingleton<BrandingSyncService>();
        serviceCollection.AddHostedService<ServerEntryPoint>();
    }
}
