using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GraphQLClient.Commands
{
    public class DocsRequest : Request
    {
        private static readonly Lazy<DocsRequest> _instance = new(() => new DocsRequest());

        private DocsRequest()
            : base("https://developer.api.autodesk.com/data/v1/projects/{0}/folders/{1}")
        {
        }

        public static DocsRequest Instance => _instance.Value;

        public async Task<bool> FindItemByNameAsync(string folderUrn, string projectId, CancellationToken cancellationToken = default)
        {
            var token = await TokenService.RequestTokenAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = string.Format(BaseUrl, projectId, folderUrn);
            using var message = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/contents?filter[displayName]=Project.xml")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token.AccessToken}" },
                    { "Accept", "application/json"},
                    { "Region", GQLRequest.Region }
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

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                throw new Exception("GraphQL response does not contain a valid 'data' array.");
            }

            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("attributes", out var attrs)) continue;
                if (!attrs.TryGetProperty("displayName", out var nameProp)) continue;

                var displayName = nameProp.GetString();
                if (string.Equals(displayName, "Project.xml", StringComparison.OrdinalIgnoreCase))
                {
                    var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    return true;
                }
            }

            return false;
        }

        public async Task<(string?, string?)> FindFilePlusFoldersAsync(string folderUrn, string projectId, CancellationToken cancellationToken = default)
        {
            var token = await TokenService.RequestTokenAsync(cancellationToken).ConfigureAwait(false);

            var baseUrl = string.Format(BaseUrl, projectId, folderUrn);
            using var message = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/contents?filter[type]=folders")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token.AccessToken}" },
                    { "Accept", "application/json"}
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

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                throw new Exception("GraphQL response does not contain a valid 'data' array.");
            }

            var res = (pid: (string?)null, p3d: (string?)null);
            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("attributes", out var attrs)) continue;
                if (!attrs.TryGetProperty("displayName", out var nameProp)) continue;

                var displayName = nameProp.GetString();
                if (string.Equals(displayName, "PID DWG", StringComparison.OrdinalIgnoreCase))
                {
                    var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    res.pid = id;
                }
                else if (string.Equals(displayName, "Plant 3D Models", StringComparison.OrdinalIgnoreCase))
                {
                    var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    res.p3d = id;
                }
            }
            return res;
        }
    }
}
