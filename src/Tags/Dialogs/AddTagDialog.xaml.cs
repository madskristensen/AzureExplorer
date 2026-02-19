using System.Collections.Generic;
using System.Windows;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.Tags.Dialogs
{
    /// <summary>
    /// Dialog for adding a tag to an Azure resource.
    /// Provides editable ComboBoxes that auto-populate from previously used tags.
    /// </summary>
    public partial class AddTagDialog : DialogWindow
    {
        private readonly TagService _tagService = TagService.Instance;

        public AddTagDialog()
        {
            InitializeComponent();

            // Pre-populate key dropdown with known keys
            RefreshKeyDropdown();
        }

        /// <summary>
        /// Gets the tag key entered by the user.
        /// </summary>
        public string TagKey => KeyComboBox.Text?.Trim();

        /// <summary>
        /// Gets the tag value entered by the user.
        /// </summary>
        public string TagValue => ValueComboBox.Text?.Trim();

        private void RefreshKeyDropdown()
        {
            IReadOnlyList<string> keys = _tagService.GetAllTagKeys();
            KeyComboBox.ItemsSource = keys;
        }

        private void RefreshValueDropdown()
        {
            var currentKey = KeyComboBox.Text?.Trim();
            if (string.IsNullOrEmpty(currentKey))
            {
                ValueComboBox.ItemsSource = null;
                return;
            }

            // Get values previously used for this key
            IReadOnlyList<string> values = _tagService.GetValuesForKey(currentKey);
            ValueComboBox.ItemsSource = values;
        }

        private void KeyComboBox_DropDownOpened(object sender, EventArgs e)
        {
            RefreshKeyDropdown();
        }

        private void KeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // When key changes, refresh value suggestions for that key
            RefreshValueDropdown();
        }

        private void ValueComboBox_DropDownOpened(object sender, EventArgs e)
        {
            RefreshValueDropdown();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(TagKey))
            {
                VS.MessageBox.ShowWarningAsync("Add Tag", "Please enter a tag key.").FireAndForget();
                KeyComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TagValue))
            {
                VS.MessageBox.ShowWarningAsync("Add Tag", "Please enter a tag value.").FireAndForget();
                ValueComboBox.Focus();
                return;
            }

            // Validate key format (Azure tag keys have restrictions)
            if (TagKey.Length > 512)
            {
                VS.MessageBox.ShowWarningAsync("Add Tag", "Tag key cannot exceed 512 characters.").FireAndForget();
                KeyComboBox.Focus();
                return;
            }

            if (TagValue.Length > 256)
            {
                VS.MessageBox.ShowWarningAsync("Add Tag", "Tag value cannot exceed 256 characters.").FireAndForget();
                ValueComboBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
