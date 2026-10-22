using ReReviewer.Data.Entities.BaseEntity;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Application.Services.Base;

// Общие шаги AppService'ов поверх CRUD-сервисов слоя Data; к базе напрямую не обращаются.
internal static class AppServiceExtensions
{
    public static async Task<TEntity> GetRequiredAsync<TEntity, TKey>(this IBaseService<TEntity, TKey> service, TKey id)
        where TEntity : Entity<TKey> =>
        await service.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"{typeof(TEntity).Name} not found.");

    public static async Task DeleteRequiredAsync<TEntity, TKey>(this IBaseService<TEntity, TKey> service, TKey id)
        where TEntity : Entity<TKey>
    {
        if (!await service.DeleteAsync(id))
            throw new KeyNotFoundException($"{typeof(TEntity).Name} not found.");
    }

    // id = null — создаёт новую запись, иначе загружает существующую. В обоих случаях применяет apply и сохраняет.
    public static async Task<TEntity> SaveAsync<TEntity, TKey>(this IBaseService<TEntity, TKey> service, TKey? id, Action<TEntity> apply)
        where TEntity : Entity<TKey>, new()
        where TKey : struct
    {
        if (id is not { } existingId)
        {
            var created = new TEntity();
            apply(created);

            return await service.AddAsync(created);
        }

        var entity = await service.GetRequiredAsync(existingId);
        apply(entity);
        await service.UpdateAsync(entity);

        return entity;
    }

    // Приводит коллекцию связей к нужному набору: лишнее удаляет, недостающее добавляет.
    // Обе коллекции из одного DbContext, поэтому одна и та же запись — один и тот же объект.
    public static void SyncWith<T>(this ICollection<T> current, IReadOnlyCollection<T> desired)
    {
        foreach (var removed in current.Except(desired).ToList())
            current.Remove(removed);

        foreach (var added in desired.Except(current).ToList())
            current.Add(added);
    }
}