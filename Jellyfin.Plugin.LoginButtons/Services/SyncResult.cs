namespace Jellyfin.Plugin.LoginButtons.Services;

/// <summary>
/// Result of syncing login buttons to Jellyfin branding settings.
/// </summary>
public sealed class SyncResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the sync succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a human-readable status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
