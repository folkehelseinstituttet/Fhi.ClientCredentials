using System.Net.Http.Headers;

namespace Fhi.ClientCredentialsKeypairs
{
    public class HttpAuthHandler : DelegatingHandler
    {
        public const string AnonymousOptionKey = "Anonymous";
        public const string BearerSchemeType = "Bearer";
        public const string DpopSchemeType = "DPOP";
        public const string DpopHeaderName = "DPoP";

        private readonly IAuthTokenStore _authTokenStore;

        public HttpAuthHandler(IAuthTokenStore authTokenStore)
        {
            _authTokenStore = authTokenStore;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Options.All(x => x.Key != AnonymousOptionKey))
            {
                var token = await _authTokenStore.GetToken(request.Method, request.RequestUri?.AbsoluteUri ?? "");
                if (token != null)
                {
                    if (token.TokenType.ToUpper() == DpopSchemeType.ToUpper())
                    {
                        return await SendWithDpopAsync(request, cancellationToken, token);
                    }
                    else 
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                    }
                }
            }

            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }

        private async Task<HttpResponseMessage> SendWithDpopAsync(HttpRequestMessage request,
            CancellationToken cancellationToken, JwtAccessToken token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

            request.Headers.TryAddWithoutValidation(DpopHeaderName, token.DpopProof);

            var dpopResponse = await base.SendAsync(request, cancellationToken);

            if (dpopResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var supportedSchemes = dpopResponse.Headers.WwwAuthenticate.Select(x => x.Scheme).ToArray();

                if (!supportedSchemes.Contains(DpopSchemeType, StringComparer.InvariantCultureIgnoreCase))
                {
                    // downgrade request to Dpop if Dpop is not supported
                    request.Headers.Authorization = new AuthenticationHeaderValue(BearerSchemeType, token.AccessToken);
                    request.Headers.Remove(DpopHeaderName);

                    return await base.SendAsync(request, cancellationToken);
                }
            }

            return dpopResponse;
        }
    }
}
