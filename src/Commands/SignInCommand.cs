using AzureExplorer.Services;

namespace AzureExplorer
{
    [Command(PackageIds.SignIn)]
    internal sealed class SignInCommand : BaseCommand<SignInCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                await AzureAuthService.Instance.SignInAsync();
                await VS.StatusBar.ShowMessageAsync("Signed in to Azure successfully.");
            }
            catch (OperationCanceledException)
            {
                await VS.StatusBar.ShowMessageAsync("Azure sign-in was cancelled.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Azure Sign In", ex.Message);
            }
        }
    }
}
