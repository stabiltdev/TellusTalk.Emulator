using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Reports;

/// <summary>
/// A delivery report as TellusTalk's K8 platform was seen posting one: its fields, their order and their values for a
/// delivered SMS and for one the operator could not route. TellusTalk publishes no format of its own.
/// </summary>
internal sealed record DeliveryReport(
    [property: JsonPropertyName("log_status")] string LogStatus,
    [property: JsonPropertyName("log_status_code")] int LogStatusCode,
    [property: JsonPropertyName("failed_reason")] string FailedReason,
    [property: JsonPropertyName("article_id")] string ArticleId,
    [property: JsonPropertyName("to_protocol")] string ToProtocol,
    [property: JsonPropertyName("to_address")] string ToAddress,
    [property: JsonPropertyName("to_name")] string? ToName,
    [property: JsonPropertyName("from_address")] string FromAddress,
    [property: JsonPropertyName("from_name")] string FromName,
    [property: JsonPropertyName("subject")] string? Subject,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("html")] string Html,
    [property: JsonPropertyName("echo")] JsonElement? Echo,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("log_uuid")] string LogUuid,
    [property: JsonPropertyName("conversation_uuid")] string ConversationUuid,
    [property: JsonPropertyName("job_uuid")] string JobUuid,
    [property: JsonPropertyName("report_time")] string ReportTime,
    [property: JsonPropertyName("failed_details")] string? FailedDetails,
    [property: JsonPropertyName("text_linebreaksbr")] string TextLineBreaksBr,
    [property: JsonPropertyName("template_optional_line")] string TemplateOptionalLine)
{
    private static readonly JsonSerializerOptions Options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.Never };

    /// <summary>
    /// <c>quantity</c> is taken as the number of parts: TellusTalk's e-mail report for the same SMS listed
    /// "SMS Parts: 1" where the JSON report had a quantity of 1.
    /// </summary>
    public static DeliveryReport For(Message message, ReportStatus status, DateTimeOffset reportedAt)
    {
        var delivered = status == ReportStatus.Delivered;
        var body = message.Request.Body;
        var html = body.Html.ValueKind == JsonValueKind.String ? body.Html.GetString()! : message.Text;

        return new DeliveryReport(
            delivered ? "txtDELIVERED" : "txtDELIVERY_FAILED",
            delivered ? 22 : 21,
            delivered ? string.Empty : "txtFAILED_REASON: txtOPERATOR_ROUTING_ERROR, None",
            "SMS_Sent",
            "SMS",
            "sms:" + message.To.Number,
            null,
            "user_id:" + message.UserName,
            string.Empty,
            message.Request.Subject,
            message.Text,
            html,
            body.Echo.ValueKind == JsonValueKind.Undefined ? null : body.Echo,
            message.Parts.Count,
            message.Id,
            message.Id,
            message.JobId,
            reportedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            null,
            message.Text.Replace("\n", "<br>", StringComparison.Ordinal),
            "--------------");
    }

    public static string Json(DeliveryReport report) => JsonSerializer.Serialize(report, Options);
}
