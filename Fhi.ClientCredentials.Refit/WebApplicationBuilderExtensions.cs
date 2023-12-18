using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Refit;

namespace Fhi.ClientCredentialsKeypairs.Refit
{
    public static class WebApplicationBuilderExtensions
    {
        public static RefitClientCredentialsBuilder AddClientCredentialsKeypairs(this WebApplicationBuilder builder, string? configSection = null, RefitSettings? refitSettings = null)
        {
            var configuration = builder.Configuration
                .GetSection(configSection ?? nameof(ClientCredentialsConfiguration))
                .Get<ClientCredentialsConfiguration>();

            return new RefitClientCredentialsBuilder(builder, configuration, refitSettings);
        }

        public static RefitClientCredentialsBuilder AddClientCredentialsKeypairs(this WebApplicationBuilder builder, RefitSettings? refitSettings = null)
        {
            var configuration = builder.Configuration
                .GetSection(nameof(ClientCredentialsConfiguration))
                .Get<ClientCredentialsConfiguration>();

            return new RefitClientCredentialsBuilder(builder, configuration, refitSettings);
        }
    }
}
