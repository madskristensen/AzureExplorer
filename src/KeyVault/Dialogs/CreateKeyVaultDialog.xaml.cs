using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.KeyVault.Dialogs
{
    /// <summary>
    /// Dialog for creating a new Azure Key Vault.
    /// </summary>
    internal partial class CreateKeyVaultDialog : DialogWindow
    {
        // Key Vault names: 3-24 chars, alphanumeric and hyphens, must start with letter, can't end with hyphen
        private static readonly Regex _validNameRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9-]{1,22}[a-zA-Z0-9]$", RegexOptions.Compiled);

        public CreateKeyVaultDialog()
        {
            InitializeComponent();
            InitializeSkus();
        }

        /// <summary>
        /// Gets the entered Key Vault name.
        /// </summary>
        public string VaultName => VaultNameBox.Text.Trim();

        /// <summary>
        /// Gets the selected location.
        /// </summary>
        public AzureLocation SelectedLocation => LocationComboBox.SelectedItem as AzureLocation;

        /// <summary>
        /// Gets the selected SKU.
        /// </summary>
        public KeyVaultSkuOption SelectedSku => SkuComboBox.SelectedItem as KeyVaultSkuOption;

        /// <summary>
        /// Sets the available locations in the dropdown.
        /// </summary>
        public void SetLocations(IReadOnlyList<AzureLocation> locations, string defaultLocationName = null)
        {
            LocationComboBox.ItemsSource = locations;

            // Try to select the default location (resource group's location)
            if (!string.IsNullOrEmpty(defaultLocationName))
            {
                foreach (AzureLocation loc in locations)
                {
                    if (loc.Name == defaultLocationName)
                    {
                        LocationComboBox.SelectedItem = loc;
                        break;
                    }
                }
            }

            // If no default selected, pick first
            if (LocationComboBox.SelectedItem == null && locations.Count > 0)
            {
                LocationComboBox.SelectedIndex = 0;
            }

            ValidateInput();
        }

        private void InitializeSkus()
        {
            var skus = new List<KeyVaultSkuOption>
            {
                new("standard", "Standard", "Software-protected keys, secrets, and certificates"),
                new("premium", "Premium", "HSM-protected keys + all Standard features"),
            };

            SkuComboBox.ItemsSource = skus;
            SkuComboBox.SelectedIndex = 0;
        }

        private void OnVaultNameChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void OnLocationChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            string name = VaultName;
            bool isValid = !string.IsNullOrWhiteSpace(name) &&
                           name.Length >= 3 &&
                           name.Length <= 24 &&
                           _validNameRegex.IsMatch(name) &&
                           SelectedLocation != null &&
                           SelectedSku != null;

            CreateButton.IsEnabled = isValid;
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Represents a Key Vault SKU option in the dropdown.
    /// </summary>
    internal sealed class KeyVaultSkuOption
    {
        public KeyVaultSkuOption(string skuName, string displayName, string description)
        {
            SkuName = skuName;
            DisplayName = displayName;
            Description = description;
        }

        public string SkuName { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public override string ToString() => $"{DisplayName} - {Description}";
    }
}
