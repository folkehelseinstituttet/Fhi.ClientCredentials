namespace Fhi.ClientCredentialsKeypairs;

public static class GlobalAuthenticationStore
{
    public static IAuthTokenStore? AuthTokenStore { get; private set; }
    public static IAuthenticationService? AuthenticatedService { get; private set; }

    public static void CreateGlobalTokenStore(IAuthTokenStore store, IAuthenticationService authenticatedService)
    {
        AuthTokenStore = store;
        AuthenticatedService = authenticatedService;
    }
}
