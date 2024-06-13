using System.Net.Http.Headers;

namespace Fhi.ClientCredentialsKeypairs;

public class AuthHeaderHandler : DelegatingHandler
{
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
            request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
            if (token.DpopProof != null)
            {
                request.Headers.TryAddWithoutValidation("DPoP", token.DpopProof);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
