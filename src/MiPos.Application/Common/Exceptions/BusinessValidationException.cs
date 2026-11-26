namespace MiPos.Application.Common.Exceptions;

public class BusinessValidationException(string title, string message) : Exception(message)
{
    public string Title { get; } = title;
}
