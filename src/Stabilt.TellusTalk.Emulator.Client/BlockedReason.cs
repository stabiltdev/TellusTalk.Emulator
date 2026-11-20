namespace Stabilt.TellusTalk.Emulator.Client;

/// <summary>Why a caught message was not sent.</summary>
public enum BlockedReason
{
    /// <summary>It duplicates a send received within five minutes, which held it back.</summary>
    Duplicate,

    /// <summary>Its SMS number holds other characters than digits after an optional leading <c>+</c>.</summary>
    NumberNotNumeric,
}
