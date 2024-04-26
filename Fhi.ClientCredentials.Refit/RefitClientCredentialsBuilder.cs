using Fhi.ClientCredentialsKeypairs;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fhi.ClientCredentials.Refit;

public class RefitClientCredentialsBuilder
{
    private List<Type> DelegationHandlers = new();
    private readonly IServiceCollection services;
    private readonly ClientCredentialsConfiguration clientCredentialsConfig;
    private readonly RefitClientCredentialsBuilderOptions options = new RefitClientCredentialsBuilderOptions();

    public RefitSettings RefitSettings { get; set; }

    public RefitClientCredentialsBuilder(IServiceCollection services, ClientCredentialsConfiguration config, RefitSettings? refitSettings)
    {
        RefitSettings = refitSettings ?? CreateRefitSettings();

        this.services = services;
        clientCredentialsConfig = config;

        services.AddTransient<IAuthenticationService>(_ => new AuthenticationService(config));
        services.AddSingleton<IAuthTokenStore, AuthenticationStore>();

        services.AddSingleton(options);

        AddHandler<HttpAuthHandler>();
        AddHandler<FhiHeaderDelegationHandler>();
    }

    public RefitClientCredentialsBuilder AddHandler<T>() where T : DelegatingHandler
    {
        DelegationHandlers.Add(typeof(T));
        services.AddTransient<T>();
        return this;
    }

    public RefitClientCredentialsBuilder ClearHandlers()
    {
        DelegationHandlers.Clear();
        return this;
    }

    /// <summary>
    /// Adds propagation and handling of correlation ids. You should add this before any logging-delagates. Remember to add "app.UseCorrelationId()" in your startup code
    /// </summary>
    /// <returns></returns>
    public RefitClientCredentialsBuilder AddCorrelationId()
    {
        options.UseCorrelationId = true;

        AddHandler<CorrelationIdHandler>();

        services.AddHttpContextAccessor();

        return this;
    }

    /// <summary>
    /// The default implementation of HttpClientFactry sets the complete URI in the logging Scope,
    /// which might contain sensitive information that we are not able to remove.
    /// We therefore wish to remove the default logger. Use this method if you want to preserve it.
    /// </summary>
    /// <param name="preserveDefaultLogger"></param>
    /// <returns></returns>
    public RefitClientCredentialsBuilder ConfigureDefaultLogging(bool preserveDefaultLogger)
    {
        options.PreserveDefaultLogger = preserveDefaultLogger;
        return this;
    }

    public RefitClientCredentialsBuilder AddRefitClient<T>(string? nameOfService = null, Func<IHttpClientBuilder, IHttpClientBuilder>? extra = null) where T : class
    {
        var clientBuilder = services.AddRefitClient<T>(RefitSettings)
            .ConfigureHttpClient(httpClient =>
            {
                httpClient.BaseAddress = clientCredentialsConfig.UriToApiByName(nameOfService ?? typeof(T).Name);
            });

        if (options.PreserveDefaultLogger == false ||
            (options.PreserveDefaultLogger == null &&
            DelegationHandlers.Any(x => x == typeof(LoggingDelegationHandler))))
        {
            clientBuilder.RemoveAllLoggers();
        }

        foreach (var type in DelegationHandlers)
        {
            clientBuilder.AddHttpMessageHandler((s) => (DelegatingHandler)s.GetRequiredService(type));
        }

        extra?.Invoke(clientBuilder);

        return this;
    }

    private static RefitSettings CreateRefitSettings()
    {
        var jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var refitSettings = new RefitSettings()
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions),
        };

        return refitSettings;
    }
}
