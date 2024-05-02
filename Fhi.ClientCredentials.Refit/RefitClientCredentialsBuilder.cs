using Fhi.ClientCredentialsKeypairs;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fhi.ClientCredentials.Refit;

public class RefitClientCredentialsBuilder
{
    private readonly List<Type> DelegationHandlers = new();
    private readonly IServiceCollection services;
    private readonly ClientCredentialsConfiguration clientCredentialsConfig;
    private readonly RefitClientCredentialsBuilderOptions builderOptions;
    private readonly RefitSettings refitSettings;

    public RefitClientCredentialsBuilder(
        IServiceCollection services, 
        ClientCredentialsConfiguration config, 
        RefitSettings? refitSettings, 
        RefitClientCredentialsBuilderOptions? options)
    {
        this.refitSettings = refitSettings ?? CreateRefitSettings();
        this.services = services;
        builderOptions = options ?? new RefitClientCredentialsBuilderOptions();
        clientCredentialsConfig = config;

        services.AddTransient<IAuthenticationService>(_ => new AuthenticationService(config));
        services.AddSingleton<IAuthTokenStore, AuthenticationStore>();

        services.AddSingleton(builderOptions);

        if (builderOptions.UseDefaultTokenHandler)
        {
            AddHandler<HttpAuthHandler>();
        }
        if (builderOptions.HtmlEncodeFhiHeaders)
        {
            AddHandler<FhiHeaderDelegationHandler>();
        }
        if (builderOptions.UseCorrelationId)
        {
            AddHandler<CorrelationIdHandler>();

            services.AddHttpContextAccessor();
            services.AddHeaderPropagation(o =>
            {
                o.Headers.Add(CorrelationIdHandler.CorrelationIdHeaderName, context => string.IsNullOrEmpty(context.HeaderValue) ? Guid.NewGuid().ToString() : context.HeaderValue);
            });
        }
        if (builderOptions.UseAnonymizationLogger)
        {
            AddHandler<LoggingDelegationHandler>();
        }
    }

    /// <summary>
    /// Add a custom handler that will be dependency-injected into the Refit client.
    /// </summary>
    /// <typeparam name="T">A DelegatingHandler</typeparam>
    /// <returns></returns>
    public RefitClientCredentialsBuilder AddHandler<T>() where T : DelegatingHandler
    {
        var type = typeof(T);
        if (!DelegationHandlers.Any(x => x == type))
        {
            DelegationHandlers.Add(typeof(T));
            services.AddTransient<T>();
        }
        return this;
    }

    /// <summary>
    /// Add a RefitClient to the DI-container.
    /// </summary>
    /// <typeparam name="T">Refit interface</typeparam>
    /// <param name="nameOfService">Name of the service in the ClientCredentials configuration file</param>
    /// <param name="extra">Extra IHttpClientBuilder steps</param>
    /// <returns></returns>
    public RefitClientCredentialsBuilder AddRefitClient<T>(string? nameOfService = null, Func<IHttpClientBuilder, IHttpClientBuilder>? extra = null) where T : class
    {
        var clientBuilder = services.AddRefitClient<T>(refitSettings)
            .ConfigureHttpClient(httpClient =>
            {
                httpClient.BaseAddress = clientCredentialsConfig.UriToApiByName(nameOfService ?? typeof(T).Name);
            });

        if (!builderOptions.PreserveDefaultLogger)
        {
            clientBuilder.RemoveAllLoggers();
        }

        if (builderOptions.UseCorrelationId)
        {
            clientBuilder.AddHeaderPropagation();
            // populate the HeaderPropagationValues dictionary so we can create the client regardless of being in or outside a HttpContext
            clientBuilder.ConfigureHttpClient((sp, client) =>
            {
                var values = sp.GetRequiredService<HeaderPropagationValues>();
                values.Headers = values.Headers ?? new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            });
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
