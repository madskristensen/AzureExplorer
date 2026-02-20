using System.Collections.Generic;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.ResourceGroup.Dialogs
{
    /// <summary>
    /// Dialog for creating a new Azure Resource Group.
    /// </summary>
    internal partial class CreateResourceGroupDialog : DialogWindow
    {
        public CreateResourceGroupDialog()
        {
            InitializeComponent();
            ResourceGroupNameBox.Focus();
        }

        /// <summary>
        /// Gets the resource group name entered by the user.
        /// </summary>
        public string ResourceGroupName => ResourceGroupNameBox.Text?.Trim();

        /// <summary>
        /// Gets the selected Azure location.
        /// </summary>
        public AzureLocation SelectedLocation => LocationComboBox.SelectedItem as AzureLocation;

        /// <summary>
        /// Sets the available locations in the dropdown.
        /// </summary>
        public void SetLocations(IReadOnlyList<AzureLocation> locations)
        {
            LocationComboBox.ItemsSource = locations;
            if (locations.Count > 0)
            {
                LocationComboBox.SelectedIndex = 0;
            }
        }

        private void OnCreateClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResourceGroupName))
            {
                ResourceGroupNameBox.Focus();
                return;
            }

            if (SelectedLocation == null)
            {
                LocationComboBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
