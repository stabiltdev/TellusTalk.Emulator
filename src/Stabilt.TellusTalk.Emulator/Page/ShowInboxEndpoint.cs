using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Page;

internal sealed class ShowInboxEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("/", Handle);

    private static RazorComponentResult<InboxPage> Handle(string? tab, string? id, string? q) =>
        new(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(InboxPage.Tab)] = tab,
            [nameof(InboxPage.SelectedId)] = id,
            [nameof(InboxPage.Search)] = q,
        });
}
