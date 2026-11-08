namespace Stabilt.TellusTalk.Emulator.Http;

/// <summary>One route. A sealed class with a parameterless constructor whose Map only wires the route to a private static Handle.</summary>
internal interface IEndpoint
{
    RouteHandlerBuilder Map(IEndpointRouteBuilder builder);
}
