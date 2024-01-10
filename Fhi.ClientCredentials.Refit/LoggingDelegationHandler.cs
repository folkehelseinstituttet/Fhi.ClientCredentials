using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Web;

namespace Fhi.ClientCredentials.Refit;

public class LoggingDelegationHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegationHandler> logger;

    public LoggingDelegationHandler(ILogger<LoggingDelegationHandler> logger)
    {
        this.logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request
            .Headers
            .ToDictionary(x => x.Key, x => x.Value.First());

        foreach (var header in headers)
        {
            if (header.Key.StartsWith("fhi-"))
            {
                request.Headers.Remove(header.Key);
                request.Headers.Add(header.Key, HttpUtility.HtmlEncode(header.Value));
            }
        }

        var correalationId = request.Headers.FirstOrDefault(x => x.Key == CorrelationIdHandler.CorrelationIdHeaderName).Value.FirstOrDefault();

        var logUrl = AnonymizePersonalIdentifiers(request.RequestUri?.ToString());

        var start = DateTime.Now;
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            var time = (int)(DateTime.Now - start).TotalMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Requested HTTP {Method} {Url} in {ms}ms with response {StatusCode} {Reason} with CorrelationId: {CorrelationId}", request.Method, logUrl, time, (int)response.StatusCode, response.ReasonPhrase, correalationId);
            }
            else
            {
                logger.LogWarning("Requested HTTP {Method} {Url} in {ms}ms with response {StatusCode} {Reason} with CorrelationId: {CorrelationId}", request.Method, logUrl, time, (int)response.StatusCode, response.ReasonPhrase, correalationId);
            }

            return response;
        }
        catch (Exception ex)
        {
            var time = (int)(DateTime.Now - start).TotalMilliseconds;
            logger.LogError(ex, "Requested HTTP {Method} {Url} in {ms}ms with exception {Exception} with CorrelationId: {CorrelationId}", request.Method, logUrl, time, ex.Message, correalationId);
            throw;
        }
    }

    public static string? AnonymizePersonalIdentifiers(string? sourceToAnonymize)
    {
        if (string.IsNullOrWhiteSpace(sourceToAnonymize))
            return sourceToAnonymize;

        return Regex.Replace(sourceToAnonymize, "\\d{6,11}", "***********");
    }
}

