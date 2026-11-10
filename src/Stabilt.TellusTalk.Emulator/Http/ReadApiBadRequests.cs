namespace Stabilt.TellusTalk.Emulator.Http;

/// <summary>
/// Gives the read API's error body to a <c>400</c> or <c>415</c> the framework left empty, which it does when it cannot
/// bind a request, such as a body that is not the JSON its endpoint takes. The read API's own refusals always carry
/// the body, so an empty one is the framework's. Endpoint filters cannot do this: they never run when it is the body
/// that fails to bind.
/// </summary>
internal static class ReadApiBadRequests
{
    public static async Task Answer(HttpContext context, RequestDelegate next)
    {
        await next(context);

        var response = context.Response;
        if (IsReadApi(context) && !response.HasStarted && response.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status415UnsupportedMediaType)
        {
            await response.WriteAsJsonAsync(new ApiError(Message(response.StatusCode)), context.RequestAborted);
        }
    }

    private static bool IsReadApi(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase);

    private static string Message(int statusCode) => statusCode == StatusCodes.Status415UnsupportedMediaType
        ? "The request body must be JSON."
        : "The request body is missing or is not the JSON this endpoint takes.";
}
