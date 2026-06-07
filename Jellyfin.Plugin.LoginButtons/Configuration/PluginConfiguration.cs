using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.LoginButtons.Configuration;

/// <summary>
/// Plugin configuration for custom login page buttons.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        DisclaimerText = string.Empty;
        SignUpUrl = string.Empty;
        SignUpLabel = "Sign Up";
        ForgotPasswordUrl = string.Empty;
        ForgotPasswordLabel = "Forgot Password";
        LastSyncStatus = string.Empty;
    }

    /// <summary>
    /// Gets or sets optional text shown above the login buttons.
    /// </summary>
    public string DisclaimerText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sign-up button is enabled.
    /// </summary>
    public bool EnableSignUpButton { get; set; }

    /// <summary>
    /// Gets or sets the sign-up button destination URL.
    /// </summary>
    public string SignUpUrl { get; set; }

    /// <summary>
    /// Gets or sets the sign-up button label.
    /// </summary>
    public string SignUpLabel { get; set; }

    /// <summary>
    /// Gets or sets the custom forgot-password URL. When set, the default Jellyfin button is hidden.
    /// </summary>
    public string ForgotPasswordUrl { get; set; }

    /// <summary>
    /// Gets or sets the custom forgot-password button label.
    /// </summary>
    public string ForgotPasswordLabel { get; set; }

    /// <summary>
    /// Gets or sets the result message from the last branding sync attempt.
    /// </summary>
    public string LastSyncStatus { get; set; }
}
