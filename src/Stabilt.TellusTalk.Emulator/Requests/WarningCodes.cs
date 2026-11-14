namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>The codes a test can assert on; the read API and the README publish them, so they never change.</summary>
internal static class WarningCodes
{
    public const string UnknownField = "unknown-field";
    public const string WrongJsonType = "wrong-json-type";
    public const string MissingPrefix = "to-missing-prefix";
    public const string SpacesRemoved = "to-spaces-removed";
    public const string NotDigits = "to-not-digits";
    public const string OriginatorTextTooLong = "originator-text-too-long";
    public const string SubjectTooLong = "subject-too-long";
    public const string OriginatorNumberInvalid = "originator-number-invalid";
    public const string SingleThreadWithoutReplySms = "single-thread-without-replysms";
    public const string CharacterSetInvalid = "character-set-invalid";
    public const string FormatDeprecated = "format-deprecated";
    public const string BinaryWithoutUdh = "binary-without-udh";
    public const string BinaryNotHex = "binary-not-hex";
    public const string GsmUnsupportedCharacters = "gsm-unsupported-characters";
    public const string MaxPartsInvalid = "max-parts-invalid";
    public const string MaxPartsExceeded = "max-parts-exceeded";
    public const string BillingIdInvalid = "billing-id-invalid";
    public const string ScheduledTimeInvalid = "scheduled-time-invalid";
    public const string ReportAddressInvalid = "report-address-invalid";
}
