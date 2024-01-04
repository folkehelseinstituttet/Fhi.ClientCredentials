using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Fhi.ClientCredentialsKeypairs.Refit
{
    public static class WebApplicationBuilderExtensions
    {
        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this WebApplicationBuilder builder, string? configSection = null, RefitSettings? refitSettings = null)
        {
            var configuration = builder.Configuration
                .GetSection(configSection ?? nameof(ClientCredentialsConfiguration))
                .Get<ClientCredentialsConfiguration>();

            return new RefitClientCredentialsBuilder(builder.Services, configuration, refitSettings);
        }

        public static RefitClientCredentialsBuilder AddClientCredentialsRefitBuilder(this IServiceCollection services, ClientCredentialsConfiguration configuration, RefitSettings? refitSettings = null)
        {
            return new RefitClientCredentialsBuilder(services, configuration, refitSettings);
        }
    }
}
