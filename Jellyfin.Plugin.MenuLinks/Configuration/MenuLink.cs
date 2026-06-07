namespace Jellyfin.Plugin.MenuLinks.Configuration;

/// <summary>
/// A custom navigation menu link for Jellyfin Web.
/// </summary>
public class MenuLink
{
    /// <summary>
    /// Gets or sets the display name shown in the side menu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional Material Design icon name.
    /// </summary>
    public string? Icon { get; set; }
}
