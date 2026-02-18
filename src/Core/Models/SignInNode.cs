using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Shown when the user is not signed in to Azure. Double-clicking triggers the sign-in command.
    /// </summary>
    internal sealed class SignInNode : ExplorerNodeBase
    {
        public SignInNode() : base("Sign in to Azure...") { }

        public override ImageMoniker IconMoniker => KnownMonikers.AddUser;
        public override int ContextMenuId => 0;
        public override bool SupportsChildren => false;
    }
}
