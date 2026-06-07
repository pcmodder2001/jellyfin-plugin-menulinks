namespace Jellyfin.Plugin.AccountPortal.Services;

/// <summary>
/// Result of a branding sync operation.
/// </summary>
public class SyncResult
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
