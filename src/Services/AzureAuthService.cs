using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;

namespace AzureExplorer.Services
{
    /// <summary>
    /// Manages Azure authentication state with persistent token caching.
    /// Tokens and authentication records are stored to disk so users don't
    /// need to re-authenticate every VS session.
    /// </summary>
    internal sealed class AzureAuthService
    {
        private static readonly Lazy<AzureAuthService> _instance = new(() => new AzureAuthService());
        private static readonly string _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzureExplorer");
        private static readonly string _authRecordPath = Path.Combine(_cacheDir, "auth-record.bin");

        private InteractiveBrowserCredential _credential;
        private string _accountName;

        private AzureAuthService() { }

        public static AzureAuthService Instance => _instance.Value;

        public bool IsSignedIn { get; private set; }

        public bool IsSigningIn { get; private set; }

        public string AccountName => _accountName;

        public TokenCredential Credential => _credential;

        public event EventHandler AuthStateChanged;

        /// <summary>
        /// Attempts to restore a previous session silently using the persisted
        /// authentication record and token cache. Returns true if successful.
        /// This method never opens a browser.
        /// </summary>
        public async Task<bool> TrySilentSignInAsync(CancellationToken cancellationToken = default)
        {
            if (IsSignedIn)
                return true;

            AuthenticationRecord record = await LoadAuthRecordAsync();
            if (record == null)
                return false;

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

                var context = new TokenRequestContext(["https://management.azure.com/.default"]);
                await silentCredential.GetTokenAsync(context, cancellationToken);

                // Silent token acquisition succeeded — create the real credential
                // (without DisableAutomaticAuthentication) for ArmClient use.
                _credential = CreateCredential(record);
                _accountName = record.Username ?? "Azure Account";
                IsSignedIn = true;
                AuthStateChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (AuthenticationRequiredException)
            {
                // Silent auth failed — user interaction required (expected case)
                return false;
            }
            catch (CredentialUnavailableException)
            {
                // Credential not available (expired refresh token, revoked, etc.)
                return false;
            }
        }

        /// <summary>
        /// Signs in with a single interactive browser prompt.
        /// Persists the authentication record and token cache to disk.
        /// </summary>
        public async Task SignInAsync(CancellationToken cancellationToken = default)
        {
            if (IsSigningIn)
                return;

            IsSigningIn = true;
            try
            {
                InteractiveBrowserCredential credential = CreateCredential(authRecord: null);

                // AuthenticateAsync opens the browser ONCE and returns an
                // AuthenticationRecord we can persist for future silent auth.
                var context = new TokenRequestContext(["https://management.azure.com/.default"]);
                AuthenticationRecord record = await credential.AuthenticateAsync(context, cancellationToken);

                SaveAuthRecord(record);

                _credential = credential;
                _accountName = record.Username ?? "Azure Account";
                IsSignedIn = true;
                AuthStateChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsSigningIn = false;
            }
        }

        /// <summary>
        /// Clears credentials from memory and deletes persisted tokens.
        /// </summary>
        public void SignOut()
        {
            _credential = null;
            _accountName = null;
            IsSignedIn = false;
            DeleteAuthRecord();
            AzureResourceService.Instance.ClearClientCache();
            AuthStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private static InteractiveBrowserCredential CreateCredential(AuthenticationRecord authRecord)
        {
            var options = new InteractiveBrowserCredentialOptions
            {
                // Persist tokens to disk so silent sign-in works across VS restarts.
                // UnsafeAllowUnencryptedStorage is needed for .NET Framework 4.8
                // where the encrypted MSAL cache may not initialize properly.
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "AzureExplorer",
                    UnsafeAllowUnencryptedStorage = true,
                },
                // Allow the credential to acquire tokens for any tenant,
                // not just the home tenant — fixes empty subscription lists
                // for users with resources in work/guest tenants.
                AdditionallyAllowedTenants = { "*" },
            };

            if (authRecord != null)
            {
                options.AuthenticationRecord = authRecord;
            }

            return new InteractiveBrowserCredential(options);
        }

        private static async Task<AuthenticationRecord> LoadAuthRecordAsync()
        {
            try
            {
                // Offload file I/O to background thread to avoid blocking UI thread on startup
                return await Task.Run(() =>
                {
                    if (!File.Exists(_authRecordPath))
                        return null;

                    using (FileStream stream = File.OpenRead(_authRecordPath))
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

        private static void SaveAuthRecord(AuthenticationRecord record)
        {
            try
            {
                Directory.CreateDirectory(_cacheDir);
                using (FileStream stream = File.Create(_authRecordPath))
                {
                    record.Serialize(stream);
                }
            }
            catch
            {
                // Non-critical — user will just need to sign in again next session
            }
        }

        private static void DeleteAuthRecord()
        {
            try
            {
                if (File.Exists(_authRecordPath))
                    File.Delete(_authRecordPath);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
