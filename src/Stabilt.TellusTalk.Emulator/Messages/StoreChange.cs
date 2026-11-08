namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>What happened to the store: <c>added</c>, <c>removed</c>, <c>cleared</c>, <c>due</c> or <c>reported</c>.</summary>
internal sealed record StoreChange(string Kind)
{
    public static readonly StoreChange Added = new("added");
    public static readonly StoreChange Removed = new("removed");
    public static readonly StoreChange Cleared = new("cleared");
    public static readonly StoreChange Due = new("due");
    public static readonly StoreChange Reported = new("reported");
}
