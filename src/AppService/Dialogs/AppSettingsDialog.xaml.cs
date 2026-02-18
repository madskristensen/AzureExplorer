using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.AppService.Dialogs
{
    /// <summary>
    /// Dialog for viewing and editing Azure App Service application settings.
    /// </summary>
    public partial class AppSettingsDialog : DialogWindow
    {
        public AppSettingsDialog(string appServiceName, Dictionary<string, string> settings)
        {
            InitializeComponent();
            Title = $"App Settings - {appServiceName}";

            Settings = new ObservableCollection<AppSettingItem>(
                settings.OrderBy(kvp => kvp.Key)
                        .Select(kvp => new AppSettingItem(kvp.Key, kvp.Value)));

            SettingsGrid.ItemsSource = Settings;
        }

        public ObservableCollection<AppSettingItem> Settings { get; }

        /// <summary>
        /// Gets the settings as a dictionary for saving.
        /// </summary>
        public Dictionary<string, string> GetSettingsDictionary()
        {
            var result = new Dictionary<string, string>();
            foreach (AppSettingItem item in Settings)
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    result[item.Name] = item.Value ?? string.Empty;
                }
            }
            return result;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var newItem = new AppSettingItem("NEW_SETTING", string.Empty);
            Settings.Add(newItem);
            SettingsGrid.SelectedItem = newItem;
            SettingsGrid.ScrollIntoView(newItem);
            SettingsGrid.BeginEdit();
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (SettingsGrid.SelectedItem is AppSettingItem selectedItem)
            {
                Settings.Remove(selectedItem);
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            // Validate: check for empty names or duplicates
            var names = new HashSet<string>();
            foreach (AppSettingItem item in Settings)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    System.Windows.MessageBox.Show("Setting names cannot be empty.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!names.Add(item.Name))
                {
                    System.Windows.MessageBox.Show($"Duplicate setting name: {item.Name}", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Represents a single app setting key-value pair for data binding.
    /// </summary>
    public sealed class AppSettingItem(string name, string value) : INotifyPropertyChanged
    {
        private string _value = value;

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
