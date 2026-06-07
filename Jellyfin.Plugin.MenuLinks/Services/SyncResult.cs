namespace Jellyfin.Plugin.MenuLinks.Services;

/// <summary>
/// Result of syncing menu links to the web client config.json.
/// </summary>
public sealed class SyncResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the sync succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the config.json path that was used or attempted.
    /// </summary>
    public string ConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
