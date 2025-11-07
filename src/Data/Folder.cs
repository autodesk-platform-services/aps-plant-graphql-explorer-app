using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        public bool IsPlantProject { get; set; }
        [JsonIgnore]
        public string PipingDataset { get; set; }
        [JsonIgnore]
        public string PIDDataset { get; set; }

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
