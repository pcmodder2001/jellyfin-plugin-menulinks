using System.Globalization;
using Jellyfin.Plugin.AccountPortal.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AccountPortal;

/// <summary>
/// Plugin entry point for self-service account signup and email password reset.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="xmlSerializer">The XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("c9e6f3c4-8d5b-4a2f-9e0c-3b1a7d8e5f2b");

    /// <inheritdoc />
    public override string Name => "Account Portal";

    /// <inheritdoc />
    public override string Description => "Open self-service signup, auto-enabled accounts, and email password reset links on the login page.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "accountportal",
            DisplayName = Name,
            EmbeddedResourcePath = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.Configuration.configPage.html",
                GetType().Namespace),
            EnableInMainMenu = true,
            MenuIcon = "person_add"
        };
    }
}
