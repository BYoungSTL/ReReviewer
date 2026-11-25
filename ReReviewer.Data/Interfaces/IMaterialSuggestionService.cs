using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.Interfaces;

public interface IMaterialSuggestionService : IBaseService<MaterialSuggestion, Guid>
{
    // Текст уже проверен и обрезан в Application; здесь — то, для чего нужна база: обзорщик, игра и свободная ссылка.
    public Task<MaterialSuggestion> CreateAsync(Guid userId, Guid reviewerId, Guid gameId, string title, string url,
        MaterialFormat format, DateOnly? publishedAt, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<MaterialSuggestion>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<MaterialSuggestion>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default);

    // Создаёт материал обзорщика из заявки.
    public Task<MaterialSuggestion> ApproveAsync(Guid suggestionId, CancellationToken cancellationToken = default);
    public Task<MaterialSuggestion> RejectAsync(Guid suggestionId, CancellationToken cancellationToken = default);
}
