using System.Reflection;
using System.Runtime.InteropServices;
using AzureExplorer;

[assembly: AssemblyTitle(Vsix.Name)]
[assembly: AssemblyDescription(Vsix.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(Vsix.Author)]
[assembly: AssemblyProduct(Vsix.Name)]
[assembly: AssemblyCopyright(Vsix.Author)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion(Vsix.Version)]
[assembly: AssemblyFileVersion(Vsix.Version)]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AzureExplorer.Test")]

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}
