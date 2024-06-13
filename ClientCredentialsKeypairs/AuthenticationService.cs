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

    public async Task SetupToken()
    {
        Jti = Guid.NewGuid().ToString();

        var c = new HttpClient();
        var cctr = new ClientCredentialsTokenRequest
        {
            Address = Config.Authority,
            ClientId = Config.ClientId,
            DPoPProofToken = BuildDpopAssertion(Jti),
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
                cctr.DPoPProofToken = BuildDpopAssertion(Jti, nonce: response.DPoPNonce ?? Guid.NewGuid().ToString());
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

        var ath = CreateDpopAth(AccessToken);

        return new JwtAccessToken()
        {
            AccessToken = AccessToken,
            TokenType = "DPoP",
            DpopProof = BuildDpopAssertion(Jti, ath: ath),
        };
    }

    /// <summary>
    /// Hash of the access token. The value MUST be the result of a base64url encoding (as defined in Section 2 of [RFC7515]) the SHA-256 [SHS] hash of the ASCII encoding of the associated access token's value.
    /// </summary>
    private static string? CreateDpopAth(string accessToken)
    {
        using var encryptor = SHA256.Create();

        // this may or may not be correct
        var input = Encoding.ASCII.GetBytes(accessToken);

        var sha256 = encryptor.ComputeHash(input);

        return Convert.ToBase64String(sha256);
    }

    private string BuildDpopAssertion(string jti, string? nonce = null, string? ath = null)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

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

        if (nonce != null)
        {
            claims.Add(new("nonce", nonce));
        }

        var signingCredentials = GetClientAssertionSigningCredentials();

        var jwtSecurityToken = new JwtSecurityToken(null, null, claims, null, null, signingCredentials);
        jwtSecurityToken.Header.Remove("typ");
        jwtSecurityToken.Header.Add("typ", "dpop+jwt");
        jwtSecurityToken.Header.Add("jwk", GetPublicJwk());

        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        return token;
    }

    private JsonWebKey GetPublicJwk()
    {
        var key = new JsonWebKey(Config.PrivateKey);
        return new JsonWebKey()
        {
            Alg = key.Alg,
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
