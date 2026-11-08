using System.Runtime.CompilerServices;

namespace Stabilt.TellusTalk.Emulator.Tests;

/// <summary>
/// FluentAssertions 8 is free for open-source projects. Accepting its licence here, once per test assembly, silences
/// the licence notice. Only the tests use it; it never ships in the image.
/// </summary>
internal static class AcceptFluentAssertionsLicense
{
    [ModuleInitializer]
    internal static void Accept() => License.Accepted = true;
}
