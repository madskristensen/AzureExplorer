using System.ComponentModel;

namespace AzureExplorer.Core.Options
{
    /// <summary>
    /// General options for the Azure Explorer extension.
    /// </summary>
    internal sealed class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        /// <summary>
        /// Gets or sets whether to hide resource type nodes that have no children.
        /// When true, empty category nodes (e.g., "App Services" with 0 items) are hidden from the tree.
        /// </summary>
        [Category("Display")]
        [DisplayName("Hide Empty Resource Types")]
        [Description("When enabled, resource type categories with no resources are hidden from the tree view.")]
        [DefaultValue(false)]
        public bool HideEmptyResourceTypes { get; set; }
    }
}
