using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

[Collection(DatabaseCollection.Name)]
public class BadgeNominationServiceTests(DatabaseFixture database)
{
    [Fact]
    public async Task NominateAsync_Twice_ThrowsValidation_AndWithdrawLetsNominateAgain()
    {
        var (user, reviewer, badge) = await ArrangeAsync();
        await using var db = database.CreateContext();
        var service = new BadgeNominationService(db);
        await service.NominateAsync(user, reviewer, badge);

        var exception = await Should.ThrowAsync<ValidationException>(() => service.NominateAsync(user, reviewer, badge));
        exception.Message.ShouldBe("You have already nominated this reviewer for this badge.");
        (await service.GetUserBadgeIdsAsync(user, reviewer)).ShouldBe([badge]);

        await service.WithdrawAsync(user, reviewer, badge);
        (await service.GetUserBadgeIdsAsync(user, reviewer)).ShouldBeEmpty();
        await service.NominateAsync(user, reviewer, badge);
    }

    [Fact]
    public async Task NominateAsync_OwnProfile_ThrowsValidation()
    {
        await using var db = database.CreateContext();
        var user = await TestData.UserAsync(db);
        var reviewer = await TestData.ReviewerAsync(db, user.Id);
        var badge = await TestData.BadgeAsync(db);

        var exception = await Should.ThrowAsync<ValidationException>(() => new BadgeNominationService(db).NominateAsync(user.Id, reviewer.Id, badge.Id));

        exception.Message.ShouldBe("You cannot nominate your own reviewer profile.");
    }

    [Fact]
    public async Task GetQueueAsync_CountsNominationsPerReviewerAndBadge()
    {
        var (first, reviewer, badge) = await ArrangeAsync();
        await using var db = database.CreateContext();
        var second = (await TestData.UserAsync(db)).Id;
        var otherBadge = (await TestData.BadgeAsync(db)).Id;
        var service = new BadgeNominationService(db);
        await service.NominateAsync(first, reviewer, badge);
        await service.NominateAsync(second, reviewer, badge);
        await service.NominateAsync(first, reviewer, otherBadge);

        await using var check = database.CreateContext();
        var queue = (await new BadgeNominationService(check).GetQueueAsync(0, 100)).ToList();

        var popular = queue.Single(q => q.ReviewerId == reviewer && q.BadgeId == badge);
        popular.NominationCount.ShouldBe(2);
        popular.FirstNominatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        queue.Single(q => q.ReviewerId == reviewer && q.BadgeId == otherBadge).NominationCount.ShouldBe(1);
        // Больше номинаций — выше в очереди.
        queue.IndexOf(popular).ShouldBeLessThan(queue.FindIndex(q => q.ReviewerId == reviewer && q.BadgeId == otherBadge));
    }

    [Fact]
    public async Task ApproveAsync_AwardsBadge_ClearsNominations_AndBlocksNewOnes()
    {
        var (user, reviewer, badge) = await ArrangeAsync();
        await using (var db = database.CreateContext())
        {
            var service = new BadgeNominationService(db);
            await service.NominateAsync(user, reviewer, badge);
            await service.ApproveAsync(reviewer, badge);
        }

        await using var check = database.CreateContext();
        (await check.ReviewerBadges.AnyAsync(a => a.ReviewerId == reviewer && a.BadgeId == badge)).ShouldBeTrue();
        (await check.BadgeNominations.AnyAsync(n => n.ReviewerId == reviewer && n.BadgeId == badge)).ShouldBeFalse();
        var exception = await Should.ThrowAsync<ValidationException>(() => new BadgeNominationService(check).NominateAsync(user, reviewer, badge));
        exception.Message.ShouldBe("This reviewer already has this badge.");
    }

    [Fact]
    public async Task RejectAsync_ClearsNominations_WithoutAwarding()
    {
        var (user, reviewer, badge) = await ArrangeAsync();
        await using (var db = database.CreateContext())
        {
            var service = new BadgeNominationService(db);
            await service.NominateAsync(user, reviewer, badge);
            await service.RejectAsync(reviewer, badge);
        }

        await using var check = database.CreateContext();
        (await check.ReviewerBadges.AnyAsync(a => a.ReviewerId == reviewer && a.BadgeId == badge)).ShouldBeFalse();
        (await check.BadgeNominations.AnyAsync(n => n.ReviewerId == reviewer && n.BadgeId == badge)).ShouldBeFalse();
        await Should.ThrowAsync<KeyNotFoundException>(() => new BadgeNominationService(check).RejectAsync(reviewer, badge));
    }

    private async Task<(Guid User, Guid Reviewer, int Badge)> ArrangeAsync()
    {
        await using var db = database.CreateContext();

        return ((await TestData.UserAsync(db)).Id, (await TestData.ReviewerAsync(db)).Id, (await TestData.BadgeAsync(db)).Id);
    }
}
