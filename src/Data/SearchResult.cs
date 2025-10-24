using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GraphQLClient.Data
{
    public class ElementGroupData
    {
        [JsonPropertyName("elementGroupsByFolder")]
        public Result<ElementGroup>? ElementGroups { get; set; }
    }

    public class ElementGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SearchData
    {
        [JsonPropertyName("elementsByElementGroup")]
        public Result<RowWrap> ElementWraps { get; set; }
    }

    public class Definition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class RowWrap
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("properties")]
        public Row RowData { get; set; }
        [JsonPropertyName("lastModifiedOn")]
        public string LastModifiedOn { get; set; }
        [JsonPropertyName("lastModifiedBy")]
        public UserInfo LastModifiedBy { get; set; }
    }

    public class UserInfo
    {
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class Row
    {
        [JsonPropertyName("results")]
        public List<Column> Columns { get; set; }
    }

    public class Column
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("displayValue")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("definition")]
        public Definition Definition { get; set; }
    }
}
