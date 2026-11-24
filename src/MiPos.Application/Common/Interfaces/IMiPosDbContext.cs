namespace MiPos.Application.Common.Interfaces;

public interface IMiPosDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
