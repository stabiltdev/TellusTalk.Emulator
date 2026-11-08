namespace Stabilt.TellusTalk.Emulator.Requests;

internal static class PhoneNumbers
{
    /// <summary>The specification's form of a number: <c>+</c> followed by digits only.</summary>
    public static bool IsPlusDigits(string value) =>
        value.Length > 1 && value[0] == '+' && !value.AsSpan(1).ContainsAnyExceptInRange('0', '9');
}
