using Jellyfin.Plugin.AccountPortal.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Jellyfin.Plugin.AccountPortal.Services;

/// <summary>
/// Sends account-related email through SMTP.
/// </summary>
public class EmailService
{
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether SMTP is configured for outbound mail.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <returns>True when SMTP settings are complete.</returns>
    public static bool IsConfigured(PluginConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration.SmtpHost)
            && configuration.SmtpPort > 0
            && !string.IsNullOrWhiteSpace(configuration.SmtpFromEmail);
    }

    /// <summary>
    /// Sends a password reset email containing a reset link.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="resetLink">The absolute reset URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendPasswordResetEmailAsync(
        PluginConfiguration configuration,
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured(configuration))
        {
            throw new InvalidOperationException("SMTP is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(configuration.SmtpFromName, configuration.SmtpFromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Reset your Jellyfin password";

        var body = new BodyBuilder
        {
            TextBody =
                "A password reset was requested for your Jellyfin account.\n\n" +
                "Use this link to choose a new password:\n" +
                resetLink + "\n\n" +
                "If you did not request this, you can ignore this email.\n\n" +
                "This link expires in 1 hour.",
            HtmlBody =
                "<p>A password reset was requested for your Jellyfin account.</p>" +
                "<p><a href=\"" + resetLink + "\">Reset your password</a></p>" +
                "<p>If you did not request this, you can ignore this email.</p>" +
                "<p>This link expires in 1 hour.</p>"
        };

        message.Body = body.ToMessageBody();
        await SendAsync(configuration, message, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendAsync(PluginConfiguration configuration, MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        var secureSocketOptions = ResolveSecurity(configuration.SmtpSecurity);

        try
        {
            await client.ConnectAsync(configuration.SmtpHost.Trim(), configuration.SmtpPort, secureSocketOptions, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(configuration.SmtpUsername))
            {
                await client.AuthenticateAsync(configuration.SmtpUsername.Trim(), configuration.SmtpPassword, cancellationToken)
                    .ConfigureAwait(false);
            }

            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account email via SMTP");
            throw;
        }
    }

    private static SecureSocketOptions ResolveSecurity(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "none" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls
        };
    }
}
