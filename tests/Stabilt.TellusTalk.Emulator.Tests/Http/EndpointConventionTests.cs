using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Tests.Http;

public sealed class EndpointConventionTests
{
    private static readonly IReadOnlyList<Type> Endpoints = typeof(IEndpoint).Assembly
        .GetTypes()
        .Where(type => type is { IsAbstract: false, IsInterface: false })
        .Where(type => typeof(IEndpoint).IsAssignableFrom(type))
        .ToList();

    [Fact]
    public void Every_endpoint_is_sealed()
    {
        Endpoints.Should().NotBeEmpty().And.AllSatisfy(endpoint =>
            endpoint.IsSealed.Should().BeTrue("{0} is an endpoint", endpoint.FullName));
    }

    [Fact]
    public void Every_endpoint_is_named_with_an_Endpoint_suffix()
    {
        Endpoints.Should().NotBeEmpty().And.AllSatisfy(endpoint =>
            endpoint.Name.Should().EndWith("Endpoint", "{0} is an endpoint", endpoint.FullName));
    }
}
