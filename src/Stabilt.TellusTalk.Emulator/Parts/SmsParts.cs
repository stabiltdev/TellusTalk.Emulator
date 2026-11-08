namespace Stabilt.TellusTalk.Emulator.Parts;

/// <summary>How an SMS splits into the parts an operator sends and bills.</summary>
/// <param name="CharacterSet">The alphabet the text is sent in.</param>
/// <param name="Length">Septets for GSM, UTF-16 code units for UCS-2, bytes for binary.</param>
/// <param name="Starts">Where each part begins: an index into the text, or a byte offset for binary; the first is 0.</param>
/// <param name="LeftInLastPart">The room left in the last part, in the same unit as <paramref name="Length"/>.</param>
internal sealed record SmsParts(SmsCharacterSet CharacterSet, int Length, IReadOnlyList<int> Starts, int LeftInLastPart)
{
    public int Count => Starts.Count;
}
