using System.Collections.Generic;
using System.Text.RegularExpressions;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.Storage.Dialogs
{
    /// <summary>
    /// Dialog for creating a new Azure Storage Account.
    /// </summary>
    internal partial class CreateStorageAccountDialog : DialogWindow
    {
        private static readonly Regex ValidNamePattern = new("^[a-z0-9]+$", RegexOptions.Compiled);

        public CreateStorageAccountDialog()
        {
            InitializeComponent();
            InitializeSkuOptions();
            AccountNameBox.Focus();
        }

        /// <summary>
        /// Gets the storage account name entered by the user.
        /// </summary>
        public string AccountName => AccountNameBox.Text?.Trim();

        /// <summary>
        /// Gets the selected Azure location.
        /// </summary>
        public AzureLocation SelectedLocation => LocationComboBox.SelectedItem as AzureLocation;

        /// <summary>
        /// Gets the selected SKU option.
        /// </summary>
        public StorageSkuOption SelectedSku => SkuComboBox.SelectedItem as StorageSkuOption;

        /// <summary>
        /// Sets the available locations in the dropdown, optionally pre-selecting a default.
        /// </summary>
        /// <param name="locations">Available Azure locations.</param>
        /// <param name="defaultLocationName">The location name to pre-select (e.g., "eastus").</param>
        public void SetLocations(IReadOnlyList<AzureLocation> locations, string defaultLocationName = null)
        {
            LocationComboBox.ItemsSource = locations;

            // Try to select the default location (e.g., resource group's location)
            if (!string.IsNullOrEmpty(defaultLocationName))
            {
                for (int i = 0; i < locations.Count; i++)
                {
                    if (string.Equals(locations[i].Name, defaultLocationName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        LocationComboBox.SelectedIndex = i;
                        return;
                    }
                }
            }

            // Fall back to first item
            if (locations.Count > 0)
            {
                LocationComboBox.SelectedIndex = 0;
            }
        }

        private void InitializeSkuOptions()
        {
            var skuOptions = new List<StorageSkuOption>
            {
                new("Standard_LRS", "Standard / Locally-redundant (LRS)", "Lowest cost, data replicated 3x within one datacenter"),
                new("Standard_GRS", "Standard / Geo-redundant (GRS)", "Data replicated to a secondary region"),
                new("Standard_ZRS", "Standard / Zone-redundant (ZRS)", "Data replicated across 3 availability zones"),
                new("Premium_LRS", "Premium / Locally-redundant (LRS)", "SSD-based, high performance, locally redundant")
            };

            SkuComboBox.ItemsSource = skuOptions;
            SkuComboBox.SelectedIndex = 0;
            SkuComboBox.SelectionChanged += OnSkuSelectionChanged;
            UpdateSkuDescription();
        }

        private void OnSkuSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateSkuDescription();
        }

        private void UpdateSkuDescription()
        {
            if (SelectedSku != null)
            {
                SkuDescriptionText.Text = SelectedSku.Description;
            }
        }

        private void OnCreateClick(object sender, System.Windows.RoutedEventArgs e)
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                AccountNameBox.Focus();
                return;
            }

            if (AccountName.Length < 3 || AccountName.Length > 24)
            {
                AccountNameBox.Focus();
                return;
            }

            if (!ValidNamePattern.IsMatch(AccountName))
            {
                AccountNameBox.Focus();
                return;
            }

            if (SelectedLocation == null)
            {
                LocationComboBox.Focus();
                return;
            }

            if (SelectedSku == null)
            {
                SkuComboBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Represents a storage account SKU option for the dropdown.
    /// </summary>
    internal sealed class StorageSkuOption
    {
        public StorageSkuOption(string skuName, string displayName, string description)
        {
            SkuName = skuName;
            DisplayName = displayName;
            Description = description;
        }

        public string SkuName { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public override string ToString() => DisplayName;
    }
}
