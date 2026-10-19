using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.Entities.BaseEntity;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.CRUD.Base;

public class BaseService<TEntity, TKey>(ReReviewerDbContext context) : IBaseService<TEntity, TKey>
    where TEntity : Entity<TKey>
{
    protected readonly ReReviewerDbContext Context = context;

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await Set.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Set.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Set.Add(entity);
        await Context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // Изменения отслеживаемой сущности трекер EF найдёт сам; Update() нужен только для отсоединённой,
        // например собранной из данных запроса, иначе он пометил бы изменёнными все столбцы.
        if (Context.Entry(entity).State == EntityState.Detached)
            Set.Update(entity);

        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FindAsync([id], cancellationToken);
        if (entity is null)
            return false;

        Set.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);

        return true;
    }
}