using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GraphQLClient.Data
{
    // Root response wrapper for GraphQL
    public class GraphQLResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    // The "hubs" object in the response
    public class HubsData
    {
        [JsonPropertyName("hubs")]
        public Result<Hub>? Hubs { get; set; }
    }

    // Contains pagination and results
    //public class HubsResult
    //{
    //    [JsonPropertyName("pagination")]
    //    public Pagination? Pagination { get; set; }

    //    [JsonPropertyName("results")]
    //    public List<Hub>? Results { get; set; }
    //}

    public class Pagination
    {
        [JsonPropertyName("pageSize")]
        public int? PageSize { get; set; }

        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }
    }

    public class Result<T>
    {
        [JsonPropertyName("pagination")]
        public Pagination? Pagination { get; set; }

        [JsonPropertyName("results")]
        public List<T>? Results { get; set; }
    }

    // Individual hub item
    public class Hub
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
