namespace MiPos.Application.Common.Interfaces;

public interface IRequestContext
{
    Guid UserId { get; }
    Guid BusinessId { get; }
    string RoleCode { get; }
    IReadOnlyList<string> Permissions { get; }
}
