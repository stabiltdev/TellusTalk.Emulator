namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>The headers TellusTalk's K8 platform was seen sending with every answer to a send, refusals included.</summary>
internal static class TellusTalkHeaders
{
    public static void Apply(HttpResponse response, string requestId)
    {
        var headers = response.Headers;
        headers["X-Request-Id"] = requestId;
        headers.XContentTypeOptions = "nosniff";
        headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
    }
}
