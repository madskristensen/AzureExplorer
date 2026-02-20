global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;

global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using AzureExplorer.AppService.Services;
using AzureExplorer.ToolWindows;
using Microsoft.VisualStudio;

namespace AzureExplorer
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideKeyBindingTable(PackageGuids.AzureExplorerToolWindowString, 110)]
    [ProvideToolWindow(typeof(AzureExplorerWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
    [ProvideToolWindow(typeof(LogDocumentWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell, Transient = true, MultiInstances = true)]
    [ProvideToolWindowVisibility(typeof(AzureExplorerWindow.Pane), VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideToolWindowVisibility(typeof(AzureExplorerWindow.Pane), VSConstants.UICONTEXT.SolutionHasSingleProject_string)]
    [ProvideToolWindowVisibility(typeof(AzureExplorerWindow.Pane), VSConstants.UICONTEXT.SolutionHasMultipleProjects_string)]
    [ProvideToolWindowVisibility(typeof(AzureExplorerWindow.Pane), VSConstants.UICONTEXT.EmptySolution_string)]
    [Guid(PackageGuids.AzureExplorerString)]
    public sealed class AzureExplorerPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
            this.RegisterToolWindows();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stop all active log streams when VS closes
                LogStreamService.Stop();
            }

            base.Dispose(disposing);
        }
    }
}
