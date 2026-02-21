using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Tracks user activities performed through the Azure Explorer extension.
    /// Activities are persisted to disk and restored on startup.
    /// </summary>
    internal sealed class ActivityLogService : IDisposable
    {
        private const int _maxActivities = 100;
        private const int _saveDebounceMs = 500;
        private static readonly string _storageFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "VisualStudio", "AzureExplorer");
        private static readonly string _storageFile = Path.Combine(_storageFolder, "activity-log.json");

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly object _lock = new();
        private CancellationTokenSource _saveCts;
        private bool _disposed;
        private bool _isLoaded;

        public static ActivityLogService Instance { get; } = new();

        public ObservableCollection<ActivityEntry> Activities { get; } = [];

        /// <summary>
        /// Gets whether the activity log has been loaded from disk.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        private ActivityLogService()
        {
            // Don't load from disk in constructor - defer to LoadAsync() for better startup perf
            SubscribeToResourceEvents();
        }

        /// <summary>
        /// Subscribes to resource lifecycle events to automatically log creations and deletions.
        /// </summary>
        private void SubscribeToResourceEvents()
        {
            ResourceNotificationService.ResourceCreated += OnResourceCreated;
            ResourceNotificationService.ResourceDeleted += OnResourceDeleted;
        }

        /// <summary>
        /// Unsubscribes from resource lifecycle events to prevent memory leaks.
        /// </summary>
        private void UnsubscribeFromResourceEvents()
        {
            ResourceNotificationService.ResourceCreated -= OnResourceCreated;
            ResourceNotificationService.ResourceDeleted -= OnResourceDeleted;
        }

        /// <summary>
        /// Releases resources and unsubscribes from static events.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            UnsubscribeFromResourceEvents();

            // Unsubscribe from all activity entry events
            foreach (var entry in Activities)
            {
                entry.PropertyChanged -= OnEntryPropertyChanged;
            }

            _disposed = true;
        }

        private void OnResourceCreated(object sender, ResourceCreatedEventArgs e)
        {
            string resourceType = GetFriendlyResourceType(e.ResourceType);

            // Check if there's an in-progress entry for this resource and update it
            var existingEntry = FindInProgressEntry("Creating", e.ResourceName, resourceType);
            if (existingEntry != null)
            {
                existingEntry.Complete();
            }
            else
            {
                // Log as immediate success if no in-progress entry exists
                LogActivity("Created", e.ResourceName, resourceType, ActivityStatus.Success, resourceId: e.ResourceType);
            }
        }

        private void OnResourceDeleted(object sender, ResourceDeletedEventArgs e)
        {
            string resourceType = GetFriendlyResourceType(e.ResourceType);

            // Check if there's an in-progress entry for this resource and update it
            var existingEntry = FindInProgressEntry("Deleting", e.ResourceName, resourceType);
            if (existingEntry != null)
            {
                existingEntry.Complete();
            }
            else
            {
                // Log as immediate success if no in-progress entry exists
                LogActivity("Deleted", e.ResourceName, resourceType, ActivityStatus.Success, resourceId: e.ResourceType);
            }
        }

        /// <summary>
        /// Finds an existing in-progress activity entry for the specified resource.
        /// </summary>
        private ActivityEntry FindInProgressEntry(string action, string resourceName, string resourceType)
        {
            return Activities.FirstOrDefault(a =>
                a.Status == ActivityStatus.InProgress &&
                a.Action == action &&
                a.ResourceName == resourceName &&
                a.ResourceType == resourceType);
        }

        /// <summary>
        /// Converts Azure resource provider types to friendly display names.
        /// </summary>
        private static string GetFriendlyResourceType(string resourceType)
        {
            return resourceType switch
            {
                "Microsoft.Resources/resourceGroups" => "Resource Group",
                "Microsoft.Storage/storageAccounts" => "Storage Account",
                "Microsoft.KeyVault/vaults" => "Key Vault",
                "Microsoft.KeyVault/vaults/secrets" => "Secret",
                "Microsoft.KeyVault/vaults/keys" => "Key",
                "Microsoft.KeyVault/vaults/certificates" => "Certificate",
                "Microsoft.Web/sites" => "App Service",
                "Microsoft.Web/sites/slots" => "Deployment Slot",
                "Microsoft.Compute/virtualMachines" => "Virtual Machine",
                "Microsoft.Sql/servers" => "SQL Server",
                "Microsoft.Sql/servers/databases" => "SQL Database",
                "Microsoft.Cdn/profiles/endpoints" => "Front Door",
                _ => resourceType?.Split('/').Last() ?? "Resource"
            };
        }

        /// <summary>
        /// Logs a new activity entry.
        /// </summary>
        public ActivityEntry LogActivity(string action, string resourceName, string resourceType, string resourceId = null)
        {
            // Ensure we've loaded existing data before modifying the collection
            // This prevents data loss when activities are logged before the UI shows the activity log
            if (!_isLoaded)
            {
                LoadFromDisk();
            }

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
            while (Activities.Count > _maxActivities)
            {
                var removed = Activities[Activities.Count - 1];
                removed.PropertyChanged -= OnEntryPropertyChanged;
                Activities.RemoveAt(Activities.Count - 1);
            }

            ScheduleSave();
            return entry;
        }

        /// <summary>
        /// Logs a completed activity (success or failure).
        /// </summary>
        public ActivityEntry LogActivity(string action, string resourceName, string resourceType, ActivityStatus status, string errorMessage = null, string resourceId = null)
        {
            // Ensure we've loaded existing data before modifying the collection
            if (!_isLoaded)
            {
                LoadFromDisk();
            }

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

            while (Activities.Count > _maxActivities)
            {
                var removed = Activities[Activities.Count - 1];
                removed.PropertyChanged -= OnEntryPropertyChanged;
                Activities.RemoveAt(Activities.Count - 1);
            }

            ScheduleSave();
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
            // Save when status or error message changes
            if (e.PropertyName == nameof(ActivityEntry.Status) || 
                e.PropertyName == nameof(ActivityEntry.ErrorMessage))
            {
                ScheduleSave();
            }
        }

        /// <summary>
        /// Schedules a debounced save to disk. Multiple rapid calls will be coalesced.
        /// </summary>
        private void ScheduleSave()
        {
            lock (_lock)
            {
                _saveCts?.Cancel();
                _saveCts = new CancellationTokenSource();
                var token = _saveCts.Token;

                // Fire-and-forget is intentional for debouncing
                _ = Task.Delay(_saveDebounceMs, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        SaveToDiskCore();
                    }
                }, TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Saves immediately without debouncing. Used when adding new activities.
        /// </summary>
        private void SaveToDisk()
        {
            lock (_lock)
            {
                _saveCts?.Cancel();
            }
            SaveToDiskCore();
        }

        private void SaveToDiskCore()
        {
            lock (_lock)
            {
                // Ensure we've loaded existing data before saving to avoid overwriting
                // This handles the case where activities are logged before the UI shows the activity log
                if (!_isLoaded)
                {
                    LoadFromDiskCore();
                }

                try
                {
                    Directory.CreateDirectory(_storageFolder);

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

                    string json = JsonSerializer.Serialize(data, _jsonOptions);
                    File.WriteAllText(_storageFile, json);
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
        }

        private void LoadFromDisk()
        {
            lock (_lock)
            {
                LoadFromDiskCore();
            }
        }

        /// <summary>
        /// Core loading logic. Must be called within a lock.
        /// </summary>
        private void LoadFromDiskCore()
        {
            if (_isLoaded)
                return;

            try
            {
                if (!File.Exists(_storageFile))
                {
                    _isLoaded = true;
                    return;
                }

                string json = File.ReadAllText(_storageFile);
                var data = JsonSerializer.Deserialize<List<ActivityEntryData>>(json, _jsonOptions);

                if (data == null)
                {
                    _isLoaded = true;
                    return;
                }

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

                    if (Activities.Count >= _maxActivities)
                        break;
                }

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                ex.Log();
                _isLoaded = true; // Mark as loaded even on failure to prevent retry loops
            }
        }

        /// <summary>
        /// Loads the activity log from disk asynchronously. Safe to call multiple times.
        /// This is deferred from the constructor to avoid blocking UI initialization.
        /// </summary>
        public async Task LoadAsync()
        {
            if (_isLoaded)
                return;

            // Read file data on background thread
            List<ActivityEntryData> data = null;
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_isLoaded)
                        return;

                    try
                    {
                        if (File.Exists(_storageFile))
                        {
                            string json = File.ReadAllText(_storageFile);
                            data = JsonSerializer.Deserialize<List<ActivityEntryData>>(json, _jsonOptions);
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }
            }).ConfigureAwait(false);

            // Switch to UI thread to populate the collection
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            lock (_lock)
            {
                if (_isLoaded)
                    return;

                if (data != null)
                {
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

                        if (Activities.Count >= _maxActivities)
                            break;
                    }
                }

                _isLoaded = true;
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
                var actionText = GetActionText();
                var baseText = $"{actionText} \"{ResourceName}\"";
                return Status == ActivityStatus.Failed && HasError
                    ? $"{baseText} - {ErrorMessage}"
                    : baseText;
            }
        }

        /// <summary>
        /// Gets the action text with appropriate tense based on status.
        /// </summary>
        private string GetActionText()
        {
            if (Status == ActivityStatus.InProgress)
                return Action;

            // Convert to past tense for completed actions
            return Action switch
            {
                "Adding Tag" => Status == ActivityStatus.Success ? "Added tag" : "Failed to add tag",
                "Deleting Tag" => Status == ActivityStatus.Success ? "Deleted tag" : "Failed to delete tag",
                "Adding" => Status == ActivityStatus.Success ? "Added" : "Failed to add",
                "Deleting" => Status == ActivityStatus.Success ? "Deleted" : "Failed to delete",
                "Creating" => Status == ActivityStatus.Success ? "Created" : "Failed to create",
                "Updating" => Status == ActivityStatus.Success ? "Updated" : "Failed to update",
                "Restarting" => Status == ActivityStatus.Success ? "Restarted" : "Failed to restart",
                "Stopping" => Status == ActivityStatus.Success ? "Stopped" : "Failed to stop",
                "Starting" => Status == ActivityStatus.Success ? "Started" : "Failed to start",
                "Uploading" => Status == ActivityStatus.Success ? "Uploaded" : "Failed to upload",
                "Downloading" => Status == ActivityStatus.Success ? "Downloaded" : "Failed to download",
                _ => Action
            };
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
