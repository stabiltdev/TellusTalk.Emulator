using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Stabilt.TellusTalk.Emulator.Testcontainers;

/// <summary>TellusTalk Emulator, a local stand-in for TellusTalk's K8 send API, run as a container.</summary>
public sealed class TellusTalkEmulatorContainer : DockerContainer
{
    internal TellusTalkEmulatorContainer(IContainerConfiguration configuration)
        : base(configuration)
    {
    }

    /// <summary>
    /// The address that serves TellusTalk's send API, the emulator's page and its read API, such as
    /// <c>http://127.0.0.1:32768/</c>: what the app under test takes where it would have <c>https://k8api.tellustalk.com/</c>.
    /// </summary>
    public string GetBaseAddress() =>
        new UriBuilder(Uri.UriSchemeHttp, Hostname, GetMappedPublicPort(TellusTalkEmulatorBuilder.TellusTalkEmulatorPort)).ToString();
}
