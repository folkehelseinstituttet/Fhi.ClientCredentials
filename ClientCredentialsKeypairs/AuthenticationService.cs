using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.ClientCredentialsKeypairs;

public interface IAuthenticationService
{
    string AccessToken { get; }

    JwtAccessToken CreateAccessToken(HttpMethod method, string url);

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

    /// <summary>
    /// The jti claim must be a unique value that identifies this particular JWT.
    /// </summary>
    private string Jti { get; set; } = "";

    private string DpopProof { get; set; } = "";

    public async Task SetupToken()
    {
        Jti = Guid.NewGuid().ToString();
        DpopProof = BuildDpopAssertion(Jti, Config.Authority, Config.ClientId);

        var c = new HttpClient();
        var cctr = new ClientCredentialsTokenRequest
        {
            Address = Config.Authority,
            ClientId = Config.ClientId,
            DPoPProofToken = DpopProof,
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
                cctr.Headers.Add("DPoP-Nonce", response.DPoPNonce);
                response = await c.RequestClientCredentialsTokenAsync(cctr);
            }

            if (response.IsError)
            {
                throw new Exception($"Unable to get access token: {response.Error}");
            }
        }

        AccessToken = response!.AccessToken ?? "";
    }

    public JwtAccessToken CreateAccessToken(HttpMethod method, string url)
    {
        if (string.IsNullOrEmpty(AccessToken))
        {
            throw new Exception("No access token is set. Unable to create Dpop Proof.");
        }

        var ath = CreateDpopAth(AccessToken, DpopProof);

        return new JwtAccessToken()
        {
            AccessToken = AccessToken,
            TokenType = "DPoP",
            DpopProof = BuildDpopAssertion(Jti, Config.Authority, Config.ClientId, ath),
        };
    }

    /// <summary>
    /// The ath claim should only be used in API calls. Its value should be a SHA-256 hash of the access token that is used in the Authorization header along with the DPoP proof.
    /// </summary>
    private static string? CreateDpopAth(string accessToken, string dpopProof)
    {
        using var encryptor = SHA256.Create();
        
        // this is probably not correct
        var input = Encoding.UTF8.GetBytes(accessToken + dpopProof);

        var sha256 = encryptor.ComputeHash(Encoding.UTF8.GetBytes(accessToken + dpopProof));

        // they do not say how this is encoded, https://utviklerportal.nhn.no/informasjonstjenester/helseid/protokoller-og-sikkerhetsprofil/sikkerhetsprofil/docs/vedlegg/formatering_av_dpop_bevis_enmd/
        return Convert.ToBase64String(sha256);
    }

    private string BuildDpopAssertion(string jti, string audience, string clientId, string? ath = null)
    {
        var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var issueTime = DateTime.UtcNow;
        var iat = (int)issueTime.Subtract(utc0).TotalSeconds;

        var claims = new List<Claim>
        {
            new("jti", jti),
            new("htm", "POST"),
            new("htu", "https://helseid-sts.test.nhn.no/connect/token"),
            new("iat", iat.ToString(), ClaimValueTypes.Integer64),
        };

        if (ath != null)
        {
            claims.Add(new("ath", ath));
        }

        var credentials = new JwtSecurityToken(clientId, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(60), GetClientAssertionSigningCredentials());
        credentials.Header.Remove("typ");
        credentials.Header.Add("typ", "dpop+jwt");
        credentials.Header.Add("jwt", BuildPopJwtToken());

        var tokenHandler = new JwtSecurityTokenHandler();
        var ret = tokenHandler.WriteToken(credentials);
        return ret;
    }

    private JsonWebKey BuildPopJwtToken()
    {
        var key = new JsonWebKey(Config.PrivateKey);
        return new JsonWebKey()
        {
            Alg = key.Alg, // is blank, should be... "RS512"?,
            N = key.N,
            E = key.E,
            Kty = key.Kty,
        };
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

public class JwtAccessToken()
{
    public string AccessToken { get; set; } = "";

    public string TokenType { get; set; } = "";

    public string? DpopProof { get; set; }
}
