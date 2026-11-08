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
    /// <summary>Every field by its JSON name, with the shape the schemas give it where they fix one.</summary>
    public static FrozenDictionary<string, JsonShape?> Known { get; } = typeof(SendRequestBody)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .ToFrozenDictionary(
            property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name,
            property => property.GetCustomAttribute<ExpectsAttribute>()?.Shape,
            StringComparer.Ordinal);

    [JsonPropertyName("to")]
    public JsonElement To { get; init; }

    [JsonPropertyName("text")]
    [Expects(JsonShape.String)]
    public JsonElement Text { get; init; }

    [JsonPropertyName("html")]
    [Expects(JsonShape.String)]
    public JsonElement Html { get; init; }

    [JsonPropertyName("html_raw")]
    public JsonElement HtmlRaw { get; init; }

    [JsonPropertyName("subject")]
    [Expects(JsonShape.String)]
    public JsonElement Subject { get; init; }

    [JsonPropertyName("email_from_name")]
    public JsonElement EmailFromName { get; init; }

    [JsonPropertyName("attachments")]
    [Expects(JsonShape.List)]
    public JsonElement Attachments { get; init; }

    [JsonPropertyName("echo")]
    public JsonElement Echo { get; init; }

    [JsonPropertyName("scheduled_send_time")]
    [Expects(JsonShape.String)]
    public JsonElement ScheduledSendTime { get; init; }

    [JsonPropertyName("billing_id")]
    [Expects(JsonShape.String)]
    public JsonElement BillingId { get; init; }

    [JsonPropertyName("report_to")]
    [Expects(JsonShape.Addresses)]
    public JsonElement ReportTo { get; init; }

    [JsonPropertyName("reply_to")]
    [Expects(JsonShape.Addresses)]
    public JsonElement ReplyTo { get; init; }

    [JsonPropertyName("sms_max_parts")]
    [Expects(JsonShape.Number)]
    public JsonElement SmsMaxParts { get; init; }

    [JsonPropertyName("sms_header")]
    [Expects(JsonShape.String)]
    public JsonElement SmsHeader { get; init; }

    [JsonPropertyName("sms_flash")]
    [Expects(JsonShape.Boolean)]
    public JsonElement SmsFlash { get; init; }

    [JsonPropertyName("sms_validity_time")]
    [Expects(JsonShape.Number)]
    public JsonElement SmsValidityTime { get; init; }

    [JsonPropertyName("sms_originator_text")]
    [Expects(JsonShape.String)]
    public JsonElement SmsOriginatorText { get; init; }

    [JsonPropertyName("sms_originator_number")]
    [Expects(JsonShape.String)]
    public JsonElement SmsOriginatorNumber { get; init; }

    [JsonPropertyName("sms_originator_source")]
    [Expects(JsonShape.String)]
    public JsonElement SmsOriginatorSource { get; init; }

    [JsonPropertyName("sms_single_thread")]
    [Expects(JsonShape.Boolean)]
    public JsonElement SmsSingleThread { get; init; }

    [JsonPropertyName("sms_format")]
    [Expects(JsonShape.String)]
    public JsonElement SmsFormat { get; init; }

    [JsonPropertyName("sms_character_set")]
    [Expects(JsonShape.String)]
    public JsonElement SmsCharacterSet { get; init; }

    [JsonPropertyName("sms_udh")]
    [Expects(JsonShape.String)]
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

internal enum JsonShape
{
    String,
    Number,
    Boolean,
    List,
    Addresses,
}

[AttributeUsage(AttributeTargets.Property)]
internal sealed class ExpectsAttribute(JsonShape shape) : Attribute
{
    public JsonShape Shape { get; } = shape;
}
