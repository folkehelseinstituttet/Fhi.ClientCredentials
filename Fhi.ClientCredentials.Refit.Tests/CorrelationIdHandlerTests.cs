using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Fhi.ClientCredentials.Refit.Tests;

public class CorrelationIdHandlerTests
{
    [Test]
    public async Task HandlerAddsCorrelationHeader()
    {
        var correlationId = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        var provider = services.BuildServiceProvider();

        var handler = new CorrelationIdHandler(CreateContext(correlationId));
        handler.InnerHandler = new DummyInnerHandler();

        var client = new HttpClient(handler);
        var response = await client.GetAsync("http://localhost/");

        var token = await response.Content.ReadAsStringAsync();

        // check that we get the correlation id from HelseIdState
        Assert.That(response.Headers.Single(x => x.Key == CorrelationIdHandler.CorrelationIdHeaderName).Value.Single(),
            Is.EqualTo(correlationId));
    }

    private IHttpContextAccessor CreateContext(string? correlationId)
    {
        var headers = new HeaderDictionary();
        if (correlationId != null)
        {
            headers.TryAdd(CorrelationIdHandler.CorrelationIdHeaderName, correlationId);
        }

        var context = Substitute.For<HttpContext>();
        context.Request.Headers.Returns(headers);

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        return accessor;
    }
}