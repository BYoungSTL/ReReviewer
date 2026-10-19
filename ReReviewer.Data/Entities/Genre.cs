using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class Genre : Entity<int>
{
    public string Title { get; set; } = string.Empty;

    #region Virtual Properties

    public virtual ICollection<Reviewer> Reviewers { get; set; } = [];
    public virtual ICollection<Game> Games { get; set; } = [];
    public virtual ICollection<Criterion> Criteria { get; set; } = [];

    #endregion
}