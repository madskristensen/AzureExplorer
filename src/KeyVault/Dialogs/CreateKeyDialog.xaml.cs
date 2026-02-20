using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.KeyVault.Dialogs
{
    /// <summary>
    /// Dialog for creating a new cryptographic key in Azure Key Vault.
    /// </summary>
    public partial class CreateKeyDialog : DialogWindow
    {
        private static readonly Regex _validKeyNameRegex = new Regex(@"^[a-zA-Z0-9-]+$", RegexOptions.Compiled);

        public CreateKeyDialog()
        {
            InitializeComponent();
            InitializeKeyTypes();
        }

        /// <summary>
        /// Gets the entered key name.
        /// </summary>
        public string KeyName => KeyNameBox.Text.Trim();

        /// <summary>
        /// Gets the selected key type.
        /// </summary>
        public KeyTypeOption SelectedKeyType => KeyTypeComboBox.SelectedItem as KeyTypeOption;

        /// <summary>
        /// Gets the selected key size.
        /// </summary>
        public KeySizeOption SelectedKeySize => KeySizeComboBox.SelectedItem as KeySizeOption;

        private void InitializeKeyTypes()
        {
            var keyTypes = new List<KeyTypeOption>
            {
                new("RSA", "RSA", "Asymmetric encryption and signing"),
                new("EC", "EC", "Elliptic curve cryptography"),
            };

            KeyTypeComboBox.ItemsSource = keyTypes;
            KeyTypeComboBox.SelectedIndex = 0;
        }

        private void UpdateKeySizes()
        {
            if (SelectedKeyType == null)
                return;

            List<KeySizeOption> sizes;

            if (SelectedKeyType.Value == "RSA")
            {
                sizes = new List<KeySizeOption>
                {
                    new(2048, "2048 bits (recommended)"),
                    new(3072, "3072 bits"),
                    new(4096, "4096 bits (high security)"),
                };
            }
            else // EC
            {
                sizes = new List<KeySizeOption>
                {
                    new("P-256", "P-256 (recommended)"),
                    new("P-384", "P-384"),
                    new("P-521", "P-521 (high security)"),
                };
            }

            KeySizeComboBox.ItemsSource = sizes;
            KeySizeComboBox.SelectedIndex = 0;
        }

        private void OnKeyTypeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateKeySizes();
            ValidateInput();
        }

        private void OnKeyNameChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            bool isValid = !string.IsNullOrWhiteSpace(KeyName) &&
                           _validKeyNameRegex.IsMatch(KeyName) &&
                           SelectedKeyType != null &&
                           SelectedKeySize != null;

            CreateButton.IsEnabled = isValid;
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Represents a key type option in the dropdown.
    /// </summary>
    public sealed class KeyTypeOption
    {
        public KeyTypeOption(string value, string displayName, string description)
        {
            Value = value;
            DisplayName = displayName;
            Description = description;
        }

        public string Value { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public override string ToString() => $"{DisplayName} - {Description}";
    }

    /// <summary>
    /// Represents a key size option in the dropdown.
    /// </summary>
    public sealed class KeySizeOption
    {
        public KeySizeOption(int sizeInBits, string displayName)
        {
            SizeInBits = sizeInBits;
            CurveName = null;
            DisplayName = displayName;
        }

        public KeySizeOption(string curveName, string displayName)
        {
            SizeInBits = null;
            CurveName = curveName;
            DisplayName = displayName;
        }

        /// <summary>
        /// Key size in bits (for RSA keys).
        /// </summary>
        public int? SizeInBits { get; }

        /// <summary>
        /// Curve name (for EC keys).
        /// </summary>
        public string CurveName { get; }

        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }
}
