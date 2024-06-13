namespace Fhi.ClientCredentialsKeypairs;

public partial class ClientCredentialsConfiguration
{
    public string Authority => authority;
    public string ClientId => clientId;
    public string Scopes => scopes == null ? "" : string.Join(" ", scopes);

    public string PrivateKey => privateJwk;

    /// <summary>
    /// Set this lower than the lifetime of the access token
    /// </summary>
    public int RefreshTokenAfterMinutes { get; set; } = 8;

    /// <summary>
    /// DPoP (Demonstrating Proof of Posssession in the Application Layer)
    /// Must be supported in the receving api.
    /// https://utviklerportal.nhn.no/informasjonstjenester/helseid/bruksmoenstre-og-eksempelkode/bruk-av-helseid/docs/dpop/dpop_no_nbmd/
    /// </summary>
    public bool UseDpop { get; set; } = false;

    public List<Api> Apis { get; set; } = new();

    public Uri UriToApiByName(string name)
    {
        var url = Apis.FirstOrDefault(o => o.Name == name)?.Url ?? throw new InvalidApiNameException(name); ;
        return new Uri(url);
    }
}

public class Api
{
    /// <summary>
    /// User friendly name of the Api, prefer using nameof(WhateverApiService)
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Actual Url to Api
    /// </summary>
    public string Url { get; set; } = "";
}