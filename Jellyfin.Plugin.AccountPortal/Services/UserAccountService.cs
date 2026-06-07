using System.Net.Mail;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.AccountPortal.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountPortal.Services;

/// <summary>
/// Handles public account registration and password reset flows.
/// </summary>
public partial class UserAccountService
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly IUserManager _userManager;
    private readonly AccountDataStore _dataStore;
    private readonly EmailService _emailService;
    private readonly ILogger<UserAccountService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccountService"/> class.
    /// </summary>
    /// <param name="userManager">The Jellyfin user manager.</param>
    /// <param name="dataStore">The account data store.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="logger">The logger.</param>
    public UserAccountService(
        IUserManager userManager,
        AccountDataStore dataStore,
        EmailService emailService,
        ILogger<UserAccountService> logger)
    {
        _userManager = userManager;
        _dataStore = dataStore;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new Jellyfin user when open signup is enabled.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <param name="username">The requested username.</param>
    /// <param name="email">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="confirmPassword">The password confirmation.</param>
    /// <returns>The operation result.</returns>
    public async Task<AccountOperationResult> RegisterAsync(
        PluginConfiguration configuration,
        string username,
        string email,
        string password,
        string confirmPassword)
    {
        if (!configuration.EnableOpenSignup)
        {
            return AccountOperationResult.Fail("Sign up is currently disabled.");
        }

        username = username.Trim();
        email = email.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
        {
            return AccountOperationResult.Fail("Username and email are required.");
        }

        if (!IsValidEmail(email))
        {
            return AccountOperationResult.Fail("Enter a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AccountOperationResult.Fail("Password is required.");
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return AccountOperationResult.Fail("Passwords do not match.");
        }

        if (password.Length < Math.Max(1, configuration.MinimumPasswordLength))
        {
            return AccountOperationResult.Fail($"Password must be at least {configuration.MinimumPasswordLength} characters.");
        }

        if (_userManager.GetUserByName(username) is not null)
        {
            return AccountOperationResult.Fail("That username is already taken.");
        }

        if (_dataStore.FindUserIdByEmail(email) is not null)
        {
            return AccountOperationResult.Fail("That email address is already registered.");
        }

        try
        {
            var user = await _userManager.CreateUserAsync(username).ConfigureAwait(false);
            await _userManager.ChangePassword(user, password).ConfigureAwait(false);

            var policy = _userManager.GetUserDto(user, null).Policy ?? new UserPolicy();
            policy.IsDisabled = false;
            policy.IsHidden = false;
            await _userManager.UpdatePolicyAsync(user.Id, policy).ConfigureAwait(false);

            _dataStore.SetUserEmail(user.Id, email);

            _logger.LogInformation("Registered new user {Username} via Account Portal", username);
            return AccountOperationResult.Ok("Account created. You can sign in now.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user {Username}", username);
            return AccountOperationResult.Fail("Could not create the account. Try a different username.");
        }
    }

    /// <summary>
    /// Sends a password reset email when email reset is enabled.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <param name="usernameOrEmail">The username or email address.</param>
    /// <param name="publicBaseUrl">The public server URL used in reset links.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public async Task<AccountOperationResult> RequestPasswordResetAsync(
        PluginConfiguration configuration,
        string usernameOrEmail,
        string publicBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.EnableEmailPasswordReset)
        {
            return AccountOperationResult.Fail("Password reset is currently disabled.");
        }

        if (!EmailService.IsConfigured(configuration))
        {
            return AccountOperationResult.Fail("Email password reset is not configured yet.");
        }

        usernameOrEmail = usernameOrEmail.Trim();
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            return AccountOperationResult.Fail("Enter your username or email address.");
        }

        var genericMessage = "If an account exists, a reset link has been sent to its email address.";
        var user = _userManager.GetUserByName(usernameOrEmail);
        Guid? userId = user?.Id;

        if (userId is null && IsValidEmail(usernameOrEmail))
        {
            userId = _dataStore.FindUserIdByEmail(usernameOrEmail);
            if (userId is not null)
            {
                user = _userManager.GetUserById(userId.Value);
            }
        }

        if (user is null || userId is null)
        {
            return AccountOperationResult.Ok(genericMessage);
        }

        var email = _dataStore.GetUserEmail(userId.Value);
        if (string.IsNullOrWhiteSpace(email))
        {
            return AccountOperationResult.Ok(genericMessage);
        }

        var token = _dataStore.CreateResetToken(userId.Value, ResetTokenLifetime);
        var resetLink = BuildResetLink(publicBaseUrl, token);

        try
        {
            await _emailService.SendPasswordResetEmailAsync(configuration, email, resetLink, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email for user {UserId}", userId);
            return AccountOperationResult.Fail("Could not send the reset email. Check SMTP settings.");
        }

        return AccountOperationResult.Ok(genericMessage);
    }

    /// <summary>
    /// Resets a user's password using a valid reset token.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <param name="token">The reset token.</param>
    /// <param name="password">The new password.</param>
    /// <param name="confirmPassword">The password confirmation.</param>
    /// <returns>The operation result.</returns>
    public async Task<AccountOperationResult> ResetPasswordAsync(
        PluginConfiguration configuration,
        string token,
        string password,
        string confirmPassword)
    {
        if (!configuration.EnableEmailPasswordReset)
        {
            return AccountOperationResult.Fail("Password reset is currently disabled.");
        }

        token = token.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AccountOperationResult.Fail("Reset link is invalid or expired.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AccountOperationResult.Fail("Password is required.");
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return AccountOperationResult.Fail("Passwords do not match.");
        }

        if (password.Length < Math.Max(1, configuration.MinimumPasswordLength))
        {
            return AccountOperationResult.Fail($"Password must be at least {configuration.MinimumPasswordLength} characters.");
        }

        var userId = _dataStore.ConsumeResetToken(token);
        if (userId is null)
        {
            return AccountOperationResult.Fail("Reset link is invalid or expired.");
        }

        var user = _userManager.GetUserById(userId.Value);
        if (user is null)
        {
            return AccountOperationResult.Fail("Reset link is invalid or expired.");
        }

        try
        {
            await _userManager.ChangePassword(user, password).ConfigureAwait(false);
            _logger.LogInformation("Password reset completed for user {UserId}", userId);
            return AccountOperationResult.Ok("Password updated. You can sign in now.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password for user {UserId}", userId);
            return AccountOperationResult.Fail("Could not update the password.");
        }
    }

    internal static string BuildResetLink(string publicBaseUrl, string token)
    {
        var baseUrl = publicBaseUrl.Trim().TrimEnd('/');
        return $"{baseUrl}/AccountPortal/Reset?token={Uri.EscapeDataString(token)}";
    }

    internal static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return EmailRegex().IsMatch(email);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}

/// <summary>
/// Result of a public account operation.
/// </summary>
public class AccountOperationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>The result.</returns>
    public static AccountOperationResult Ok(string message)
    {
        return new AccountOperationResult { Success = true, Message = message };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The result.</returns>
    public static AccountOperationResult Fail(string message)
    {
        return new AccountOperationResult { Success = false, Message = message };
    }
}
