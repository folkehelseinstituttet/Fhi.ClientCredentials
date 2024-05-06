using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Fhi.ClientCredentialsKeypairs;

namespace Fhi.ClientCredentials.Refit
{
    public static class WebApplicationBuilderExtensions
    {
        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this WebApplicationBuilder builder, string? configSection = null, RefitClientCredentialsBuilderOptions? builderOptions = null, RefitSettings? refitSettings = null)
        {
            return AddClientCredentialsRefitBuilder(builder.Services, builder.Configuration, configSection, builderOptions, refitSettings);
        }

        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this IServiceCollection services, IConfiguration configuration, string? configSection = null, RefitClientCredentialsBuilderOptions? builderOptions = null, RefitSettings? refitSettings = null)
        {
            var config = configuration
                .GetSection(configSection ?? nameof(ClientCredentialsConfiguration))
                .Get<ClientCredentialsConfiguration>();

            return AddClientCredentialsRefitBuilder(services, config, builderOptions, refitSettings);
        }

        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this IServiceCollection services, ClientCredentialsConfiguration configuration, RefitClientCredentialsBuilderOptions? builderOptions = null, RefitSettings? refitSettings = null)
        {
            return new RefitClientCredentialsBuilder(services, configuration, refitSettings, builderOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static T UseCorrelationId<T>(this T app) where T : IApplicationBuilder
        {
            var options = app.ApplicationServices.GetService<RefitClientCredentialsBuilderOptions>();
            if (options == null)
            {
                throw new Exception("You need to call builder.AddClientCredentialsRefitBuilder() before using app.UseCorrelationId()");
            }

            if (options.UseCorrelationId)
            {
                app.UseMiddleware<CorrelationIdMiddleware>();
            }

            return app;
        }
    }
}
