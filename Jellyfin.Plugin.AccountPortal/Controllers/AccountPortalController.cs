using System.Reflection;
using System.Text;
using Jellyfin.Plugin.AccountPortal.Configuration;
using Jellyfin.Plugin.AccountPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.AccountPortal.Controllers;

/// <summary>
/// Public account portal pages and API endpoints.
/// </summary>
[ApiController]
[Route("AccountPortal")]
public class AccountPortalController : ControllerBase
{
    private readonly UserAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountPortalController"/> class.
    /// </summary>
    /// <param name="accountService">The user account service.</param>
    public AccountPortalController(UserAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Serves the sign-up page.
    /// </summary>
    /// <returns>The HTML page.</returns>
    [HttpGet("Signup")]
    [AllowAnonymous]
    [Produces("text/html")]
    public IActionResult SignupPage()
    {
        return ServeEmbeddedPage("signup.html");
    }

    /// <summary>
    /// Serves the forgot-password page.
    /// </summary>
    /// <returns>The HTML page.</returns>
    [HttpGet("ForgotPassword")]
    [AllowAnonymous]
    [Produces("text/html")]
    public IActionResult ForgotPasswordPage()
    {
        return ServeEmbeddedPage("forgot.html");
    }

    /// <summary>
    /// Serves the reset-password page.
    /// </summary>
    /// <returns>The HTML page.</returns>
    [HttpGet("Reset")]
    [AllowAnonymous]
    [Produces("text/html")]
    public IActionResult ResetPage()
    {
        return ServeEmbeddedPage("reset.html");
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("Public/Register")]
    [AllowAnonymous]
    public async Task<ActionResult<AccountOperationResult>> Register([FromBody] RegisterRequest request)
    {
        var configuration = GetConfiguration();
        if (configuration is null)
        {
            return AccountOperationResult.Fail("Account Portal is not available.");
        }

        var result = await _accountService.RegisterAsync(
            configuration,
            request.Username ?? string.Empty,
            request.Email ?? string.Empty,
            request.Password ?? string.Empty,
            request.ConfirmPassword ?? string.Empty).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Sends a password reset email when possible.
    /// </summary>
    /// <param name="request">The forgot-password request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("Public/ForgotPassword")]
    [AllowAnonymous]
    public async Task<ActionResult<AccountOperationResult>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        if (configuration is null)
        {
            return AccountOperationResult.Fail("Account Portal is not available.");
        }

        var result = await _accountService.RequestPasswordResetAsync(
            configuration,
            request.UsernameOrEmail ?? string.Empty,
            ResolvePublicBaseUrl(configuration),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Resets a password using a valid token.
    /// </summary>
    /// <param name="request">The reset request.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("Public/ResetPassword")]
    [AllowAnonymous]
    public async Task<ActionResult<AccountOperationResult>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var configuration = GetConfiguration();
        if (configuration is null)
        {
            return AccountOperationResult.Fail("Account Portal is not available.");
        }

        var result = await _accountService.ResetPasswordAsync(
            configuration,
            request.Token ?? string.Empty,
            request.Password ?? string.Empty,
            request.ConfirmPassword ?? string.Empty).ConfigureAwait(false);

        return result;
    }

    private ContentResult ServeEmbeddedPage(string fileName)
    {
        var resourceName = typeof(AccountPortalController).Assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return Content("<p>Page not found.</p>", "text/html", Encoding.UTF8);
        }

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return Content("<p>Page not found.</p>", "text/html", Encoding.UTF8);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Content(reader.ReadToEnd(), "text/html", Encoding.UTF8);
    }

    private static PluginConfiguration? GetConfiguration()
    {
        return Plugin.Instance?.Configuration;
    }

    private string ResolvePublicBaseUrl(PluginConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.PublicServerUrl))
        {
            return configuration.PublicServerUrl.Trim().TrimEnd('/');
        }

        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host.Value}";
    }
}

/// <summary>
/// Registration request payload.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the password confirmation.
    /// </summary>
    public string? ConfirmPassword { get; set; }
}

/// <summary>
/// Forgot-password request payload.
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// Gets or sets the username or email address.
    /// </summary>
    public string? UsernameOrEmail { get; set; }
}

/// <summary>
/// Reset-password request payload.
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Gets or sets the reset token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the password confirmation.
    /// </summary>
    public string? ConfirmPassword { get; set; }
}
