using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal static class FailureNumber
{
    /// <summary>
    /// The number in a send failure route, refused unless it is written as a caught message's <c>to</c> is: one written
    /// another way never matches a recipient, and its failures would go unspent without a word.
    /// </summary>
    public static BadRequest<ApiError>? Refusal(string? number) =>
        number is not null && !PhoneNumbers.IsPlusDigits(number)
            ? TypedResults.BadRequest(new ApiError("The number must be + followed by digits."))
            : null;
}
