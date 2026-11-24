namespace Stabilt.TellusTalk.Emulator.Requests;

internal static class PhoneNumbers
{
    /// <summary>The specification's form of a number: <c>+</c> followed by digits only.</summary>
    public static bool IsPlusDigits(string value) =>
        value.Length > 1 && value[0] == '+' && !value.AsSpan(1).ContainsAnyExceptInRange('0', '9');

    /// <summary>
    /// The number K8 was seen to read a number of digits without <c>+</c> as: a local one, with a single leading
    /// <c>0</c>, and one beginning with <c>46</c> as Swedish, whatever the user's country code. Any other is
    /// unobserved.
    /// </summary>
    public static string? AsSwedish(string value) => value switch
    {
        { Length: < 3 } => null,
        _ when value.AsSpan().ContainsAnyExceptInRange('0', '9') => null,
        ['4', '6', ..] => "+" + value,
        ['0', not '0', ..] => "+46" + value[1..],
        _ => null,
    };

    /// <summary>Why K8 blocks an SMS number, if it does.</summary>
    public static InvalidNumber? Invalid(string value) => value switch
    {
        "" => InvalidNumber.Missing,
        "+" => InvalidNumber.NotANumber,
        _ when value.AsSpan(value is ['+', ..] ? 1 : 0).ContainsAnyExceptInRange('0', '9') => InvalidNumber.NotNumeric,
        _ => null,
    };
}
