namespace MiPos.Application.Common.Exceptions;

public class ConflictException(string title, string message) : Exception(message)
{
    public string Title { get; } = title;
}
