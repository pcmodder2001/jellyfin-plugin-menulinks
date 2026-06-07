using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AccountPortal.Configuration;

/// <summary>
/// Plugin configuration for self-service account signup and email password reset.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        PublicServerUrl = string.Empty;
        SmtpHost = string.Empty;
        SmtpPort = 587;
        SmtpUsername = string.Empty;
        SmtpPassword = string.Empty;
        SmtpFromEmail = string.Empty;
        SmtpFromName = "Jellyfin";
        SmtpSecurity = "StartTls";
        MinimumPasswordLength = 8;
        SignUpButtonLabel = "Sign Up";
        ForgotPasswordButtonLabel = "Forgot Password";
        DisclaimerText = string.Empty;
        LastSyncStatus = string.Empty;
    }

    /// <summary>
    /// Gets or sets a value indicating whether open self-service signup is enabled.
    /// </summary>
    public bool EnableOpenSignup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether email password reset is enabled.
    /// </summary>
    public bool EnableEmailPasswordReset { get; set; }

    /// <summary>
    /// Gets or sets the public Jellyfin server URL used in reset emails (e.g. https://jellyfin.example.com).
    /// </summary>
    public string PublicServerUrl { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server hostname.
    /// </summary>
    public string SmtpHost { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server port.
    /// </summary>
    public int SmtpPort { get; set; }

    /// <summary>
    /// Gets or sets the SMTP username.
    /// </summary>
    public string SmtpUsername { get; set; }

    /// <summary>
    /// Gets or sets the SMTP password.
    /// </summary>
    public string SmtpPassword { get; set; }

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string SmtpFromEmail { get; set; }

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string SmtpFromName { get; set; }

    /// <summary>
    /// Gets or sets the SMTP security mode (None, StartTls, or Ssl).
    /// </summary>
    public string SmtpSecurity { get; set; }

    /// <summary>
    /// Gets or sets the minimum password length for new accounts.
    /// </summary>
    public int MinimumPasswordLength { get; set; }

    /// <summary>
    /// Gets or sets the sign-up button label on the login page.
    /// </summary>
    public string SignUpButtonLabel { get; set; }

    /// <summary>
    /// Gets or sets the forgot-password button label on the login page.
    /// </summary>
    public string ForgotPasswordButtonLabel { get; set; }

    /// <summary>
    /// Gets or sets optional text shown above login page buttons.
    /// </summary>
    public string DisclaimerText { get; set; }

    /// <summary>
    /// Gets or sets the result message from the last sync attempt.
    /// </summary>
    public string LastSyncStatus { get; set; }
}
