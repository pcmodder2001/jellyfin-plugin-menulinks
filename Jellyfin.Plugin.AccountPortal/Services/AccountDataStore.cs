using System.Text.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountPortal.Services;

/// <summary>
/// Persists user email mappings and password reset tokens on disk.
/// </summary>
public class AccountDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _dataFilePath;
    private readonly ILogger<AccountDataStore> _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountDataStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public AccountDataStore(IApplicationPaths applicationPaths, ILogger<AccountDataStore> logger)
    {
        _logger = logger;
        var directory = Path.Combine(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.AccountPortal");
        Directory.CreateDirectory(directory);
        _dataFilePath = Path.Combine(directory, "account-data.json");
    }

    /// <summary>
    /// Stores an email address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="email">The email address.</param>
    public void SetUserEmail(Guid userId, string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        lock (_lock)
        {
            var data = LoadUnsafe();
            var userIdKey = userId.ToString("D");

            if (data.UserEmails.TryGetValue(userIdKey, out var previousEmail)
                && !string.IsNullOrWhiteSpace(previousEmail))
            {
                data.EmailToUserId.Remove(NormalizeEmail(previousEmail));
            }

            data.UserEmails[userIdKey] = normalizedEmail;
            data.EmailToUserId[normalizedEmail] = userIdKey;
            SaveUnsafe(data);
        }
    }

    /// <summary>
    /// Gets the email address for a user, if known.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The email address, or null if not stored.</returns>
    public string? GetUserEmail(Guid userId)
    {
        lock (_lock)
        {
            var data = LoadUnsafe();
            return data.UserEmails.TryGetValue(userId.ToString("D"), out var email) ? email : null;
        }
    }

    /// <summary>
    /// Finds a user identifier by email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The user identifier, or null if not found.</returns>
    public Guid? FindUserIdByEmail(string email)
    {
        lock (_lock)
        {
            var data = LoadUnsafe();
            if (!data.EmailToUserId.TryGetValue(NormalizeEmail(email), out var userIdText)
                || !Guid.TryParse(userIdText, out var userId))
            {
                return null;
            }

            return userId;
        }
    }

    /// <summary>
    /// Creates a password reset token for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="lifetime">How long the token remains valid.</param>
    /// <returns>The reset token string.</returns>
    public string CreateResetToken(Guid userId, TimeSpan lifetime)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        lock (_lock)
        {
            var data = LoadUnsafe();
            data.ResetTokens.RemoveAll(entry => entry.UserId == userId.ToString("D") || IsExpired(entry));
            data.ResetTokens.Add(new ResetTokenEntry
            {
                Token = token,
                UserId = userId.ToString("D"),
                ExpiresUtc = DateTime.UtcNow.Add(lifetime).ToString("O")
            });
            SaveUnsafe(data);
        }

        return token;
    }

    /// <summary>
    /// Consumes a reset token and returns the associated user identifier.
    /// </summary>
    /// <param name="token">The reset token.</param>
    /// <returns>The user identifier, or null if the token is invalid or expired.</returns>
    public Guid? ConsumeResetToken(string token)
    {
        lock (_lock)
        {
            var data = LoadUnsafe();
            var entry = data.ResetTokens.FirstOrDefault(item =>
                string.Equals(item.Token, token, StringComparison.Ordinal));

            data.ResetTokens.RemoveAll(IsExpired);

            if (entry is null || IsExpired(entry))
            {
                SaveUnsafe(data);
                return null;
            }

            data.ResetTokens.Remove(entry);
            SaveUnsafe(data);

            return Guid.TryParse(entry.UserId, out var userId) ? userId : null;
        }
    }

    private AccountData LoadUnsafe()
    {
        if (!File.Exists(_dataFilePath))
        {
            return new AccountData();
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            return JsonSerializer.Deserialize<AccountData>(json, JsonOptions) ?? new AccountData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read account data file; starting with empty data");
            return new AccountData();
        }
    }

    private void SaveUnsafe(AccountData data)
    {
        data.ResetTokens.RemoveAll(IsExpired);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static bool IsExpired(ResetTokenEntry entry)
    {
        return !DateTime.TryParse(entry.ExpiresUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expires)
            || expires <= DateTime.UtcNow;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private sealed class AccountData
    {
        public Dictionary<string, string> UserEmails { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> EmailToUserId { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public List<ResetTokenEntry> ResetTokens { get; set; } = new();
    }

    private sealed class ResetTokenEntry
    {
        public string Token { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string ExpiresUtc { get; set; } = string.Empty;
    }
}
