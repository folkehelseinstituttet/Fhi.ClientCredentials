using Fhi.ClientCredentialsKeypairs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Refit;

namespace Fhi.ClientCredentials.Refit.Tests;

public partial class RefitClientCredentialsBuilderTests
{
    private const string AccessTokenValue = "test-token";
    private const string ContextCorrelationId = "context-correlation-guid";

    [Test]
    public async Task CanCreateWorkingClient()
    {
        var options = new RefitClientCredentialsBuilderOptions();

        var client = CreateTestClient(options, out var provider);

        var response = await client.Info();

        await response.EnsureSuccessStatusCodeAsync();

        // the dummy client returns the authorization header
        Assert.That(response.Content, Is.EqualTo("Bearer " + AccessTokenValue));

        // the dummy client mirrors all "fhi-" headers. Let's check that they are encoded correctly
        Assert.That(response.Headers.Single(x => x.Key == ITestClient.TestHeaderName).Value.Single(),
            Is.EqualTo("test &#230;"));

        // check that the correlation id is picked up from the state
        Assert.That(response.Headers.Single(x => x.Key == CorrelationIdHandler.CorrelationIdHeaderName).Value.Single(),
        Is.EqualTo(ContextCorrelationId));

        var logger = (TestLogger<LoggingDelegationHandler>)provider.GetRequiredService<ILogger<LoggingDelegationHandler>>();
        Assert.That(logger.Entries.Any(x => x.Contains(ContextCorrelationId)), Is.True, "Correlation id not found: " + logger.Entries.First());
    }

    public ITestClient CreateTestClient(RefitClientCredentialsBuilderOptions options, out ServiceProvider provider)
    {
        var services = CreateDefaultServiceCollection();

        var config = new ClientCredentialsConfiguration()
        {
            Apis = new List<Api>() { new() { Url = "http://localhost", Name = "ITestClient" } }
        };

        services.AddSingleton(CreateContextAccessor());

        services.AddClientCredentialsRefitBuilder(config, options)
            .AddRefitClient<ITestClient>(nameof(ITestClient), clientBuilder =>
            clientBuilder.ConfigurePrimaryHttpMessageHandler(h =>
            {
                return new DummyInnerHandler();
            }));

        // Add authentication parameters for a logged in user
        var authStore = Substitute.For<IAuthTokenStore>();
        authStore.GetToken().Returns(Task.FromResult(AccessTokenValue));
        services.AddSingleton(authStore);

        provider = services.BuildServiceProvider();

        return provider.GetRequiredService<ITestClient>();
    }

    private ServiceCollection CreateDefaultServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(TestLogger<>));
        return services;
    }

    public IHttpContextAccessor CreateContextAccessor()
    {
        var context = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        context.Request.Returns(request);

        var headers = new HeaderDictionary();
        headers.TryAdd(CorrelationIdHandler.CorrelationIdHeaderName, ContextCorrelationId);
        request.Headers.Returns(headers);

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        return accessor;
    }

    public interface ITestClient
    {
        public const string TestHeaderName = "fhi-encoded";

        [Get("/info")]
        [Headers($"{TestHeaderName}: test æ")]
        Task<ApiResponse<string>> Info();
    }
}