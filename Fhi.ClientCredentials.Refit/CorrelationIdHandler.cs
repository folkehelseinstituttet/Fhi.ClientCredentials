using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Fhi.ClientCredentials.Refit;

public class CorrelationIdHandler : DelegatingHandler
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HeaderPropagationValues _headerValues;
    private readonly IOptions<HeaderPropagationOptions> _headerPropagationOptions;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor,
        HeaderPropagationValues values,
        IOptions<HeaderPropagationOptions> headerPropagationOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _headerValues = values;
        _headerPropagationOptions = headerPropagationOptions;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();

        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var corrFromContext = context.Request.Headers.FirstOrDefault(x => x.Key == CorrelationIdHeaderName).Value.FirstOrDefault();
            if (corrFromContext != null)
            {
                correlationId = corrFromContext;
            }
        }

        if (request.Headers.TryGetValues(CorrelationIdHeaderName, out var values))
        {
            correlationId = values!.First();
        }
        else
        {
            request.Headers.Add(CorrelationIdHeaderName, correlationId);
        }

        // Populate the default header propagation values with values from headers if they are not previously set.
        // This is needed to be able to use the Refit-client outside a HttpContext
        _headerValues.Headers = _headerValues.Headers ?? GetHeadersFromContextAccessor();

        var response = await base.SendAsync(request, cancellationToken);

        if (!response.Headers.TryGetValues(CorrelationIdHeaderName, out _))
        {
            response.Headers.Add(CorrelationIdHeaderName, correlationId);
        }

        return response;
    }

    private IDictionary<string, StringValues> GetHeadersFromContextAccessor()
    {
        var headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        if (_httpContextAccessor.HttpContext?.Request?.Headers != null)
        {
            foreach (var entry in _headerPropagationOptions.Value.Headers)
            {
                if (!headers.ContainsKey(entry.CapturedHeaderName))
                {
                    _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(entry.CapturedHeaderName, out var value);
                    if (!StringValues.IsNullOrEmpty(value))
                    {
                        headers.Add(entry.CapturedHeaderName, value);
                    }
                }
            }
        }

        return headers;
    }
}
