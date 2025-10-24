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

    public class Folder
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("parentFolder")]
        public FolderReference? ParentFolder { get; set; }
    }

    public class FolderReference
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
