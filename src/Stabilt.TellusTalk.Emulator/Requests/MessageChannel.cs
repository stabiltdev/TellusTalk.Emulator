namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>The channel a recipient's address names. Not called <c>Channel</c>, which <c>System.Threading.Channels</c> owns.</summary>
internal enum MessageChannel
{
    Sms,
    Email,
    Fax,
    Mms,
    Voice,
    Group,
}
