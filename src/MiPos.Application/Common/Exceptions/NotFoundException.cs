namespace MiPos.Application.Common.Exceptions;

public sealed class NotFoundException(string title, string message) : Exception(message)
{
    public string Title { get; } = title;
}
