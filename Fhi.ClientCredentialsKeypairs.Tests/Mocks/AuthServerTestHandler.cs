namespace Fhi.ClientCredentialsKeypairs.Tests.Mocks;

public class AuthServerTestHandler : HttpMessageHandler
{
    public bool EnableDpop { get; set; } = false;

    public const string ExpectedJwt = "TESTJWT";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeaders = request.Headers.GetValues("Authorization").ToArray();

        if (authHeaders.Length != 1)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
        }

        var scheme = authHeaders[0].Split(' ')[0];
        var token = authHeaders[0].Split(' ')[1];

        if (EnableDpop && scheme != HttpAuthHandler.DpopSchemeType)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
        }

        if (!EnableDpop && scheme == HttpAuthHandler.DpopSchemeType)
        {
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            resp.Headers.Add("WWW-Authenticate", $"{HttpAuthHandler.BearerSchemeType} error='invalid_scheme'");
            return Task.FromResult(resp);
        }

        if (token != ExpectedJwt)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
