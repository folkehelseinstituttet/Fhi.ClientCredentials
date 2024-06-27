using Fhi.ClientCredentialsKeypairs.Tests.Mocks;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;

namespace Fhi.ClientCredentialsKeypairs.Tests;

public class AuthenticationServiceTests
{
    [Test]
    public async Task CanGetBearerToken()
    {
        var service = GetAuthenticationService(clientUseDpop: false, serverUseDpop: false);
        await service.SetupToken();

        var token = service.GetAccessToken(HttpMethod.Get, "https://test/help");
        Assert.That(token.AccessToken, Is.EqualTo("BearerToken"));
    }

    [Test]
    public async Task CanGetDpopToken()
    {
        var service = GetAuthenticationService(clientUseDpop: true, serverUseDpop: true);
        await service.SetupToken();

        var token = service.GetAccessToken(HttpMethod.Get, "https://test/help");
        Assert.That(token.AccessToken, Is.EqualTo("DpopToken"));
    }

    [Test]
    public async Task HandlesMismatchClientDpopServerBearer()
    {
        var service = GetAuthenticationService(clientUseDpop: true, serverUseDpop: false);
        await service.SetupToken();

        var token = service.GetAccessToken(HttpMethod.Get, "https://test/help");
        Assert.That(token.AccessToken, Is.EqualTo("BearerToken"));
    }

    [Test]
    public void HandlesMismatchClientBearerServerDpop()
    {
        var service = GetAuthenticationService(clientUseDpop: false, serverUseDpop: true);
        Assert.ThrowsAsync<Exception>(async () =>
        {
            await service.SetupToken();
        });
    }

    private AuthenticationService GetAuthenticationService(bool clientUseDpop, bool serverUseDpop)
    {
        var client = new HttpClient(new OauthTestServerHandler() { EnableDpop = serverUseDpop });

        var service = new AuthenticationService(client, new ClientCredentialsConfiguration()
        {
            authority = "https://test/oauth",
            clientId = "TEST",
            privateJwk = CreateJsonPrivateJwk(),
            UseDpop = clientUseDpop,
        });

        return service;
    }

    private string CreateJsonPrivateJwk()
    {
        var rsa = RSA.Create(2048);
        var privateKey = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = "TestKeyId" };
        var jwk1 = JsonWebKeyConverter.ConvertFromRSASecurityKey(privateKey);
        return JsonSerializer.Serialize(jwk1);
    }
}
