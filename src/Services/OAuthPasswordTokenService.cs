using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Windows;
using GraphQLClient.Views;

namespace GraphQLClient.Services
{
    public sealed class OAuthPasswordTokenService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly OAuthClientOptions _options;
        private bool _disposed;
        private OAuthTokenResult? _accessToken;
        private OAuthType _oAuthType;
        private DateTime _tokenExpiresAt = DateTime.MinValue;

        private readonly record struct PkceData(string State, string Verifier, string Challenge, bool Initialized);

        private OAuthPasswordTokenService(OAuthClientOptions options, OAuthType oAuthType, HttpClient httpClient)
        {
            _options = options;
            _oAuthType = oAuthType;
            _httpClient = httpClient;
        }

        public GraphQLEnvironment Environment => _options.Environment;

        public Uri AuthorizeEndpoint => _options.AuthorizeEndpoint;

        public Uri TokenEndpoint => _options.TokenEndpoint;

        public Uri RedirectUri => _options.RedirectUri;

        public string Scope => _options.Scope;

        public static OAuthPasswordTokenService CreateDefault(GraphQLEnvironment environment, OAuthType oAuthType)
        {
            var options = OAuthClientOptions.CreateFromEnvironment(environment, oAuthType);
            return new OAuthPasswordTokenService(options, oAuthType, CreateHttpClient());

        }

