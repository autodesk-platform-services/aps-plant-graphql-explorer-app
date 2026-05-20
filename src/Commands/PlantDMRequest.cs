using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GraphQLClient.Commands
{
    public class PlantDMRequest : Request
    {
        private static readonly Lazy<PlantDMRequest> _instance = new(() => new PlantDMRequest());

        public PlantDMRequest() : 
            base("https://developer-stg.api.autodesk.com/plant/v3/datamodel/project/{0}/folderUrn/{1}/searchFilePlus",
                 "https://developer.api.autodesk.com/plant/v3/datamodel/project/{0}/folderUrn/{1}/searchFilePlus")
        {
        }

        public static PlantDMRequest Instance => _instance.Value;

        public async Task<string?> GetFilePlusDataAsync(string folderUrn, string projectId, CancellationToken cancellationToken = default)
        {
            var token = await TokenService.RequestTokenAsync(cancellationToken).ConfigureAwait(false);

            var prjId = projectId.Substring(projectId.IndexOf(".") + 1);
            var baseUrl = string.Format(BaseUrl, prjId, folderUrn);
            using var message = new HttpRequestMessage(HttpMethod.Get, baseUrl)
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token.AccessToken}" },
                    { "Accept", "application/json"}
                }
            };

            using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var val = await response.Content.ReadAsStringAsync();
                return val.Trim('"');
            }
            return null;
        }
    }
}
