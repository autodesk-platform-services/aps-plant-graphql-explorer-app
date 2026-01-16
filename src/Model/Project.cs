using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GraphQLClient.Data
{
    public class ProjectsData
    {
        [JsonPropertyName("projects")]
        public Result<Project>? Projects { get; set; }
    }

    // Contains pagination and results
    //public class ProjectsResult
    //{
    //    [JsonPropertyName("pagination")]
    //    public Pagination? Pagination { get; set; }

    //    [JsonPropertyName("results")]
    //    public List<Project>? Results { get; set; }
    //}

    public class AlternativeIdentifiersData
    {
        [JsonPropertyName("dataManagementAPIProjectId")]
        public string DataManagementAPIProjectId { get; set; } = string.Empty;
    }

    public class Project
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("alternativeIdentifiers")]
        public AlternativeIdentifiersData AlternativeIdentifiers { get; set; } = new AlternativeIdentifiersData();
    }
}
