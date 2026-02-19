using System.Collections.Generic;
using System.Linq;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Interface for Azure resources that support tags.
    /// </summary>
    internal interface ITaggableResource
    {
        /// <summary>
        /// The resource's Azure tags (key-value pairs), excluding system tags.
        /// </summary>
        IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>
        /// Gets a formatted tooltip string showing all tags.
        /// </summary>
        string TagsTooltip { get; }

        /// <summary>
        /// Checks if the resource has a specific tag with optional value match.
        /// </summary>
        /// <param name="key">The tag key to check for.</param>
        /// <param name="value">Optional value to match. If null, only checks for key existence.</param>
        /// <returns>True if the tag exists and matches the value (if specified).</returns>
        bool HasTag(string key, string value = null);
    }

    /// <summary>
    /// Extension methods for ITaggableResource.
    /// </summary>
    internal static class TaggableResourceExtensions
    {
        /// <summary>
        /// Azure system tag prefixes that should be hidden from users.
        /// These are internal tags used by Azure for resource linking and management.
        /// </summary>
        private static readonly string[] _systemTagPrefixes =
        [
            "hidden-",          // hidden-related, hidden-link, etc.
            "ms-resource-",     // Microsoft internal resource tags
        ];

        /// <summary>
        /// Filters out Azure system/internal tags that aren't useful for users.
        /// </summary>
        public static IDictionary<string, string> FilterUserTags(this IDictionary<string, string> tags)
        {
            if (tags == null || tags.Count == 0)
                return tags;

            var filtered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> tag in tags)
            {
                // Skip system tags
                if (IsSystemTag(tag.Key))
                    continue;

                // Skip tags with empty values
                if (string.IsNullOrWhiteSpace(tag.Value))
                    continue;

                filtered[tag.Key] = tag.Value;
            }

            return filtered;
        }

        /// <summary>
        /// Checks if a tag key is an Azure system/internal tag.
        /// </summary>
        private static bool IsSystemTag(string key)
        {
            if (string.IsNullOrEmpty(key))
                return true;

            foreach (var prefix in _systemTagPrefixes)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a tooltip string from tags dictionary.
        /// </summary>
        public static string FormatTagsTooltip(this IReadOnlyDictionary<string, string> tags)
        {
            if (tags == null || tags.Count == 0)
                return null;

            return string.Join("\n", tags
                .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .Select(t => $"{t.Key}: {t.Value}"));
        }

        /// <summary>
        /// Checks if a tags dictionary contains a specific tag (case-insensitive).
        /// </summary>
        public static bool ContainsTag(this IReadOnlyDictionary<string, string> tags, string key, string value = null)
        {
            if (tags == null || string.IsNullOrEmpty(key))
                return false;

            // Case-insensitive key lookup
            foreach (KeyValuePair<string, string> kvp in tags)
            {
                if (kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    // Key matches - check value if specified
                    return value == null || kvp.Value.Equals(value, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}
