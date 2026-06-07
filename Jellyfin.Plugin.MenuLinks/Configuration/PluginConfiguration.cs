using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MenuLinks.Configuration;

/// <summary>
/// Plugin configuration for custom side menu links.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        MenuLinks = [];
        CustomWebConfigPath = string.Empty;
    }

    /// <summary>
    /// Gets or sets the custom menu links to show in Jellyfin Web.
    /// </summary>
    public MenuLink[] MenuLinks { get; set; } = [];

    /// <summary>
    /// Gets or sets an optional override path to the web client config.json file.
    /// Leave empty to use the server's default web path.
    /// </summary>
    public string CustomWebConfigPath { get; set; }
}
