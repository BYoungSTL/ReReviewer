using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers;

namespace ReReviewer.UnitTests.Server;

public class ReviewsControllerTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();
    private static readonly Guid ReviewId = Guid.NewGuid();

    private readonly IReviewAppService _reviews = Substitute.For<IReviewAppService>();

    #region Get: кто видит неопубликованный отзыв

    [Fact]
    public async Task Get_Published_IsVisibleToAnonymous()
    {
        ReviewWithStatus("Published");

        var result = await Controller(Anonymous()).Get(ReviewId);

        result.Value!.Id.ShouldBe(ReviewId);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Rejected")]
    public async Task Get_Unpublished_IsHiddenFromAnonymous(string status)
    {
        ReviewWithStatus(status);

        await Should.ThrowAsync<KeyNotFoundException>(() => Controller(Anonymous()).Get(ReviewId));
    }

    [Fact]
    public async Task Get_Unpublished_IsHiddenFromAnotherUser()
    {
        ReviewWithStatus("Pending");

        await Should.ThrowAsync<KeyNotFoundException>(() => Controller(SignedIn(Guid.NewGuid())).Get(ReviewId));
    }

    [Fact]
    public async Task Get_Unpublished_IsVisibleToAuthor()
    {
        ReviewWithStatus("Pending");

        var result = await Controller(SignedIn(AuthorId)).Get(ReviewId);

        result.Value!.Id.ShouldBe(ReviewId);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    public async Task Get_Unpublished_IsVisibleToStaff(string role)
    {
        ReviewWithStatus("Rejected");

        var result = await Controller(SignedIn(Guid.NewGuid(), role)).Get(ReviewId);

        result.Value!.Id.ShouldBe(ReviewId);
    }

    #endregion

    #region Create / Update: автор берётся из токена

    [Fact]
    public async Task Create_TakesAuthorFromToken_AndIgnoresIdFromBody()
    {
        var savedId = Guid.NewGuid();
        _reviews.SaveReviewAsync(Arg.Any<ReviewViewModel>()).Returns(ci =>
        {
            ci.Arg<ReviewViewModel>().Id = savedId;
            return true;
        });
        _reviews.GetReviewAsync(savedId).Returns(new ReviewViewModel { Id = savedId, UserId = AuthorId });

        var result = await Controller(SignedIn(AuthorId)).Create(new ReviewViewModel { Id = Guid.NewGuid(), UserId = Guid.NewGuid() });

        result.Result.ShouldBeOfType<CreatedAtActionResult>();
        await _reviews.Received(1).SaveReviewAsync(Arg.Is<ReviewViewModel>(r => r.UserId == AuthorId && r.Id == savedId));
    }

    [Fact]
    public async Task Update_TakesIdFromRoute_AndAuthorFromToken()
    {
        _reviews.GetReviewAsync(ReviewId).Returns(new ReviewViewModel { Id = ReviewId, UserId = AuthorId });

        await Controller(SignedIn(AuthorId)).Update(ReviewId, new ReviewViewModel { Id = Guid.NewGuid(), UserId = Guid.NewGuid() });

        await _reviews.Received(1).SaveReviewAsync(Arg.Is<ReviewViewModel>(r => r.Id == ReviewId && r.UserId == AuthorId));
    }

    #endregion

    private void ReviewWithStatus(string status) =>
        _reviews.GetReviewAsync(ReviewId).Returns(new ReviewViewModel { Id = ReviewId, UserId = AuthorId, Status = status });

    private ReviewsController Controller(ClaimsPrincipal user) =>
        new(_reviews) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } } };

    private static ClaimsPrincipal Anonymous() =>
        new(new ClaimsIdentity());

    // Так же, как в настоящем токене: Id в "sub", роли в "role".
    private static ClaimsPrincipal SignedIn(Guid userId, params string[] roles) =>
        new(new ClaimsIdentity(
            [new Claim("sub", userId.ToString()), .. roles.Select(role => new Claim("role", role))],
            authenticationType: "Bearer",
            nameType: "name",
            roleType: "role"));
}