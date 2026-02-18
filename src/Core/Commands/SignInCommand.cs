using AzureExplorer.Core.Services;

namespace AzureExplorer.Core.Commands
{
    [Command(PackageIds.SignIn)]
    internal sealed class SignInCommand : BaseCommand<SignInCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                await AzureAuthService.Instance.AddAccountAsync();
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

    [Command(PackageIds.SignOutAccount)]
    internal sealed class SignOutAccountCommand : BaseCommand<SignOutAccountCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (ToolWindows.AzureExplorerControl.SelectedNode is not Models.AccountNode accountNode)
                    return;

                AzureAuthService.Instance.SignOut(accountNode.AccountId);
                await VS.StatusBar.ShowMessageAsync($"Signed out from {accountNode.Label}.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Azure Sign Out", ex.Message);
            }
        }
    }
}
