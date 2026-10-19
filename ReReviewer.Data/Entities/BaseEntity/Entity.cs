namespace ReReviewer.Data.Entities.BaseEntity;

public class Entity<T>
{
    public T Id { get; set; } = default!;
}