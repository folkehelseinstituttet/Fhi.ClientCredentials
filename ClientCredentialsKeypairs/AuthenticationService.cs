using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.ClientCredentialsKeypairs;

public interface IAuthenticationService
{
    string AccessToken { get; }
    Task SetupToken();
}

public class AuthenticationService : IAuthenticationService
{
    public ClientCredentialsConfiguration Config { get; }


    public AuthenticationService(ClientCredentialsConfiguration config)
    {
        Config = config;
    }

    public string AccessToken { get; private set; } = "";

    public async Task SetupToken()
    {
        var c = new HttpClient();
        var cctr = new ClientCredentialsTokenRequest
        {
            Address = Config.Authority,
            ClientId = Config.ClientId,
            DPoPProofToken = BuildDpopAssertion(Config.Authority, Config.ClientId),
            GrantType = OidcConstants.GrantTypes.ClientCredentials,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Scope = Config.Scopes,
            ClientAssertion = new ClientAssertion
            {
                Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                Value = BuildClientAssertion(Config.Authority, Config.ClientId)
            }
        };
        var response = await c.RequestClientCredentialsTokenAsync(cctr);
        if (response.IsError)
        {
            if (response.Error == "use_dpop_nonce")
            {
                var nonce = response.DPoPNonce;
            }

            throw new Exception($"Unable to get access token: {response.Error}");
        }

        AccessToken = response.AccessToken;
    }

    private string BuildDpopAssertion(string audience, string clientId)
    {
        var claims = new List<Claim>
        {
            new("jti", Guid.NewGuid().ToString()),
            new("htm", "POST"),
            new("htu", "https://helseid-sts.test.nhn.no/connect/token"),
            new("iat", DateTime.UtcNow.Ticks.ToString()),
        };

        var credentials = new JwtSecurityToken(clientId, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(60), GetClientAssertionSigningCredentials());
        credentials.Header.Remove("typ");
        credentials.Header.Add("typ", "dpop+jwt");
        credentials.Header.Add("jwt", new JsonWebKey(Config.PrivateKey));

        var tokenHandler = new JwtSecurityTokenHandler();
        var ret = tokenHandler.WriteToken(credentials);
        return ret;
    }

    private string BuildClientAssertion(string audience, string clientId)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, clientId),
            new(JwtClaimTypes.IssuedAt, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
            new(JwtClaimTypes.JwtId, Guid.NewGuid().ToString("N")),
        };

        var credentials = new JwtSecurityToken(clientId, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(60), GetClientAssertionSigningCredentials());

        var tokenHandler = new JwtSecurityTokenHandler();
        var ret = tokenHandler.WriteToken(credentials);
        return ret;
    }

    private SigningCredentials GetClientAssertionSigningCredentials()
    {
        var securityKey = new JsonWebKey(Config.PrivateKey);
        return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
    }
}

public interface IAuthTokenStore
{
    Task<string> GetToken();
}

public class AuthenticationStoreDefault : IAuthTokenStore
{
    private readonly string token;

    public AuthenticationStoreDefault(string token)
    {
        this.token = token;
    }
    public Task<string> GetToken() => Task.FromResult(token);
}

public static class GlobalAuthenticationStore
{
    public static IAuthTokenStore? AuthTokenStore { get; private set; }
    public static IAuthenticationService? AuthenticatedService { get; private set; }

    public static void CreateGlobalTokenStore(IAuthTokenStore store, IAuthenticationService authenticatedService)
    {
        AuthTokenStore = store;
        AuthenticatedService = authenticatedService;
    }
}

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
        var token = await authTokenStore.GetToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
