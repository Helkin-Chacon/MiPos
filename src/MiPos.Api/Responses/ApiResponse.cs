namespace MiPos.Api.Responses;

public sealed record ApiResponse<T>(T? Data, ApiError? Error);
