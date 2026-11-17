using Stabilt.TellusTalk.Emulator.Ids;

namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>
/// Puts K8's headers on every answer the send endpoint gives. A send without credentials is refused before the
/// endpoint is reached, so <see cref="Authentication.BasicAuthenticationHandler"/> puts them on its 401 itself.
/// </summary>
internal sealed class TellusTalkHeadersFilter(TimeProvider time) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        TellusTalkHeaders.Apply(context.HttpContext.Response, Guid.CreateVersion1(time.GetUtcNow()).ToString("N"));

        return next(context);
    }
}
