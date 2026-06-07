using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.AccountPortal.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Branding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountPortal.Services;

/// <summary>
/// Synchronizes account portal links into Jellyfin branding settings.
/// </summary>
public partial class BrandingSyncService
{
    internal const string HtmlBeginMarker = "<!-- BEGIN Jellyfin.Plugin.AccountPortal -->";
    internal const string HtmlEndMarker = "<!-- END Jellyfin.Plugin.AccountPortal -->";
    internal const string CssBeginMarker = "/* BEGIN Jellyfin.Plugin.AccountPortal */";
    internal const string CssEndMarker = "/* END Jellyfin.Plugin.AccountPortal */";

    internal const string SignUpPath = "/AccountPortal/Signup";
    internal const string ForgotPasswordPath = "/AccountPortal/ForgotPassword";

    private readonly IServerConfigurationManager _configurationManager;
    private readonly ILogger<BrandingSyncService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrandingSyncService"/> class.
    /// </summary>
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="logger">The logger.</param>
    public BrandingSyncService(IServerConfigurationManager configurationManager, ILogger<BrandingSyncService> logger)
    {
        _configurationManager = configurationManager;
        _logger = logger;
    }

    /// <summary>
    /// Applies account portal settings to Jellyfin branding configuration.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <returns>The sync result.</returns>
    public SyncResult SyncBranding(PluginConfiguration configuration)
    {
        try
        {
            var branding = (BrandingOptions)_configurationManager.GetConfiguration("branding");
            var existingDisclaimer = branding.LoginDisclaimer ?? string.Empty;
            var userDisclaimer = string.IsNullOrWhiteSpace(configuration.DisclaimerText)
                ? ExtractUserDisclaimer(existingDisclaimer)
                : configuration.DisclaimerText.Trim();

            branding.LoginDisclaimer = BuildLoginDisclaimer(userDisclaimer, configuration);
            branding.CustomCss = MergePluginCss(branding.CustomCss, BuildPluginCss(configuration));

            _configurationManager.SaveConfiguration("branding", branding);
            _logger.LogInformation("Synced Account Portal links to Jellyfin branding settings");

            return new SyncResult
            {
                Success = true,
                Message = "Login page links synced. Hard-refresh the login page to see changes."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Account Portal links to Jellyfin branding settings");
            return new SyncResult
            {
                Success = false,
                Message = "Failed to update branding settings. Check the Jellyfin log for details."
            };
        }
    }

    /// <summary>
    /// Imports disclaimer text from existing branding if the plugin config is empty.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <returns>The imported disclaimer text, if any.</returns>
    public string? ImportDisclaimerText(PluginConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.DisclaimerText))
        {
            return null;
        }

