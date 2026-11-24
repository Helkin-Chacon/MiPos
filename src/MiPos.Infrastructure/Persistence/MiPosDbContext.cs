using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Interfaces;

namespace MiPos.Infrastructure.Persistence;

public sealed class MiPosDbContext(DbContextOptions<MiPosDbContext> options)
    : DbContext(options), IMiPosDbContext;
