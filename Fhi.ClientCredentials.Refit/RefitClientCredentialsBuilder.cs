using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fhi.ClientCredentialsKeypairs.Refit
{
    public class RefitClientCredentialsBuilder
    {
        private List<Type> DelegationHandlers = new();
        private readonly WebApplicationBuilder builder;
        private readonly ClientCredentialsConfiguration config;

        public RefitSettings RefitSettings { get; set; }

        public RefitClientCredentialsBuilder(WebApplicationBuilder builder, ClientCredentialsConfiguration config, RefitSettings? refitSettings)
        {
            this.RefitSettings = refitSettings ?? CreateRefitSettings();

            this.builder = builder;
            this.config = config;

            builder.Services.AddTransient<IAuthenticationService>(_ => new AuthenticationService(config));
            builder.Services.AddSingleton<IAuthTokenStore, AuthenticationStore>();

            AddHandler<HttpAuthHandler>();
        }

        public RefitClientCredentialsBuilder AddHandler<T>() where T : DelegatingHandler
        {
            DelegationHandlers.Add(typeof(T));
            builder.Services.AddTransient<T>();
            return this;
        }

        public RefitClientCredentialsBuilder ClearHandlers()
        {
            DelegationHandlers.Clear();
            return this;
        }

        public RefitClientCredentialsBuilder AddRefitClient<T>(string? nameOfService = null, Func<IHttpClientBuilder, IHttpClientBuilder>? extra = null) where T : class
        {
            var clientBuilder = builder.Services.AddRefitClient<T>(RefitSettings)
                .ConfigureHttpClient(httpClient =>
                {
                    httpClient.BaseAddress = config.UriToApiByName(nameOfService ?? typeof(T).Name);
                });

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
}
