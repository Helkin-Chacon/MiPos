using System.Diagnostics;

namespace MiPos.Api.Responses;

public class ApiError
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;

    internal static ApiError FromRequest(HttpContext context, string title, string message)
    {
        return new ApiError
        {
            Title = title,
            Message = message,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };
    }
}
