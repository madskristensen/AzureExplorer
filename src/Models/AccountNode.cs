using System.Threading;
using System.Threading.Tasks;
using Azure.ResourceManager.Resources;
using AzureExplorer.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Root node representing the signed-in Azure account.
    /// Children are <see cref="SubscriptionNode"/> instances.
    /// </summary>
    internal sealed class AccountNode : ExplorerNodeBase
    {
        public AccountNode(string accountName) : base(accountName)
        {
            // Add placeholder so the expand arrow is shown before children are loaded
            Children.Add(new LoadingNode());
        }

        public override ImageMoniker IconMoniker => KnownMonikers.Cloud;
        public override int ContextMenuId => 0;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                await foreach (SubscriptionResource sub in AzureResourceService.Instance.GetSubscriptionsAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddChild(new SubscriptionNode(sub.Data.DisplayName, sub.Data.SubscriptionId));
                }
            }
            finally
            {
                EndLoading();
            }
        }
    }
}
