global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;

global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using AzureExplorer.ToolWindows;
using Microsoft.VisualStudio;

namespace AzureExplorer
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(AzureExplorerWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
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
    }
}
