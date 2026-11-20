using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.Interfaces;

public interface IReviewerSuggestionService : IBaseService<ReviewerSuggestion, Guid>
{
    public Task<ReviewerSuggestion> CreateAsync(Guid userId, string fullName, string? note, bool isSelf,
        IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ReviewerSuggestion>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<ReviewerSuggestion>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default);

    public Task<ReviewerSuggestion> ApproveAsync(Guid suggestionId, CancellationToken cancellationToken = default);
    public Task<ReviewerSuggestion> RejectAsync(Guid suggestionId, CancellationToken cancellationToken = default);
}
