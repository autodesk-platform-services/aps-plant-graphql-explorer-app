using System;
using static System.Net.WebRequestMethods;

namespace GraphQLClient.Services
{
    internal sealed class OAuthClientOptions
    {
        private OAuthClientOptions(
            GraphQLEnvironment environment,
            Uri authorizeEndpoint,
            Uri tokenEndpoint,
            string clientId,
            string? clientSecret,
            Uri redirectUri,
            string scope)
        {
            Environment = environment;
            AuthorizeEndpoint = authorizeEndpoint;
            TokenEndpoint = tokenEndpoint;
            ClientId = clientId;
            ClientSecret = clientSecret;
            RedirectUri = redirectUri;
            Scope = scope;
        }

        public GraphQLEnvironment Environment { get; }

        public Uri AuthorizeEndpoint { get; }

        public Uri TokenEndpoint { get; }

        public string ClientId { get; }

        public string? ClientSecret { get; }

        public Uri RedirectUri { get; }

        public string Scope { get; }

        public static OAuthClientOptions CreateFromEnvironment(GraphQLEnvironment environment)
        {
            var defaults = environment switch
            {
                GraphQLEnvironment.Staging => new
                {
                    Authorize = "https://developer-stg.api.autodesk.com/authentication/v2/authorize",
                    Token = "https://developer-stg.api.autodesk.com/authentication/v2/token",
                    ClientId = "A74ztMCm6dFTk3tgtAR8IVbLLU7QsIqGHY6gnKb6WkWRvNdl" ?? string.Empty,
                    ClientSecret = "" ?? string.Empty
                },
                GraphQLEnvironment.Production => new
                {
                    Authorize = "https://developer.api.autodesk.com/authentication/v2/authorize",
                    Token = "https://developer.api.autodesk.com/authentication/v2/token",
                    ClientId = "" ?? string.Empty,
                    ClientSecret = "" ?? string.Empty
                },
                _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown environment."),
            };

            var authorizeUrl = System.Environment.GetEnvironmentVariable("OAUTH_AUTHORIZE_URL") ?? defaults.Authorize;
            var tokenUrl = System.Environment.GetEnvironmentVariable("OAUTH_TOKEN_URL") ?? defaults.Token;
            var clientId = System.Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID") ?? defaults.ClientId;
            var clientSecret = System.Environment.GetEnvironmentVariable("OAUTH_CLIENT_SECRET") ?? defaults.ClientSecret;
            var redirectUrl = System.Environment.GetEnvironmentVariable("OAUTH_CALLBACK_URL") ?? "http://localhost:8080/oauth/callback";
            var scope = System.Environment.GetEnvironmentVariable("OAUTH_SCOPE") ?? "data:read data:write data:create";

            if (!Uri.TryCreate(authorizeUrl, UriKind.Absolute, out var authorizeEndpoint))
            {
                throw new InvalidOperationException($"Invalid OAuth authorize URL: {authorizeUrl}");
            }

            if (!Uri.TryCreate(tokenUrl, UriKind.Absolute, out var tokenEndpoint))
            {
                throw new InvalidOperationException($"Invalid OAuth token URL: {tokenUrl}");
            }

            if (!Uri.TryCreate(redirectUrl, UriKind.Absolute, out var redirectUri))
            {
                throw new InvalidOperationException($"Invalid OAuth redirect URL: {redirectUrl}");
            }

            return new OAuthClientOptions(
                environment,
                authorizeEndpoint,
                tokenEndpoint,
                clientId,
                string.IsNullOrWhiteSpace(clientSecret) ? null : clientSecret,
                redirectUri,
                scope);
        }
    }
}

