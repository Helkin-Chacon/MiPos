using System.Diagnostics;

namespace MiPos.Api.Responses;

public sealed record ApiError(string Title, string Message, string TraceId)
{
    internal static ApiError FromRequest(HttpContext context, string title, string message) =>
        new(title, message, Activity.Current?.Id ?? context.TraceIdentifier);
}
