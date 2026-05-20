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

        public static OAuthClientOptions CreateFromEnvironment(GraphQLEnvironment environment, OAuthType oAuthType)
        {
            var defaults = environment switch
            {
                GraphQLEnvironment.Staging => new
                {
                    Authorize = "https://developer-stg.api.autodesk.com/authentication/v2/authorize",
                    Token = "https://developer-stg.api.autodesk.com/authentication/v2/token",
                    ClientId = oAuthType == OAuthType.OAuth ? ConfigurationStg.Default.ClientID : ConfigurationStg.Default.ClientID_PKCE,
                    ClientSecret = oAuthType == OAuthType.OAuth ? ConfigurationStg.Default.ClientSecret: null,
                    Callback = oAuthType == OAuthType.OAuth ? ConfigurationStg.Default.CallbackURL : ConfigurationStg.Default.CallbackURL_PKCE
                },
                GraphQLEnvironment.Production => new
                {
                    Authorize = "https://developer.api.autodesk.com/authentication/v2/authorize",
                    Token = "https://developer.api.autodesk.com/authentication/v2/token",
                    ClientId = oAuthType == OAuthType.OAuth ? Configuration.Default.ClientID : Configuration.Default.ClientID_PKCE,
                    ClientSecret = oAuthType == OAuthType.OAuth ? ConfigurationStg.Default.ClientSecret : null,
                    Callback = oAuthType == OAuthType.OAuth ? Configuration.Default.CallbackURL : Configuration.Default.CallbackURL_PKCE
                },
                _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown environment."),
            };

            var authorizeUrl = System.Environment.GetEnvironmentVariable("OAUTH_AUTHORIZE_URL") ?? defaults.Authorize;
            var tokenUrl = System.Environment.GetEnvironmentVariable("OAUTH_TOKEN_URL") ?? defaults.Token;
            var clientId = System.Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID") ?? defaults.ClientId;
            var clientSecret = System.Environment.GetEnvironmentVariable("OAUTH_CLIENT_SECRET") ?? defaults.ClientSecret;
            var redirectUrl = System.Environment.GetEnvironmentVariable("OAUTH_CALLBACK_URL") ?? defaults.Callback;
            var scope = System.Environment.GetEnvironmentVariable("OAUTH_SCOPE") ?? "data:read data:write data:create";

            if (!Uri.TryCreate(authorizeUrl, UriKind.Absolute, out var authorizeEndpoint))
            {
                throw new InvalidOperationException($"Invalid OAuth authorize URL: {ErrorMessage(authorizeUrl)}");
            }

            if (!Uri.TryCreate(tokenUrl, UriKind.Absolute, out var tokenEndpoint))
            {
                throw new InvalidOperationException($"Invalid OAuth token URL: {ErrorMessage(tokenUrl)}");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("OAuth client ID is not set.");
            }

            if (string.IsNullOrWhiteSpace(clientSecret) && oAuthType == OAuthType.OAuth)
            {
                throw new InvalidOperationException("OAuth client secret is not set.");
            }

            if (!Uri.TryCreate(redirectUrl, UriKind.Absolute, out var redirectUri))
            {
                throw new InvalidOperationException($"Invalid OAuth redirect URL: {ErrorMessage(redirectUrl)}");
            }

            return new OAuthClientOptions(
                environment,
                authorizeEndpoint,
                tokenEndpoint,
                clientId,
                clientSecret,
                redirectUri,
                scope);
        }

        private static string ErrorMessage(string url) => string.IsNullOrEmpty(url) ? "Empty" : url;
    }
}

