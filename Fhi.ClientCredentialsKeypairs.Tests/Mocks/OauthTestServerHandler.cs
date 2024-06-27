using IdentityModel;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace Fhi.ClientCredentialsKeypairs.Tests.Mocks;

public class OauthTestServerHandler : HttpMessageHandler
{
    private Dictionary<string, string> _jtiToNonce = new();

    public bool EnableDpop { get; set; } = false;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request.Headers
            .SelectMany(x => x.Value.Select(y => new { x.Key, Value = y }))
            .ToDictionary(x => x.Key, x => x.Value);
        headers.TryGetValue("DPoP", out var proof);

        var content = await request.Content!.ReadAsStringAsync();
        var parts = content
            .Split('&')
            .Select(x => new { Key = x.Split('=')[0], Value = x.Split('=')[1] })
            .ToDictionary(x => x.Key, x => x.Value);

        if (!EnableDpop)
        {
            return CreateResult(OidcConstants.TokenResponse.AccessToken, "BearerToken");
        }

        if (proof == null)
        {
            return CreateResult("error", "no dpop proof given");
        }

        var jti = GetFromJwt(proof, "jti");
        if (string.IsNullOrWhiteSpace(jti))
        {
            return CreateResult("error", "missig unique jti");
        }

        var givenNonce = GetFromJwt(proof, "nonce");

        if (string.IsNullOrWhiteSpace(givenNonce))
        {
            var val = Guid.NewGuid().ToString();
            _jtiToNonce.Add(jti, val);
            return CreateResult("error", OidcConstants.TokenErrors.UseDPoPNonce, val);
        }

        if (!_jtiToNonce.ContainsKey(jti) || givenNonce != _jtiToNonce[jti])
        {
            return CreateResult("error", "invalid_nonce");
        }

        return CreateResult(OidcConstants.TokenResponse.AccessToken, "DpopToken");
    }

    private string? GetFromJwt(string proof, string type)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(proof);
        return jwt.Claims.FirstOrDefault(x => x.Type == type)?.Value;
    }

    private HttpResponseMessage CreateResult(string key, string value, string? nonce = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        response.Content = JsonContent.Create(new Dictionary<string, string>()
        {
            { key, value }
        });

        if (nonce != null)
        {
            response.Headers.Add(OidcConstants.HttpHeaders.DPoPNonce, nonce);
        }

        return response;
    }
}