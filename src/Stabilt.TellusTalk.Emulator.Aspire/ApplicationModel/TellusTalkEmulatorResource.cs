namespace Aspire.Hosting.ApplicationModel;

/// <summary>TellusTalk Emulator, a local stand-in for TellusTalk's K8 send API, run as a container.</summary>
public sealed class TellusTalkEmulatorResource(string name) : ContainerResource(name)
{
    internal const string HttpEndpointName = "http";

    private EndpointReference? _httpEndpoint;

    /// <summary>The endpoint that serves TellusTalk's send API, the emulator's page and its read API.</summary>
    public EndpointReference HttpEndpoint => _httpEndpoint ??= new EndpointReference(this, HttpEndpointName);
}
