namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>
/// Answers with a failure armed for any send, which stands for a TellusTalk that is down. It runs before the handler
/// reads the request, so it answers whatever the request holds.
/// </summary>
internal sealed class AnySendFailureFilter(SendFailures failures, TimeProvider time) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (failures.TrySpend() is not { } failure)
        {
            return await next(context);
        }

        return await failure.WaitOutDelay(time, context.HttpContext.RequestAborted)
            ? Refusals.Failure(failure.Status)
            : TypedResults.Empty;
    }
}
