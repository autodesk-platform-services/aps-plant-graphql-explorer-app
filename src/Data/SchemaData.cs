using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GraphQLClient.Data
{
    internal class SchemaObject
    {
        [JsonPropertyName("prefix")]
        public string Prefix { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("schemas")]
        public List<SchemaData> Data { get; set; }
    }

    internal class SchemaData
    {
        [JsonPropertyName("groupName")]
        public string GroupName { get; set; }
        [JsonPropertyName("data")]
        public List<string> SchemaList { get; set; }
    }
}
