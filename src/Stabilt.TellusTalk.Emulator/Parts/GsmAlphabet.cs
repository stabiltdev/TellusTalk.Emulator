using System.Buffers;

namespace Stabilt.TellusTalk.Emulator.Parts;

/// <summary>The GSM 03.38 alphabet an SMS is sent in when every character fits.</summary>
internal static class GsmAlphabet
{
    /// <summary>The default table without its escape character, which only introduces the extension table.</summary>
    private const string Basic =
        "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?" +
        "¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";

    /// <summary>Characters reached through the escape character, so each takes two septets.</summary>
    private const string Extension = "\f^{}\\[~]|€";

    private static readonly SearchValues<char> BasicCharacters = SearchValues.Create(Basic);

    private static readonly SearchValues<char> ExtensionCharacters = SearchValues.Create(Extension);

    public static bool IsBasic(char c) => BasicCharacters.Contains(c);

    public static bool IsExtension(char c) => ExtensionCharacters.Contains(c);
}
