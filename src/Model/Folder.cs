using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace GraphQLClient.Data
{
    // Contains pagination and results
    public class ProjectFolderData
    {
        [JsonPropertyName("foldersByProject")]
        public Result<Folder>? Folders { get; set; }
    }

    public class ProjectFolderByFolderData
    {
        [JsonPropertyName("foldersByFolder")]
        public Result<Folder>? Folders { get; set; }
    }

    public class Folder : INotifyPropertyChanged
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        private List<Folder>? _children;
        public List<Folder>? Children
        {
            get => _children;
            set
            {
                if (!ReferenceEquals(_children, value))
                {
                    _children = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPlantProject;
        [JsonIgnore]
        public bool IsPlantProject
        {
            get => _isPlantProject;
            set
            {
                if (_isPlantProject != value)
                {
                    _isPlantProject = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _p3dElementGroupId = string.Empty;
        [JsonIgnore]
        public string? P3DElementGroupId
        {
            get => _p3dElementGroupId;
            set
            {
                if (!string.Equals(_p3dElementGroupId, value, StringComparison.Ordinal))
                {
                    _p3dElementGroupId = value ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasPipingDataset));
                    OnPropertyChanged(nameof(ProjectIconBrush));
                }
            }
        }

        private string _pidElementGroupId = string.Empty;
        [JsonIgnore]
        public string? PIDElementGroupId
        {
            get => _pidElementGroupId;
            set
            {
                if (!string.Equals(_pidElementGroupId, value, StringComparison.Ordinal))
                {
                    _pidElementGroupId = value ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasPIDDataset));
                    OnPropertyChanged(nameof(ProjectIconBrush));
                }
            }
        }

        [JsonIgnore]
        public bool HasPipingDataset => !string.IsNullOrWhiteSpace(_p3dElementGroupId);

        [JsonIgnore]
        public bool HasPIDDataset => !string.IsNullOrWhiteSpace(_pidElementGroupId);

        private static readonly SolidColorBrush MissingDatasetBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));

        static Folder()
        {
            if (MissingDatasetBrush.CanFreeze)
            {
                MissingDatasetBrush.Freeze();
            }
        }

        [JsonIgnore]
        public Brush ProjectIconBrush => (HasPipingDataset && HasPIDDataset) ? Brushes.Black : MissingDatasetBrush;

        public string? Notification => (!HasPipingDataset || !HasPIDDataset) ? "Dataset with one or both File+ missing" : null;

        [JsonPropertyName("parentFolder")]
        public FolderReference? ParentFolder { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FolderReference
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
