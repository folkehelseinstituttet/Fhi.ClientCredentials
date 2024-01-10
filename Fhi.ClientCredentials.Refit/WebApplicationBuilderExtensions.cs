using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Fhi.ClientCredentials.Refit;

namespace Fhi.ClientCredentialsKeypairs.Refit
{
    public static class WebApplicationBuilderExtensions
    {
        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this WebApplicationBuilder builder, string? configSection = null, RefitSettings? refitSettings = null)
        {
            return AddClientCredentialsRefitBuilder(builder.Services, builder.Configuration, configSection, refitSettings);
        }

        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this IServiceCollection services, IConfiguration configuration, string? configSection = null, RefitSettings? refitSettings = null)
        {
            var config = configuration
                .GetSection(configSection ?? nameof(ClientCredentialsConfiguration))
                .Get<ClientCredentialsConfiguration>();

            return AddClientCredentialsRefitBuilder(services, config, refitSettings);
        }

        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this IServiceCollection services, ClientCredentialsConfiguration configuration, RefitSettings? refitSettings = null)
        {
            return new RefitClientCredentialsBuilder(services, configuration, refitSettings);
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
                throw new Exception("You need to call builder.AddHelseIdForBlazor() before using app.UseHelseIdForBlazor()");
            }

            if (options.UseCorrelationId)
            {
                app.UseMiddleware<CorrelationIdMiddleware>();
            }

            return app;
        }
    }
}
