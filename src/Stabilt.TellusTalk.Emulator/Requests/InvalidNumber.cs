namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>Why K8 blocks an <c>sms:</c> number.</summary>
internal enum InvalidNumber
{
    /// <summary>Nothing after <c>sms:</c>.</summary>
    Missing,

    /// <summary>A <c>+</c> alone.</summary>
    NotANumber,

    /// <summary>Other characters than digits after an optional leading <c>+</c>.</summary>
    NotNumeric,
}
