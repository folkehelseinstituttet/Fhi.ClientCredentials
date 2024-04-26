using System.Net.Http.Headers;

namespace Fhi.ClientCredentialsKeypairs
{
    public class HttpAuthHandler : DelegatingHandler
    {
        public const string AnonymousOptionKey = "Anonymous";

        private readonly IAuthTokenStore _authTokenStore;

        public HttpAuthHandler(IAuthTokenStore authTokenStore)
        {
            _authTokenStore = authTokenStore;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!request.Options.Any(x => x.Key == AnonymousOptionKey))
            {
                var token = await _authTokenStore.GetToken();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}
