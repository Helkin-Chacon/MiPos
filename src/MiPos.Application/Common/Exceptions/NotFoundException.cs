namespace MiPos.Application.Common.Exceptions;

public class NotFoundException(string title, string message) : Exception(message)
{
    public string Title { get; } = title;
}
