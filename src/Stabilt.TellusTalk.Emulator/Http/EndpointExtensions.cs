namespace Stabilt.TellusTalk.Emulator.Http;

internal static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        var endpoints = typeof(IEndpoint).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => typeof(IEndpoint).IsAssignableFrom(type));

        foreach (var type in endpoints)
        {
            ((IEndpoint)Activator.CreateInstance(type)!).Map(builder);
        }

        return builder;
    }
}
