namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>Why a caught message was not sent.</summary>
internal enum BlockedReason
{
    /// <summary>It duplicates a send received within five minutes, which held it back.</summary>
    Duplicate,

    /// <summary>K8 logs it as <c>txtNUMBER_NOT_NUMERIC</c>.</summary>
    NumberNotNumeric,
}
