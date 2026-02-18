using Microsoft.VisualStudio.PlatformUI;

namespace AzureExplorer.Dialogs
{
    /// <summary>
    /// Dialog for updating the value of an existing secret in Azure Key Vault.
    /// </summary>
    public partial class UpdateSecretDialog : DialogWindow
    {
        public UpdateSecretDialog(string secretName)
        {
            InitializeComponent();
            SecretName = secretName;
            SecretNameLabel.Text = $"New value for '{secretName}':";
            SecretValueBox.Focus();
        }

        public string SecretName { get; }
        public string SecretValue => SecretValueBox.Password;

        private void OnUpdateClick(object sender, System.Windows.RoutedEventArgs e)
        {
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
