using Fhi.ClientCredentialsSecrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fhi.ClientCredentialsUsingSecrets;

public class ClientCredentialsSecretsSetup
{

    public IConfiguration Config { get; }
    private ClientCredentialsConfiguration ClientCredentialsConfiguration { get; }

    public ClientCredentialsSecretsSetup(IConfiguration config)
    {
        Config = config;
        ClientCredentialsConfiguration = Config.GetWorkerKonfigurasjon();

    }

    /// <summary>
    /// Use this for ClientSecret based
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAccessTokenManagement(options =>
        {
            foreach (var api in ClientCredentialsConfiguration.Apis)
            {
                options.Client.Clients.Add(api.Name, new IdentityModel.Client.ClientCredentialsTokenRequest
                {
                    Address = ClientCredentialsConfiguration.Authority,
                    ClientId = ClientCredentialsConfiguration.ClientId,
                    ClientSecret = ClientCredentialsConfiguration.ClientSecret,

                    Scope = api.Scope
                });
            }
        });


        foreach (var api in ClientCredentialsConfiguration.Apis)
        {
            if (ClientCredentialsConfiguration.Enable)
                ConfigureService(services, api);
            else
                ConfigureServiceNoAuth(services, api);
        }
    }

    public void ConfigureService(IServiceCollection services, ApiKonfigurasjon api)
    {
        services.AddClientAccessTokenHttpClient(api.Name, api.Name, configureClient: client =>
        {
            client.BaseAddress = new Uri(api.Url);

        });
    }

    public void ConfigureServiceNoAuth(IServiceCollection services, ApiKonfigurasjon api)
    {
        services.AddHttpClient(api.Name, client =>
        {
            client.BaseAddress = api.Uri;
        });
    }
}