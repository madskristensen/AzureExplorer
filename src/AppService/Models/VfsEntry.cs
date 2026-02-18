using Newtonsoft.Json;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Represents an entry returned by the Kudu VFS API.
    /// </summary>
    internal sealed class VfsEntry
    {
        /// <summary>
        /// The name of the file or directory.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The size of the file in bytes. Zero for directories.
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// The last modified time.
        /// </summary>
        [JsonProperty("mtime")]
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// The creation time.
        /// </summary>
        [JsonProperty("crtime")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// The MIME type. "inode/directory" for directories.
        /// </summary>
        [JsonProperty("mime")]
        public string Mime { get; set; }

        /// <summary>
        /// The full URL to access this entry via the VFS API.
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }

        /// <summary>
        /// The path relative to the VFS root.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Returns true if this entry represents a directory.
        /// </summary>
        public bool IsDirectory => string.Equals(Mime, "inode/directory", StringComparison.OrdinalIgnoreCase);
    }
}