        var branding = (BrandingOptions)_configurationManager.GetConfiguration("branding");
        var extracted = ExtractUserDisclaimer(branding.LoginDisclaimer ?? string.Empty);
        return string.IsNullOrWhiteSpace(extracted) ? null : extracted;
    }

    internal static string ExtractUserDisclaimer(string loginDisclaimer)
    {
        if (string.IsNullOrWhiteSpace(loginDisclaimer))
        {
            return string.Empty;
        }

        var withoutPluginBlock = PluginHtmlBlockRegex().Replace(loginDisclaimer, string.Empty);
        return withoutPluginBlock.Trim();
    }

    internal static string BuildLoginDisclaimer(string userDisclaimer, PluginConfiguration configuration)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(userDisclaimer))
        {
            builder.AppendLine(userDisclaimer.Trim());
            builder.AppendLine();
        }

        var buttonHtml = BuildButtonHtml(configuration);
        if (!string.IsNullOrWhiteSpace(buttonHtml))
        {
            builder.AppendLine(HtmlBeginMarker);
            builder.AppendLine("<div class=\"accountPortalPlugin\">");
            builder.AppendLine(buttonHtml);
            builder.AppendLine("</div>");
            builder.AppendLine(HtmlEndMarker);
        }

        return builder.ToString().Trim();
    }

    internal static string BuildButtonHtml(PluginConfiguration configuration)
    {
        var builder = new StringBuilder();

        if (configuration.EnableOpenSignup)
        {
            builder.AppendLine(BuildPrimaryLink(
                SignUpPath,
                string.IsNullOrWhiteSpace(configuration.SignUpButtonLabel) ? "Sign Up" : configuration.SignUpButtonLabel.Trim()));
        }

        if (configuration.EnableEmailPasswordReset)
        {
            builder.AppendLine(BuildSecondaryLink(
                ForgotPasswordPath,
                string.IsNullOrWhiteSpace(configuration.ForgotPasswordButtonLabel)
                    ? "Forgot Password"
                    : configuration.ForgotPasswordButtonLabel.Trim()));
        }

        return builder.ToString().Trim();
    }

    internal static string BuildPrimaryLink(string path, string label)
    {
        return $"<a href=\"{WebUtility.HtmlEncode(path)}\" class=\"raised button-submit block emby-button jellyfinPluginLoginBtn jellyfinPluginBtnSignUp\"><span>{WebUtility.HtmlEncode(label)}</span></a>";
    }

    internal static string BuildSecondaryLink(string path, string label)
    {
        return $"<a href=\"{WebUtility.HtmlEncode(path)}\" class=\"raised cancel block emby-button jellyfinPluginLoginBtn jellyfinPluginBtnForgot\"><span>{WebUtility.HtmlEncode(label)}</span></a>";
    }

    internal static string BuildLink(string path, string label)
    {
        return BuildSecondaryLink(path, label);
    }

    internal static string BuildPluginCss(PluginConfiguration configuration)
    {
        if (!configuration.EnableOpenSignup && !configuration.EnableEmailPasswordReset)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        if (configuration.EnableEmailPasswordReset)
        {
            builder.AppendLine("#loginPage .btnForgotPassword { display: none !important; }");
        }

        builder.AppendLine(GetNativeLoginButtonCss());

        return builder.ToString().Trim();
    }

    internal static string GetNativeLoginButtonCss()
    {
        return """
            #loginPage .readOnlyContent {
              display: flex;
              flex-direction: column;
            }

            #loginPage .readOnlyContent > .btnManual { order: 10; }
            #loginPage .readOnlyContent > .btnQuick { order: 20; }
            #loginPage .readOnlyContent > .btnForgotPassword { order: 30; }
            #loginPage .readOnlyContent > .btnSelectServer { order: 50; }
            #loginPage .readOnlyContent > .loginDisclaimerContainer {
              order: 40;
              display: contents;
              margin-top: 0;
            }

            #loginPage .loginDisclaimer {
              display: contents;
            }

            #loginPage .loginDisclaimer .loginButtonsPlugin,
            #loginPage .loginDisclaimer .accountPortalPlugin {
              display: contents;
            }

            #loginPage .loginDisclaimer a.jellyfinPluginLoginBtn {
              display: block;
              width: 100%;
              margin-left: auto;
              margin-right: auto;
              margin-bottom: 1em;
              text-decoration: none;
              box-sizing: border-box;
              text-align: center;
            }

            #loginPage a.jellyfinPluginBtnSignUp { order: 25; }
            #loginPage a.jellyfinPluginBtnForgot { order: 30; }
            """;
    }

    internal static string MergePluginCss(string? existingCss, string pluginCss)
    {
        var css = RemoveMarkedBlock(existingCss ?? string.Empty, CssBeginMarker, CssEndMarker).Trim();

        if (string.IsNullOrWhiteSpace(pluginCss))
        {
            return css;
        }

        if (string.IsNullOrWhiteSpace(css))
        {
            return $"{CssBeginMarker}{Environment.NewLine}{pluginCss}{Environment.NewLine}{CssEndMarker}";
        }

        return $"{css}{Environment.NewLine}{Environment.NewLine}{CssBeginMarker}{Environment.NewLine}{pluginCss}{Environment.NewLine}{CssEndMarker}";
    }

    internal static string RemoveMarkedBlock(string input, string beginMarker, string endMarker)
    {
        var start = input.IndexOf(beginMarker, StringComparison.Ordinal);
        while (start >= 0)
        {
            var end = input.IndexOf(endMarker, start, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            input = input.Remove(start, (end + endMarker.Length) - start);
            start = input.IndexOf(beginMarker, StringComparison.Ordinal);
        }

        return input;
    }

    [GeneratedRegex("<!\\-\\- BEGIN Jellyfin\\.Plugin\\.AccountPortal \\-\\->[\\s\\S]*?<!\\-\\- END Jellyfin\\.Plugin\\.AccountPortal \\-\\->", RegexOptions.IgnoreCase)]
    private static partial Regex PluginHtmlBlockRegex();
}