        public async Task<OAuthTokenResult> RequestTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_accessToken is not null)
            {
                var currentToken = _accessToken;

                if (_tokenExpiresAt > DateTime.UtcNow + TimeSpan.FromMinutes(1))
                {
                    return currentToken;
                }

                if (!string.IsNullOrWhiteSpace(currentToken.RefreshToken))
                {
                    var refreshed = await RefreshTokenAsync(currentToken.RefreshToken!, cancellationToken).ConfigureAwait(false);
                    if (refreshed.IsSuccess)
                    {
                        CacheToken(refreshed);
                        return _accessToken;
                    }
                }

                _accessToken = null;
            }

            ThrowIfDisposed();

            using var listener = new HttpListener();
            listener.Prefixes.Add($"{RedirectUri.AbsoluteUri.TrimEnd('/')}/");
            listener.Start();

            await CreatePKCEURL(listener, _oAuthType, cancellationToken);
            //var pkce = CreatePkceData();
            //var authorizeUrl = BuildAuthorizeUrl(pkce);

            //Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true });

            //try
            //{
            //    var code = await WaitForAuthorizationCodeAsync(listener, pkce.State, cancellationToken).ConfigureAwait(false);
            //    var result = await RedeemAuthorizationCodeAsync(code, pkce, cancellationToken).ConfigureAwait(false);
            //    CacheToken(result);
            //}
            //finally
            //{
            //    listener.Stop();
            //}
            return _accessToken!;
        }

        public async Task<OAuthTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(BuildRefreshRequestBody(refreshToken)),
            };

            //// If you have a client secret, stick with HTTP Basic; do NOT duplicate client_id in the body.
            //if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
            //{
            //    var credentials = Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}");
            //    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
            //}

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return OAuthTokenResult.Failure(response.StatusCode, payload, "Token refresh failed.");
            }

            try
            {
                using var json = JsonDocument.Parse(payload);
                var root = json.RootElement;

                if (!root.TryGetProperty("access_token", out var tokenElement))
                {
                    return OAuthTokenResult.Failure(response.StatusCode, payload, "Response missing access_token.");
                }

                var tokenType = root.TryGetProperty("token_type", out var typeElement) ? typeElement.GetString() ?? "Bearer" : "Bearer";
                var expiresIn = root.TryGetProperty("expires_in", out var expiresElement) ? expiresElement.GetInt32() : (int?)null;
                var newRefreshToken = root.TryGetProperty("refresh_token", out var refreshElement) ? refreshElement.GetString() : refreshToken;

                return OAuthTokenResult.Success(tokenElement.GetString() ?? string.Empty, tokenType, expiresIn, newRefreshToken, payload);
            }
            catch (JsonException ex)
            {
                return OAuthTokenResult.Failure(response.StatusCode, payload, "Invalid JSON payload.", ex);
            }
        }

        private IEnumerable<KeyValuePair<string, string>> BuildRefreshRequestBody(string refreshToken)
        {
            yield return new KeyValuePair<string, string>("grant_type", "refresh_token");
            yield return new KeyValuePair<string, string>("refresh_token", refreshToken);
            yield return new KeyValuePair<string, string>("redirect_uri", RedirectUri.AbsoluteUri);

            if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                yield return new KeyValuePair<string, string>("client_id", _options.ClientId);
            }
        }

        private async Task CreatePKCEURL(HttpListener listener, OAuthType oAuthType, CancellationToken cancellationToken)
        {
            var pkce = CreatePkceData(oAuthType == OAuthType.OAuth_PKCE);
            var authorizeUrl = BuildAuthorizeUrl(pkce);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var waitForCodeTask = WaitForAuthorizationCodeAsync(listener, pkce, linkedCts.Token);

            bool? dialogResult = await Application.Current.Dispatcher.InvokeAsync<bool?>(() =>
            {
                var dialog = new OAuthWindow(new Uri(authorizeUrl), RedirectUri)
                {
                    Owner = Application.Current.MainWindow
                };

                dialog.Closed += (_, __) =>
                {
                    if (dialog.DialogResult == true)
                    {
                        return;
                    }

                    if (!linkedCts.IsCancellationRequested)
                    {
                        linkedCts.Cancel();
                    }

                    try
                    {
                        listener.Stop();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (HttpListenerException)
                    {
                    }
                };

                return dialog.ShowDialog();
            });

            if (dialogResult != true || !linkedCts.IsCancellationRequested)
            {
                linkedCts.Cancel();
            }

            try
            {
                var code = await WaitForAuthorizationCodeAsync(listener, pkce, linkedCts.Token).ConfigureAwait(false);
                var result = await RedeemAuthorizationCodeAsync(code, pkce, cancellationToken).ConfigureAwait(false);
                CacheToken(result);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("OAuth login was cancelled by the user.", cancellationToken);
            }
            finally
            {
                try
                {
                    listener.Stop();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (HttpListenerException)
                {
                }
            }
        }

        private void CacheToken(OAuthTokenResult result)
        {
            if (!result.IsSuccess)
            {
                _accessToken = null;
                _tokenExpiresAt = DateTime.MinValue;
                return;
            }

            _accessToken = result;

            if (result.ExpiresIn.HasValue && result.ExpiresIn.Value > 0)
            {
                _tokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn.Value);
            }
            else
            {
                _tokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            }
        }

        private string BuildAuthorizeUrl(PkceData pkce)
        {
            var query = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = _options.ClientId,
                ["redirect_uri"] = RedirectUri.AbsoluteUri,
                ["scope"] = Scope,
                ["prompt"] = "login"
            };

            if (pkce.Initialized)
            {
                query["state"] = pkce.State;
                query["code_challenge"] = pkce.Challenge;
                query["code_challenge_method"] = "S256";
            }

            var builder = new StringBuilder(AuthorizeEndpoint.AbsoluteUri);
            builder.Append(AuthorizeEndpoint.AbsoluteUri.Contains("?") ? "&" : "?");
            builder.Append(string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}")));
            return builder.ToString();
        }

        private static PkceData CreatePkceData(bool isInitialized = true)
        {
            var stateBytes = new byte[16];
            RandomNumberGenerator.Fill(stateBytes);
            var state = Base64UrlEncode(stateBytes);

            var verifierBytes = new byte[32];
            RandomNumberGenerator.Fill(verifierBytes);
            var verifier = Base64UrlEncode(verifierBytes);

            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            var challenge = Base64UrlEncode(challengeBytes);

            return new PkceData(state, verifier, challenge, isInitialized);
        }

        private static async Task<string> WaitForAuthorizationCodeAsync(HttpListener listener, PkceData pkce, CancellationToken cancellationToken)
        {
            while (true)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                catch (InvalidOperationException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteResponseAsync(context.Response, "Login cancelled.").ConfigureAwait(false);
                    context.Response.Close();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var query = context.Request.QueryString;
                var returnedState = query["state"];
                var code = query["code"];
                var error = query["error"];

                if (!string.IsNullOrWhiteSpace(error))
                {
                    await WriteResponseAsync(context.Response, "Login failed.").ConfigureAwait(false);
                    context.Response.Close();
                    throw new InvalidOperationException($"OAuth authorization failed: {error}");
                }

                if (!pkce.Initialized || (pkce.Initialized && string.Equals(returnedState, pkce.State, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(code)))
                {
                    await WriteResponseAsync(context.Response, "You may close this window and return to the application.").ConfigureAwait(false);
                    context.Response.Close();
                    return code;
                }

                await WriteResponseAsync(context.Response, "Unexpected response.").ConfigureAwait(false);
                context.Response.Close();
            }
        }

        private async Task<OAuthTokenResult> RedeemAuthorizationCodeAsync(string code, PkceData pkce, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(BuildTokenRequestBody(code, pkce)),
            };

            if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                var credentials = Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return OAuthTokenResult.Failure(response.StatusCode, payload, "Token endpoint returned an error.");
            }

            try
            {
                using var json = JsonDocument.Parse(payload);
                var root = json.RootElement;
                if (!root.TryGetProperty("access_token", out var tokenElement))
                {
                    return OAuthTokenResult.Failure(response.StatusCode, payload, "Missing access_token in response.");
                }

                var tokenType = root.TryGetProperty("token_type", out var typeElement) ? typeElement.GetString() ?? "Bearer" : "Bearer";
                var expiresIn = root.TryGetProperty("expires_in", out var expiresElement) ? expiresElement.GetInt32() : (int?)null;
                var refreshToken = root.TryGetProperty("refresh_token", out var refreshElement) ? refreshElement.GetString() : null;

                return OAuthTokenResult.Success(tokenElement.GetString() ?? string.Empty, tokenType, expiresIn, refreshToken, payload);
            }
            catch (JsonException ex)
            {
                return OAuthTokenResult.Failure(response.StatusCode, payload, "Invalid JSON payload from token endpoint.", ex);
            }
        }

        private IEnumerable<KeyValuePair<string, string>> BuildTokenRequestBody(string code, PkceData pkce)
        {
            yield return new KeyValuePair<string, string>("grant_type", "authorization_code");
            yield return new KeyValuePair<string, string>("code", code);
            yield return new KeyValuePair<string, string>("redirect_uri", RedirectUri.AbsoluteUri);
            if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                yield return new KeyValuePair<string, string>("client_id", _options.ClientId);
            }
            if (pkce.Initialized)
            {
                yield return new KeyValuePair<string, string>("code_verifier", pkce.Verifier);
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static async Task WriteResponseAsync(HttpListenerResponse response, string message)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/html";
            var bytes = Encoding.UTF8.GetBytes($"<html><body>{WebUtility.HtmlEncode(message)}</body></html>");
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _httpClient.Dispose();
        }

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

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OAuthPasswordTokenService));
            }
        }
    }
}

