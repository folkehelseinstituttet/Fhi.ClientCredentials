using Fhi.ClientCredentialsKeypairs;
using Fhi.ClientCredentialsKeypairs.Tests.Mocks;
using NSubstitute;
using System.Net;

namespace Fhi.ClientCredentialsUsingSecrets.Tests;

public class HttpAuthHandlerTests
{
    [Test]
    public async Task CanSendBearer()
    {
        var token = new JwtAccessToken()
        {
            TokenType = HttpAuthHandler.BearerSchemeType,
            AccessToken = AuthServerTestHandler.ExpectedJwt
        };

        var client = CreateClientWithHandler(token, enableDpopOnServer: false);

        var response = await client.GetAsync("https://test/");
        response.EnsureSuccessStatusCode();
    }

    [Test]
    public async Task CanSendDpop()
    {
        var token = new JwtAccessToken()
        {
            TokenType = HttpAuthHandler.DpopSchemeType,
            AccessToken = AuthServerTestHandler.ExpectedJwt
        };

        var client = CreateClientWithHandler(token, enableDpopOnServer: true);

        var response = await client.GetAsync("https://test/");
        response.EnsureSuccessStatusCode();
    }

    [Test]
    public async Task CannotSendBearerWhenDpop()
    {
        var token = new JwtAccessToken()
        {
            TokenType = HttpAuthHandler.BearerSchemeType,
            AccessToken = AuthServerTestHandler.ExpectedJwt
        };

        var client = CreateClientWithHandler(token, enableDpopOnServer: true);

        var response = await client.GetAsync("https://test/");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DowngradesDpopToBearer()
    {
        var token = new JwtAccessToken()
        {
            TokenType = HttpAuthHandler.DpopSchemeType,
            AccessToken = AuthServerTestHandler.ExpectedJwt
        };

        var client = CreateClientWithHandler(token, enableDpopOnServer: false);

        var response = await client.GetAsync("https://test/");
        response.EnsureSuccessStatusCode();
    }

    [Test]
    public async Task CannotSendWithWrongBearerToken()
    {
        var token = new JwtAccessToken()
        {
            TokenType = HttpAuthHandler.BearerSchemeType,
            AccessToken = "InvalidToken"
        };

        var client = CreateClientWithHandler(token, enableDpopOnServer: false);

        var response = await client.GetAsync("https://test/");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CannotSendWithWrongDpopToken()
    {
        var token = new JwtAccessToken()
        {
            TokenType = HttpAuthHandler.DpopSchemeType,
            AccessToken = "InvalidToken"
        };

        var client = CreateClientWithHandler(token, enableDpopOnServer: true);

        var response = await client.GetAsync("https://test/");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CannotSendWithWrongDpopTokenWhenDowngrade()
    {
        var token = new JwtAccessToken()
        {
            TokenType = HttpAuthHandler.DpopSchemeType,
            AccessToken = "InvalidToken"
        };

        var client = CreateClientWithHandler(token, enableDpopOnServer: false);

        var response = await client.GetAsync("https://test/");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private HttpClient CreateClientWithHandler(JwtAccessToken token, bool enableDpopOnServer)
    {
        var store = Substitute.For<IAuthTokenStore>();
        store.GetToken(Arg.Any<HttpMethod>(), Arg.Any<string>()).Returns(token);
        var handler = new HttpAuthHandler(store)
        {
            InnerHandler = new AuthServerTestHandler() { EnableDpop = enableDpopOnServer }
        };

        return new HttpClient(handler);
    }
}
