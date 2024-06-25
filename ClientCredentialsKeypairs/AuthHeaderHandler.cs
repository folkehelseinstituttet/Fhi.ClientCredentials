using System.Net.Http.Headers;

namespace Fhi.ClientCredentialsKeypairs;

public class AuthHeaderHandler : DelegatingHandler
{
    public const string BearerSchemeType = "Bearer";
    public const string DpopSchemeType = "DPOP";
    public const string DpopHeaderName = "DPoP";

    private readonly IAuthTokenStore authTokenStore;

    public AuthHeaderHandler(IAuthTokenStore? authTokenStore)
    {
        this.authTokenStore = authTokenStore ?? throw new ArgumentNullException(nameof(authTokenStore));
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await authTokenStore.GetToken(request.Method, request.RequestUri?.AbsoluteUri ?? "");
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

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
