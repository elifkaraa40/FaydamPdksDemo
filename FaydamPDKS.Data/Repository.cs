using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data;

public class Repository<TEntity>(AppDbContext context) : IRepository<TEntity> where TEntity : class
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<TEntity> Set { get; } = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public void Remove(TEntity entity) => Set.Remove(entity);
}
