using Fhi.ClientCredentials.Refit;
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
        private readonly IServiceCollection services;
        private readonly ClientCredentialsConfiguration config;

        public RefitSettings RefitSettings { get; set; }

        public RefitClientCredentialsBuilder(IServiceCollection services, ClientCredentialsConfiguration config, RefitSettings? refitSettings)
        {
            this.RefitSettings = refitSettings ?? CreateRefitSettings();

            this.services = services;
            this.config = config;

            services.AddTransient<IAuthenticationService>(_ => new AuthenticationService(config));
            services.AddSingleton<IAuthTokenStore, AuthenticationStore>();

            AddHandler<HttpAuthHandler>();
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
        /// Adds propagation and handling of correlation ids. You should add this before any logging-delagates. Remember to add "app.UseHeaderPropagation()" in your startup code
        /// </summary>
        /// <returns></returns>
        public RefitClientCredentialsBuilder AddCorrelationId()
        {
            AddHandler<CorrelationIdHandler>();

            services.AddHeaderPropagation(o =>
            {
                o.Headers.Add(CorrelationIdHandler.CorrelationIdHeaderName, context => string.IsNullOrEmpty(context.HeaderValue) ? Guid.NewGuid().ToString() : context.HeaderValue);
            });

            return this;
        }

        public RefitClientCredentialsBuilder AddRefitClient<T>(string? nameOfService = null, Func<IHttpClientBuilder, IHttpClientBuilder>? extra = null) where T : class
        {
            var clientBuilder = services.AddRefitClient<T>(RefitSettings)
                .ConfigureHttpClient(httpClient =>
                {
                    httpClient.BaseAddress = config.UriToApiByName(nameOfService ?? typeof(T).Name);
                })
                .AddHeaderPropagation();

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
