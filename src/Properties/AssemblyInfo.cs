using System.Reflection;
using System.Runtime.InteropServices;
using AzureExplorer;

// Force VS to load Azure SDK assemblies from the VSIX package folder instead of VS's own versions
[assembly: ProvideCodeBase(AssemblyName = "Azure.Core")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Identity")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Identity.Broker")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.AppService")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.Cdn")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.Compute")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.KeyVault")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.Network")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.ResourceGraph")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.Sql")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.ResourceManager.Storage")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Storage.Blobs")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Storage.Queues")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Storage.Common")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Data.Tables")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Security.KeyVault.Keys")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Security.KeyVault.Certificates")]
[assembly: ProvideCodeBase(AssemblyName = "Azure.Security.KeyVault.Secrets")]
[assembly: ProvideCodeBase(AssemblyName = "System.Memory.Data")]
[assembly: ProvideCodeBase(AssemblyName = "System.ClientModel")]

// Redirect all old Azure SDK versions to the ones we ship - this handles version mismatches
[assembly: ProvideBindingRedirection(AssemblyName = "Azure.Core", OldVersionLowerBound = "1.0.0.0", OldVersionUpperBound = "1.50.0.0", NewVersion = "1.50.0.0")]
[assembly: ProvideBindingRedirection(AssemblyName = "Azure.Identity", OldVersionLowerBound = "1.0.0.0", OldVersionUpperBound = "1.17.1.0", NewVersion = "1.17.1.0")]
[assembly: ProvideBindingRedirection(AssemblyName = "Azure.ResourceManager", OldVersionLowerBound = "1.0.0.0", OldVersionUpperBound = "1.13.2.0", NewVersion = "1.13.2.0")]

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
