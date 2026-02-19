namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Represents the provisioning state of an Azure resource.
    /// Used by resources that report Succeeded/Failed states (e.g., Key Vault, Storage Account).
    /// </summary>
    internal enum ProvisioningState
    {
        Unknown,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Helper methods for parsing provisioning state strings.
    /// </summary>
    internal static class ProvisioningStateParser
    {
        /// <summary>
        /// Parses a provisioning state string from Azure into a <see cref="ProvisioningState"/> enum value.
        /// </summary>
        public static ProvisioningState Parse(string state)
        {
            if (string.IsNullOrEmpty(state))
                return ProvisioningState.Unknown;

            if (state.Equals("Succeeded", System.StringComparison.OrdinalIgnoreCase))
                return ProvisioningState.Succeeded;

            if (state.Equals("Failed", System.StringComparison.OrdinalIgnoreCase))
                return ProvisioningState.Failed;

            return ProvisioningState.Unknown;
        }
    }
}
