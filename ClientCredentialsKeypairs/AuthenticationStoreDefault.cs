namespace Fhi.ClientCredentialsKeypairs;

/// <summary>
/// Used in tests by different repos..
/// </summary>
[Obsolete("Create your own implementation of IAuthTokenStore for testing purposes.")]
public class AuthenticationStoreDefault : IAuthTokenStore
{
    private readonly string token;

    public AuthenticationStoreDefault(string token)
    {
        this.token = token;
    }
    public Task<string> GetToken() => Task.FromResult(token);

    public Task<JwtAccessToken> GetToken(HttpMethod method, string url)
    {
        return Task.FromResult(new JwtAccessToken()
        {
            AccessToken = token,
            TokenType = "Bearer",
        });
    }
}
