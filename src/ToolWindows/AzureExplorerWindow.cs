using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows
{
    public class AzureExplorerWindow : BaseToolWindow<AzureExplorerWindow>
    {
        public override string GetTitle(int toolWindowId) => "Azure Explorer";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult<FrameworkElement>(new AzureExplorerControl());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Azure Explorer: Failed to create tool window content: {ex}");
                return Task.FromResult<FrameworkElement>(
                    new TextBlock { Text = $"Failed to load Azure Explorer:\n{ex.Message}", Margin = new Thickness(10) });
            }
        }

        [Guid("d4b65484-2b5e-4e73-b5a0-9c9f91e1dc21")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.AzureResourceGroup;
                ToolBar = new CommandID(PackageGuids.AzureExplorer, PackageIds.ToolWindowToolbar);
                ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;
            }
        }
    }
}
