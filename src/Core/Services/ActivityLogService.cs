using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Tracks user activities performed through the Azure Explorer extension.
    /// Activities are persisted to disk and restored on startup.
    /// </summary>
    internal sealed class ActivityLogService
    {
        private const int MaxActivities = 100;
        private static readonly string StorageFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "VisualStudio", "AzureExplorer");
        private static readonly string StorageFile = Path.Combine(StorageFolder, "activity-log.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static ActivityLogService Instance { get; } = new();

        public ObservableCollection<ActivityEntry> Activities { get; } = [];

        private ActivityLogService()
        {
            LoadFromDisk();
        }

        /// <summary>
        /// Logs a new activity entry.
        /// </summary>
        public ActivityEntry LogActivity(string action, string resourceName, string resourceType, string resourceId = null)
        {
            var entry = new ActivityEntry
            {
                Action = action,
                ResourceName = resourceName,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Status = ActivityStatus.InProgress,
                Timestamp = DateTime.Now
            };

            // Subscribe to property changes to save when status updates
            entry.PropertyChanged += OnEntryPropertyChanged;

            // Insert at the top so newest is first
            Activities.Insert(0, entry);

            // Enforce maximum limit
            while (Activities.Count > MaxActivities)
            {
                var removed = Activities[Activities.Count - 1];
                removed.PropertyChanged -= OnEntryPropertyChanged;
                Activities.RemoveAt(Activities.Count - 1);
            }

            SaveToDisk();
            return entry;
        }

        /// <summary>
        /// Logs a completed activity (success or failure).
        /// </summary>
        public ActivityEntry LogActivity(string action, string resourceName, string resourceType, ActivityStatus status, string errorMessage = null, string resourceId = null)
        {
            var entry = new ActivityEntry
            {
                Action = action,
                ResourceName = resourceName,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Status = status,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.Now
            };

            entry.PropertyChanged += OnEntryPropertyChanged;
            Activities.Insert(0, entry);

            while (Activities.Count > MaxActivities)
            {
                var removed = Activities[Activities.Count - 1];
                removed.PropertyChanged -= OnEntryPropertyChanged;
                Activities.RemoveAt(Activities.Count - 1);
            }

            SaveToDisk();
            return entry;
        }

        /// <summary>
        /// Clears all activities from the log.
        /// </summary>
        public void Clear()
        {
            foreach (var entry in Activities)
            {
                entry.PropertyChanged -= OnEntryPropertyChanged;
            }
            Activities.Clear();
            SaveToDisk();
        }

        private void OnEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Save when status changes (e.g., InProgress -> Success/Failed)
            if (e.PropertyName == nameof(ActivityEntry.Status))
            {
                SaveToDisk();
            }
        }

        private void SaveToDisk()
        {
            try
            {
                Directory.CreateDirectory(StorageFolder);

                var data = new List<ActivityEntryData>();
                foreach (var entry in Activities)
                {
                    data.Add(new ActivityEntryData
                    {
                        Action = entry.Action,
                        ResourceName = entry.ResourceName,
                        ResourceType = entry.ResourceType,
                        ResourceId = entry.ResourceId,
                        Status = entry.Status,
                        ErrorMessage = entry.ErrorMessage,
                        Timestamp = entry.Timestamp
                    });
                }

                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(StorageFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save activity log: {ex.Message}");
            }
        }

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(StorageFile))
                    return;

                string json = File.ReadAllText(StorageFile);
                var data = JsonSerializer.Deserialize<List<ActivityEntryData>>(json, JsonOptions);

                if (data == null)
                    return;

                foreach (var item in data)
                {
                    var entry = new ActivityEntry
                    {
                        Action = item.Action,
                        ResourceName = item.ResourceName,
                        ResourceType = item.ResourceType,
                        ResourceId = item.ResourceId,
                        Status = item.Status,
                        ErrorMessage = item.ErrorMessage,
                        Timestamp = item.Timestamp
                    };
                    entry.PropertyChanged += OnEntryPropertyChanged;
                    Activities.Add(entry);

                    if (Activities.Count >= MaxActivities)
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load activity log: {ex.Message}");
            }
        }

        /// <summary>
        /// Simple data class for JSON serialization (without UI properties).
        /// </summary>
        private sealed class ActivityEntryData
        {
            public string Action { get; set; }
            public string ResourceName { get; set; }
            public string ResourceType { get; set; }
            public string ResourceId { get; set; }
            public ActivityStatus Status { get; set; }
            public string ErrorMessage { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    /// <summary>
    /// Represents a single activity entry in the Activity Log.
    /// </summary>
    internal sealed class ActivityEntry : INotifyPropertyChanged
    {
        private ActivityStatus _status;
        private string _errorMessage;

        public string Action { get; set; }
        public string ResourceName { get; set; }
        public string ResourceType { get; set; }
        public string ResourceId { get; set; }
        public DateTime Timestamp { get; set; }

        public ActivityStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Description));
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        [JsonIgnore]
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        [JsonIgnore]
        public ImageMoniker StatusIcon => Status switch
        {
            ActivityStatus.Success => KnownMonikers.StatusOK,
            ActivityStatus.Failed => KnownMonikers.StatusError,
            ActivityStatus.InProgress => KnownMonikers.StatusRunning,
            _ => KnownMonikers.StatusInformation
        };

        [JsonIgnore]
        public ImageMoniker ResourceIcon => ResourceType switch
        {
            "AppService" => KnownMonikers.WebApplication,
            "FunctionApp" => KnownMonikers.AzureFunctionsApp,
            "Blob" => KnownMonikers.Document,
            "Container" => KnownMonikers.FolderClosed,
            "VM" => KnownMonikers.VirtualMachine,
            "KeyVault" => KnownMonikers.Key,
            "Secret" => KnownMonikers.HiddenField,
            "StorageAccount" => KnownMonikers.AzureStorageAccount,
            "SqlServer" => KnownMonikers.Database,
            "SqlDatabase" => KnownMonikers.Database,
            _ => KnownMonikers.AzureResourceGroup
        };

        [JsonIgnore]
        public string Description
        {
            get
            {
                var baseText = $"{Action} \"{ResourceName}\"";
                return Status == ActivityStatus.Failed && HasError
                    ? $"{baseText} - {ErrorMessage}"
                    : baseText;
            }
        }

        [JsonIgnore]
        public string TimeAgo
        {
            get
            {
                var elapsed = DateTime.Now - Timestamp;
                if (elapsed.TotalSeconds < 60) return "just now";
                if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
                if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
                return Timestamp.ToString("MMM d");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Marks this activity as completed successfully.
        /// </summary>
        public void Complete()
        {
            Status = ActivityStatus.Success;
        }

        /// <summary>
        /// Marks this activity as failed with an error message.
        /// </summary>
        public void Fail(string errorMessage)
        {
            ErrorMessage = errorMessage;
            Status = ActivityStatus.Failed;
        }
    }

    internal enum ActivityStatus
    {
        InProgress,
        Success,
        Failed
    }
}
