using DotNet.Testcontainers.Images;

namespace Stabilt.TellusTalk.Emulator.Testcontainers.Tests;

/// <summary>
/// The emulator image a test starts: one built from this repository into the local Docker and named by
/// <c>TELLUSTALK_EMULATOR_TEST_IMAGE</c>, which CI sets. Without it the test is skipped, so the tests need no Docker
/// unless asked to.
/// </summary>
internal static class LocalImage
{
    private const string Variable = "TELLUSTALK_EMULATOR_TEST_IMAGE";

    public static TellusTalkEmulatorBuilder Builder()
    {
        var image = Environment.GetEnvironmentVariable(Variable);

        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(image),
            $"{Variable} names no image. Build one with `dotnet publish src/Stabilt.TellusTalk.Emulator -c Release -r linux-x64 -t:PublishContainer -p:LocalRegistry=Docker` and set it to stabiltdev/tellustalk-emulator:latest.");

        // Never pulled: the image is a local build, and the same name on a public registry could be anyone's.
        return new TellusTalkEmulatorBuilder(image).WithImagePullPolicy(PullPolicy.Never);
    }
}
