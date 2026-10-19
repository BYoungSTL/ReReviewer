using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Interfaces.Base;

public interface IBaseService<TEntity, TKey> where TEntity : Entity<TKey>
{
    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}