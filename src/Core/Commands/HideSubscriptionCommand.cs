using AzureExplorer.Core.Models;
using AzureExplorer.Core.Options;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Context menu command to hide or unhide a subscription.
    /// Hidden subscriptions are filtered from the tree unless ShowAll is enabled.
    /// </summary>
    [Command(PackageIds.HideSubscription)]
    internal sealed class HideSubscriptionCommand : BaseCommand<HideSubscriptionCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Visible = false;

            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;
            if (selectedNode is not SubscriptionNode subscriptionNode)
            {
                return;
            }

            Command.Visible = true;

            // Change text based on current hidden state
            Command.Text = subscriptionNode.IsHidden ? "Unhide Subscription" : "Hide Subscription";
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;
            if (selectedNode is not SubscriptionNode subscriptionNode)
            {
                return;
            }

            try
            {
                GeneralOptions options = GeneralOptions.Instance;
                var wasHidden = subscriptionNode.IsHidden;

                // Toggle the hidden state
                options.ToggleSubscriptionHidden(subscriptionNode.SubscriptionId);
                await options.SaveAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (wasHidden)
                {
                    // Subscription was unhidden - update visibility to show it normally
                    subscriptionNode.NotifyVisibilityChanged();
                    await VS.StatusBar.ShowMessageAsync($"Subscription '{subscriptionNode.Label}' is now visible.");
                }
                else
                {
                    // Subscription was hidden
                    if (options.ShowAll)
                    {
                        // ShowAll is enabled, just dim it
                        subscriptionNode.NotifyVisibilityChanged();
                        await VS.StatusBar.ShowMessageAsync($"Subscription '{subscriptionNode.Label}' is now hidden (visible because Show All is enabled).");
                    }
                    else
                    {
                        // ShowAll is disabled, hide it via visibility binding
                        subscriptionNode.NotifyVisibilityChanged();
                        await VS.StatusBar.ShowMessageAsync($"Subscription '{subscriptionNode.Label}' is now hidden.");
                    }
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to toggle subscription visibility: {ex.Message}");
            }
        }
    }
}
