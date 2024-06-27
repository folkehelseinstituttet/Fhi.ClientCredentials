using Microsoft.Extensions.Options;

namespace Fhi.ClientCredentialsKeypairs.Tests;

public class AuthenticationStoreTests
{
    [Test]
    public async Task CanGetLegacyToken()
    {
        var service = GetService(true);
        var token = await service.GetToken();
        Assert.That(token, Is.EqualTo("TestToken"));
    }

    [Test]
    public async Task CanGetDpop()
    {
        var service = GetService(true);
        var token = await service.GetToken(HttpMethod.Get, "http://test/help");
        Assert.That(token.AccessToken, Is.EqualTo("TestToken"));
        Assert.That(token.TokenType, Is.EqualTo("DPoP"));
        Assert.That(token.DpopProof, Is.EqualTo("Proof"));
    }

    [Test]
    public async Task CanGetBearerIfDpopDisabled()
    {
        var service = GetService(false);
        var token = await service.GetToken(HttpMethod.Get, "http://test/help");
        Assert.That(token.AccessToken, Is.EqualTo("TestToken"));
        Assert.That(token.TokenType, Is.EqualTo("Bearer"));
        Assert.That(token.DpopProof, Is.EqualTo(null));
    }


    private AuthenticationStore GetService(bool useDpop)
    {
        return new AuthenticationStore(new TestAuthenticationService(useDpop), Options.Create(new ClientCredentialsConfiguration()));
    }

    private class TestAuthenticationService(bool useDpop) : IAuthenticationService
    {
        private string? _accessToken;

        public string AccessToken => _accessToken ?? throw new Exception("No access token avalible");

        public JwtAccessToken GetAccessToken(HttpMethod method, string url)
        {
            return new JwtAccessToken()
            {
                AccessToken = AccessToken,
                TokenType = useDpop ? "DPoP" : "Bearer",
                DpopProof = useDpop ? "Proof" : null,
            };
        }

        public Task SetupToken()
        {
            _accessToken = "TestToken";
            return Task.CompletedTask;
        }
    }
}
