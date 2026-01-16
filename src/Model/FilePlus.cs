using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GraphQLClient.Data
{
    public class FilePlus
    {
        [JsonPropertyName("documents")]
        public List<FilePlusDocument> Documents { get; set; }
    }

    public class FilePlusDocument
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("parentFolderUrn")]
        public string ParentFolderUrn { get; set; }
    }
}
