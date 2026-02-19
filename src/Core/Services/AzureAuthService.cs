using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Manages Azure authentication state for multiple accounts with persistent token caching.
    /// Tokens and authentication records are stored to disk so users don't
    /// need to re-authenticate every VS session.
    /// </summary>
    internal sealed class AzureAuthService
    {
        private static readonly Lazy<AzureAuthService> _instance = new(() => new AzureAuthService());
        private static readonly string _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzureExplorer");
        private static readonly string _accountsDir = Path.Combine(_cacheDir, "accounts");

        private readonly Dictionary<string, AccountCredential> _accounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        private AzureAuthService() { }

        public static AzureAuthService Instance => _instance.Value;

        /// <summary>
        /// Returns true if at least one account is signed in.
        /// </summary>
        public bool IsSignedIn => _accounts.Count > 0;

        public bool IsSigningIn { get; private set; }

        /// <summary>
        /// Gets a read-only collection of all signed-in accounts.
        /// </summary>
        public IReadOnlyList<AccountInfo> Accounts
        {
            get
            {
                lock (_lock)
                {
                    return _accounts.Values
                        .Select(a => new AccountInfo(a.AccountId, a.Username, a.Credential))
                        .ToList()
                        .AsReadOnly();
                }
            }
        }

        public event EventHandler AuthStateChanged;

        /// <summary>
        /// Gets the credential for a specific account.
        /// </summary>
        public TokenCredential GetCredential(string accountId)
        {
            lock (_lock)
            {
                if (_accounts.TryGetValue(accountId, out AccountCredential account))
                    return account.Credential;
            }

            throw new InvalidOperationException($"Account '{accountId}' is not signed in.");
        }

        /// <summary>
        /// Attempts to restore all previous sessions silently using persisted
        /// authentication records and token cache. Returns number of accounts restored.
        /// This method never opens a browser.
        /// </summary>
        public async Task<int> TrySilentSignInAsync(CancellationToken cancellationToken = default)
        {
            List<AuthenticationRecord> records = await LoadAllAuthRecordsAsync();
            if (records.Count == 0)
            {
                // Migration: try loading legacy single-account auth record
                AuthenticationRecord legacyRecord = await LoadLegacyAuthRecordAsync();
                if (legacyRecord != null)
                    records.Add(legacyRecord);
            }

            var restored = 0;
            foreach (AuthenticationRecord record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Use DisableAutomaticAuthentication so GetTokenAsync throws
                    // AuthenticationRequiredException instead of opening a browser.
                    var silentOptions = new InteractiveBrowserCredentialOptions
                    {
                        TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                        {
                            Name = "AzureExplorer",
                            UnsafeAllowUnencryptedStorage = true,
                        },
                        AdditionallyAllowedTenants = { "*" },
                        AuthenticationRecord = record,
                        DisableAutomaticAuthentication = true,
                    };
                    var silentCredential = new InteractiveBrowserCredential(silentOptions);

                    var context = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
                    await silentCredential.GetTokenAsync(context, cancellationToken);

                    // Silent token acquisition succeeded — create the real credential
                    InteractiveBrowserCredential credential = CreateCredential(record);
                    var accountId = GetAccountId(record);

                    lock (_lock)
                    {
                        _accounts[accountId] = new AccountCredential(accountId, record.Username ?? "Azure Account", credential, record);
                    }

                    restored++;
                }
                catch (AuthenticationRequiredException)
                {
                    // Silent auth failed for this account — skip it
                }
                catch (CredentialUnavailableException)
                {
                    // Credential not available (expired refresh token, revoked, etc.)
                }
            }

            if (restored > 0)
                AuthStateChanged?.Invoke(this, EventArgs.Empty);

            return restored;
        }

        /// <summary>
        /// Signs in a new account with an interactive browser prompt.
        /// Persists the authentication record and token cache to disk.
        /// </summary>
        public async Task<AccountInfo> AddAccountAsync(CancellationToken cancellationToken = default)
        {
            if (IsSigningIn)
                return null;

            IsSigningIn = true;
            try
            {
                InteractiveBrowserCredential credential = CreateCredential(authRecord: null);

                // AuthenticateAsync opens the browser ONCE and returns an
                // AuthenticationRecord we can persist for future silent auth.
                var context = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
                AuthenticationRecord record = await credential.AuthenticateAsync(context, cancellationToken);

                var accountId = GetAccountId(record);
                var username = record.Username ?? "Azure Account";

                // Check if account already exists
                lock (_lock)
                {
                    if (_accounts.ContainsKey(accountId))
                    {
                        // Update existing account credential
                        _accounts[accountId] = new AccountCredential(accountId, username, credential, record);
                    }
                    else
                    {
                        _accounts[accountId] = new AccountCredential(accountId, username, credential, record);
                    }
                }

                SaveAuthRecord(accountId, record);
                AuthStateChanged?.Invoke(this, EventArgs.Empty);

                return new AccountInfo(accountId, username, credential);
            }
            finally
            {
                IsSigningIn = false;
            }
        }

        /// <summary>
        /// Signs out a specific account and removes its persisted tokens.
        /// </summary>
        public void SignOut(string accountId)
        {
            bool noAccountsRemaining;
            lock (_lock)
            {
                _accounts.Remove(accountId);
                noAccountsRemaining = _accounts.Count == 0;
            }

            DeleteAuthRecord(accountId);

            // When the last account is signed out, also delete legacy auth record
            // to prevent it from being restored on next startup
            if (noAccountsRemaining)
            {
                DeleteLegacyAuthRecord();
            }

            AzureResourceService.Instance.ClearClientCache();
            AuthStateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Signs out all accounts and removes all persisted tokens.
        /// </summary>
        public void SignOutAll()
        {
            lock (_lock)
            {
                _accounts.Clear();
            }

            // Delete ALL auth record files, not just those in memory.
            // This handles cases where silent auth failed during startup
            // but the auth record file still exists on disk.
            DeleteAllAuthRecords();

            // Also delete legacy auth record if it exists
            DeleteLegacyAuthRecord();

            AzureResourceService.Instance.ClearClientCache();
            AuthStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private static string GetAccountId(AuthenticationRecord record)
        {
            // Use a combination of username and home account id to create a unique identifier
            return record.HomeAccountId ?? record.Username ?? Guid.NewGuid().ToString();
        }

        private static string GetSafeFileName(string accountId)
        {
            // Create a safe filename from the account ID
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                accountId = accountId.Replace(c, '_');
            }
            return accountId;
        }

        private static InteractiveBrowserCredential CreateCredential(AuthenticationRecord authRecord)
        {
            var options = new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "AzureExplorer",
                    UnsafeAllowUnencryptedStorage = true,
                },
                AdditionallyAllowedTenants = { "*" },
            };

            if (authRecord != null)
            {
                options.AuthenticationRecord = authRecord;
            }

            return new InteractiveBrowserCredential(options);
        }

        private static async Task<List<AuthenticationRecord>> LoadAllAuthRecordsAsync()
        {
            var records = new List<AuthenticationRecord>();

            try
            {
                if (!Directory.Exists(_accountsDir))
                    return records;

                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(_accountsDir, "*.bin"))
                    {
                        try
                        {
                            using (FileStream stream = File.OpenRead(file))
                            {
                                var record = AuthenticationRecord.Deserialize(stream);
                                records.Add(record);
                            }
                        }
                        catch
                        {
                            // Skip corrupted files
                        }
                    }
                });
            }
            catch
            {
                // Return empty list on error
            }

            return records;
        }

        private static async Task<AuthenticationRecord> LoadLegacyAuthRecordAsync()
        {
            var legacyPath = Path.Combine(_cacheDir, "auth-record.bin");
            try
            {
                return await Task.Run(() =>
                {
                    if (!File.Exists(legacyPath))
                        return null;

                    using (FileStream stream = File.OpenRead(legacyPath))
                    {
                        return AuthenticationRecord.Deserialize(stream);
                    }
                });
            }
            catch
            {
                return null;
            }
        }

        private static void SaveAuthRecord(string accountId, AuthenticationRecord record)
        {
            try
            {
                Directory.CreateDirectory(_accountsDir);
                var fileName = GetSafeFileName(accountId) + ".bin";
                var filePath = Path.Combine(_accountsDir, fileName);

                using (FileStream stream = File.Create(filePath))
                {
                    record.Serialize(stream);
                }
            }
            catch
            {
                // Non-critical — user will just need to sign in again next session
            }
        }

        private static void DeleteAuthRecord(string accountId)
        {
            try
            {
                var fileName = GetSafeFileName(accountId) + ".bin";
                var filePath = Path.Combine(_accountsDir, fileName);

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        private static void DeleteAllAuthRecords()
        {
            try
            {
                if (Directory.Exists(_accountsDir))
                {
                    foreach (var file in Directory.GetFiles(_accountsDir, "*.bin"))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Best effort cleanup for individual files
                        }
                    }
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        private static void DeleteLegacyAuthRecord()
        {
            try
            {
                var legacyPath = Path.Combine(_cacheDir, "auth-record.bin");
                if (File.Exists(legacyPath))
                    File.Delete(legacyPath);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        private sealed class AccountCredential(string accountId, string username, InteractiveBrowserCredential credential, AuthenticationRecord record)
        {
            public string AccountId { get; } = accountId;
            public string Username { get; } = username;
            public InteractiveBrowserCredential Credential { get; } = credential;
            public AuthenticationRecord Record { get; } = record;
        }
    }

    /// <summary>
    /// Public information about a signed-in Azure account.
    /// </summary>
    internal sealed class AccountInfo(string accountId, string username, TokenCredential credential)
    {
        public string AccountId { get; } = accountId;
        public string Username { get; } = username;
        public TokenCredential Credential { get; } = credential;
    }
}
