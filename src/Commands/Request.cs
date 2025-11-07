using GraphQLClient.Services;
using System.Net;
using System.Net.Http;

namespace GraphQLClient.Commands
{
    public abstract class Request
    {
        protected readonly HttpClient httpClient = CreateHttpClient();

        protected string StagingUrl { get; }
        protected string ProductionUrl { get; }

        public static OAuthPasswordTokenService TokenService { get; set; }

        public static GraphQLEnvironment Environment { get; set; } = GraphQLEnvironment.Staging;

        public Request(string stagingUrl, string productionUrl)
        {
            StagingUrl = stagingUrl;
            ProductionUrl = productionUrl;
        }

        public virtual Task<T> QueryAsync<T>(string query, object? variables = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("This method should be implemented in a derived class.");
        }

        public string BaseUrl => Environment == GraphQLEnvironment.Staging ? StagingUrl : ProductionUrl;

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
    }
}
