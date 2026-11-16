namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>A request the emulator cannot record; its text says it is the emulator's limit.</summary>
internal sealed class EmulatorLimit(int statusCode, string text) : TextResult(statusCode, PlainText, text);
