using GraphQLClient.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;

namespace GraphQLClient.Commands
{
    internal sealed class GQLRequest : Request
    {
        private static readonly Lazy<GQLRequest> _instance = new(() => new GQLRequest());

        private GQLRequest()
            : base("https://developer-stg.api.autodesk.com/aec/graphql", "https://developer.api.autodesk.com/aec/graphql")
        {
        }

        public static GQLRequest Instance => _instance.Value;

        public override async Task<T> QueryAsync<T>(string query, object? variables = null, CancellationToken cancellationToken = default)
        {
            var token = await TokenService.RequestTokenAsync(cancellationToken).ConfigureAwait(false);

            // Create proper GraphQL request body
            var requestBody = new Dictionary<string, object?>
            {
                ["query"] = query
            };

            if (variables != null)
            {
                requestBody["variables"] = variables;
            }

            var jsonContent = JsonSerializer.Serialize(requestBody);
            using var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json"),
                Headers =
                {
                    { "Authorization", $"Bearer {token.AccessToken}" },
                    { "Accept", "application/json"},
                    { "Region", "US" }
                }
            };

            using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"GraphQL request failed with status code {response.StatusCode} and reason {response.ReasonPhrase}");
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(payload))
            {
                throw new Exception("GraphQL response payload is empty.");
            }

            var result = JsonSerializer.Deserialize<T>(payload) ?? throw new Exception("Failed to deserialize GraphQL response.");
            return result;
        }
    }
}
