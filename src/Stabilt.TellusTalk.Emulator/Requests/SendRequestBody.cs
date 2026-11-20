using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>
/// A send request's body: every field TellusTalk's send schemas define, for any channel. The schemas cannot be read one
/// at a time: the only <c>additionalProperties: false</c>, on <c>sms_mms_params</c>, would forbid even <c>to</c>, and
/// the specification's own list example sends <c>subject</c> to an SMS recipient. Each field keeps the JSON it arrived
/// as, whatever its type, so a value of the wrong type is a warning rather than a refusal; a field the body left out is
/// <see cref="JsonValueKind.Undefined"/> and one sent as null is <see cref="JsonValueKind.Null"/>.
/// </summary>
internal sealed class SendRequestBody
{
    /// <summary>Every field by its JSON name.</summary>
    public static FrozenSet<string> Names { get; } = typeof(SendRequestBody)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name)
        .ToFrozenSet(StringComparer.Ordinal);

    [JsonPropertyName("to")]
    public JsonElement To { get; init; }

    [JsonPropertyName("text")]
    public JsonElement Text { get; init; }

    [JsonPropertyName("html")]
    public JsonElement Html { get; init; }

    [JsonPropertyName("html_raw")]
    public JsonElement HtmlRaw { get; init; }

    [JsonPropertyName("subject")]
    public JsonElement Subject { get; init; }

    [JsonPropertyName("email_from_name")]
    public JsonElement EmailFromName { get; init; }

    [JsonPropertyName("attachments")]
    public JsonElement Attachments { get; init; }

    [JsonPropertyName("echo")]
    public JsonElement Echo { get; init; }

    [JsonPropertyName("scheduled_send_time")]
    public JsonElement ScheduledSendTime { get; init; }

    [JsonPropertyName("billing_id")]
    public JsonElement BillingId { get; init; }

    [JsonPropertyName("report_to")]
    public JsonElement ReportTo { get; init; }

    [JsonPropertyName("reply_to")]
    public JsonElement ReplyTo { get; init; }

    [JsonPropertyName("sms_max_parts")]
    public JsonElement SmsMaxParts { get; init; }

    [JsonPropertyName("sms_header")]
    public JsonElement SmsHeader { get; init; }

    [JsonPropertyName("sms_flash")]
    public JsonElement SmsFlash { get; init; }

    [JsonPropertyName("sms_validity_time")]
    public JsonElement SmsValidityTime { get; init; }

    [JsonPropertyName("sms_originator_text")]
    public JsonElement SmsOriginatorText { get; init; }

    [JsonPropertyName("sms_originator_number")]
    public JsonElement SmsOriginatorNumber { get; init; }

    [JsonPropertyName("sms_originator_source")]
    public JsonElement SmsOriginatorSource { get; init; }

    [JsonPropertyName("sms_single_thread")]
    public JsonElement SmsSingleThread { get; init; }

    [JsonPropertyName("sms_format")]
    public JsonElement SmsFormat { get; init; }

    [JsonPropertyName("sms_character_set")]
    public JsonElement SmsCharacterSet { get; init; }

    [JsonPropertyName("sms_udh")]
    public JsonElement SmsUdh { get; init; }

    [JsonPropertyName("to_language")]
    public JsonElement ToLanguage { get; init; }

    [JsonPropertyName("voice_speechrate")]
    public JsonElement VoiceSpeechrate { get; init; }

    [JsonPropertyName("public_message")]
    public JsonElement PublicMessage { get; init; }

    [JsonPropertyName("login_options")]
    public JsonElement LoginOptions { get; init; }

    [JsonPropertyName("sign_options")]
    public JsonElement SignOptions { get; init; }

    [JsonPropertyName("payment")]
    public JsonElement Payment { get; init; }
}
