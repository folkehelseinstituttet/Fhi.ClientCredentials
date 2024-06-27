namespace Fhi.ClientCredentialsKeypairs;

/// <summary>
/// (legacy?) Auth handler in use by 
/// Fhi.Koronasertifikat.QrPdfApi.Api.IntegrationTests
/// Fhi.Grunndata.PersonoppslagApi.IntegrationTests 
/// Fhi.Grunndata.OppslagAdmin
/// some people use this one when creating new HttpClient manually..
/// </summary>
[Obsolete("Use HttpAuthHandler and assign InnerHandler = new HttpClientHandler() manually or use HttpClientFactory.")]
public class AuthHeaderHandler : HttpAuthHandler
{
    public AuthHeaderHandler(IAuthTokenStore? authTokenStore) 
        : base(authTokenStore ?? throw new ArgumentNullException(nameof(authTokenStore)))
    {
        InnerHandler = new HttpClientHandler();
    }
}
