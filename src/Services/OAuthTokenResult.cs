using System.Net;

namespace GraphQLClient.Services
{
    public sealed record OAuthTokenResult(
        bool IsSuccess,
        string AccessToken,
        string TokenType,
        int? ExpiresIn,
        string? RefreshToken,
        HttpStatusCode StatusCode,
        string RawResponse,
        string? ErrorMessage,
        Exception? Exception
    )
    {
        public static OAuthTokenResult Success(string accessToken, string tokenType, int? expiresIn, string? refreshToken, string rawResponse)
            => new(true, accessToken, tokenType, expiresIn, refreshToken, HttpStatusCode.OK, rawResponse, null, null);

        public static OAuthTokenResult Failure(HttpStatusCode statusCode, string rawResponse, string errorMessage, Exception? exception = null)
            => new(false, string.Empty, string.Empty, null, null, statusCode, rawResponse, errorMessage, exception);
    }
}

