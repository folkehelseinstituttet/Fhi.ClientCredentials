using NUnit.Framework;

namespace Fhi.ClientCredentials.Refit.Tests;

public class FhiHeaderDelegationHandlerTests
{
    [Test]
    public async Task HandlerEncodesValues()
    {
        var headerName = "fhi-test";

        var handler = new FhiHeaderDelegationHandler();
        handler.InnerHandler = new DummyInnerHandler();

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.TryAddWithoutValidation(headerName, "'Fornavn' Ætternavn");

        var response = await client.GetAsync("http://localhost/");

        var token = await response.Content.ReadAsStringAsync();

        // check that we get the correlation id from HelseIdState
        Assert.That(response.Headers.Single(x => x.Key == headerName).Value.Single(),
            Is.EqualTo("&#39;Fornavn&#39; &#198;tternavn"));
    }
}