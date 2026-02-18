using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.Dialogs
{
    /// <summary>
    /// Dialog for adding a new secret to an Azure Key Vault.
    /// </summary>
    public partial class AddSecretDialog : DialogWindow
    {
        public AddSecretDialog()
        {
            InitializeComponent();
            SecretNameBox.Focus();
        }

        public string SecretName => SecretNameBox.Text?.Trim();
        public string SecretValue => SecretValueBox.Password;

        private void OnAddClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SecretName))
            {
                SecretNameBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(SecretValue))
            {
                SecretValueBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
