using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stabilt.TellusTalk.Emulator.Client;

internal static class ClientJson
{
    /// <summary>
    /// The JSON as the emulator writes it, enums included. An unknown property is ignored, so an older package reads an
    /// emulator that has only added properties.
    /// </summary>
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };
}
