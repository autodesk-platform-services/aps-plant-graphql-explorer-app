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
    internal class GQLRequest
    {
        private string _baseUrl = "https://developer-stg.api.autodesk.com/aec/graphql";
        private static readonly Lazy<GQLRequest> _instance = new Lazy<GQLRequest>(() => new GQLRequest());
        private readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return client;
        }

        private GQLRequest()
        {
        }

        public static OAuthPasswordTokenService TokenService { get; set; }

        public static GQLRequest Instance => _instance.Value;

        public async Task<T?> QueryAsync<T>(string query, object? variables = null, CancellationToken cancellationToken = default)
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
            
            using var message = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json"),
                Headers =
                {
                    { "Authorization", $"Bearer {token.AccessToken}" },
                    { "Accept", "application/json"},
                    { "Region", "US" }
                }
            };

            using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            //using var json = JsonDocument.Parse(payload);
            //var root = json.RootElement;
            //root.TryGetProperty("", out var dataElement);
            return JsonSerializer.Deserialize<T>(payload);
        }
    }
}
