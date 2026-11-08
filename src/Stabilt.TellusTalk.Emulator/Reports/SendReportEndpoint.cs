using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;
using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Reports;

internal sealed class SendReportEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapPost("api/v1/messages/{id}/reports", Handle);

    /// <summary>
    /// Sends a delivery report now, with the status asked for, whatever reports the message had before. It runs under
    /// the host's lifetime rather than the request's: a caller that stops waiting, such as a page navigated away from,
    /// must not leave a report sent to its receiver but missing from the message.
    /// </summary>
    private static async Task<Results<Ok<List<ReportAttemptResponse>>, NotFound, BadRequest<ApiError>>> Handle(
        MessageStore store,
        ReportSender sender,
        IHostApplicationLifetime lifetime,
        string id,
        ReportOrder order)
    {
        if (store.Find(id) is not { } message)
        {
            return TypedResults.NotFound();
        }

        ReportStatus? status = order.Status switch
        {
            "delivered" => ReportStatus.Delivered,
            "failed" => ReportStatus.Failed,
            _ => null,
        };

        if (status is not { } wanted)
        {
            return TypedResults.BadRequest(new ApiError("status must be delivered or failed."));
        }

        var attempts = await sender.Send(message, wanted, lifetime.ApplicationStopping);
        store.AddReports(message.Id, attempts);

        return TypedResults.Ok(attempts
            .Select(ReportAttemptResponse.From)
            .ToList());
    }

    private sealed record ReportOrder(string? Status);
}
