using Microsoft.Extensions.Options;
using System.Reflection.PortableExecutable;

namespace Fhi.ClientCredentialsKeypairs
{
    public class AuthenticationStore : IAuthTokenStore
    {
        private readonly IAuthenticationService authenticationService;
        private DateTime tokenDateTime;
        private readonly int refreshTokenAfterMinutes;

        public AuthenticationStore(IAuthenticationService authenticationService, IOptions<ClientCredentialsConfiguration> configuration)
        {
            refreshTokenAfterMinutes = configuration.Value.RefreshTokenAfterMinutes;
            this.authenticationService = authenticationService;
            this.tokenDateTime = DateTime.MinValue;
        }

        public async Task<string> GetToken()
        {
            if ((DateTime.Now - tokenDateTime).TotalMinutes > refreshTokenAfterMinutes)
            {
                await Refresh();
            }
            return authenticationService.AccessToken;
        }

        private async Task Refresh()
        {
            await authenticationService.SetupToken();
            tokenDateTime = DateTime.Now;
        }
    }
}
