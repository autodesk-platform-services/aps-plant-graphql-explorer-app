using GraphQLClient.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GraphQLClient.Commands
{
    public sealed class DMRequest : Request
    {
        private static readonly Lazy<DMRequest> _instance = new(() => new DMRequest());

        private DMRequest()
            : base("https://developer.api.autodesk.com/dm/v3/projects/{0}/entities:search")
        {
        }

        public static DMRequest Instance => _instance.Value;

        public override async Task<T> QueryAsync<T>(string query, dynamic? variables = null, CancellationToken cancellationToken = default)
        {
            var token = await TokenService.RequestTokenAsync(cancellationToken).ConfigureAwait(false);

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables), "Project ID must be provided in variables.");
            }

            var projectId = variables as string;
            var baseUrl = string.Format(BaseUrl, projectId.Substring(projectId.IndexOf('.') + 1));
            using var message = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(query, Encoding.UTF8, "application/json"),
                Headers =
                {
                    { "Authorization", $"Bearer {token.AccessToken}" },
                    { "Accept", "application/json"},
                    { "Regioni", GQLRequest.Region }
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
