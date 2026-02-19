using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using AzureExplorer.Core.Models;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Service for aggregating and filtering tags across all loaded resources.
    /// Lazily collects tags as resources are loaded into the tree.
    /// </summary>
    internal sealed class TagService
    {
        private static readonly Lazy<TagService> _instance = new(() => new TagService());

        /// <summary>
        /// All known tag key-value pairs collected from loaded resources.
        /// Key format: "tagKey=tagValue" for quick lookup.
        /// </summary>
        private readonly ConcurrentDictionary<string, TagInfo> _knownTags = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Currently active tag filter. Null means no filter.
        /// </summary>
        private TagFilter _activeFilter;

        private TagService() { }

        public static TagService Instance => _instance.Value;

        /// <summary>
        /// Event raised when the known tags collection changes.
        /// </summary>
        public event EventHandler TagsChanged;

        /// <summary>
        /// Event raised when the active filter changes.
        /// </summary>
        public event EventHandler<TagFilter> FilterChanged;

        /// <summary>
        /// Gets the currently active tag filter, or null if no filter is active.
        /// </summary>
        public TagFilter ActiveFilter => _activeFilter;

        /// <summary>
        /// Registers tags from a resource into the known tags collection.
        /// Call this when loading resources that implement ITaggableResource.
        /// </summary>
        public void RegisterTags(IReadOnlyDictionary<string, string> tags)
        {
            if (tags == null || tags.Count == 0)
                return;

            var changed = false;

            foreach (KeyValuePair<string, string> tag in tags)
            {
                var key = $"{tag.Key}={tag.Value}";
                if (_knownTags.TryAdd(key, new TagInfo(tag.Key, tag.Value)))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                TagsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets all unique tag keys.
        /// </summary>
        public IReadOnlyList<string> GetAllTagKeys()
        {
            return _knownTags.Values
                .Select(t => t.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Gets all known tag key-value pairs, grouped by key.
        /// </summary>
        public IReadOnlyList<TagInfo> GetAllTags()
        {
            return _knownTags.Values
                .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Gets all values for a specific tag key.
        /// </summary>
        public IReadOnlyList<string> GetValuesForKey(string key)
        {
            return _knownTags.Values
                .Where(t => t.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Sets the active tag filter.
        /// </summary>
        /// <param name="key">Tag key to filter by.</param>
        /// <param name="value">Optional tag value. If null, filters by key only.</param>
        public void SetFilter(string key, string value = null)
        {
            _activeFilter = string.IsNullOrEmpty(key) ? null : new TagFilter(key, value);
            FilterChanged?.Invoke(this, _activeFilter);
        }

        /// <summary>
        /// Clears the active tag filter.
        /// </summary>
        public void ClearFilter()
        {
            _activeFilter = null;
            FilterChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Checks if a resource matches the current filter.
        /// Returns true if no filter is active or if the resource matches.
        /// </summary>
        public bool MatchesFilter(ITaggableResource resource)
        {
            if (_activeFilter == null)
                return true;

            if (resource == null)
                return false;

            return resource.HasTag(_activeFilter.Key, _activeFilter.Value);
        }

        /// <summary>
        /// Clears all known tags. Call when signing out or refreshing.
        /// </summary>
        public void Clear()
        {
            _knownTags.Clear();
            _activeFilter = null;
            TagsChanged?.Invoke(this, EventArgs.Empty);
            FilterChanged?.Invoke(this, null);
        }
    }

    /// <summary>
    /// Represents a tag key-value pair.
    /// </summary>
    internal sealed class TagInfo(string key, string value)
    {
        public string Key { get; } = key;
        public string Value { get; } = value;

        /// <summary>
        /// Display format for UI: "Key = Value"
        /// </summary>
        public string DisplayText => $"{Key} = {Value}";

        /// <summary>
        /// Filter format: "Key=Value"
        /// </summary>
        public string FilterKey => $"{Key}={Value}";

        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// Represents an active tag filter.
    /// </summary>
    internal sealed class TagFilter(string key, string value = null)
    {
        public string Key { get; } = key;
        public string Value { get; } = value;

        /// <summary>
        /// Display text for the filter.
        /// </summary>
        public string DisplayText => Value != null ? $"{Key} = {Value}" : Key;

        public override string ToString() => DisplayText;
    }
}
