using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Отзывы пишут и правят вошедшие пользователи, автор всегда берётся из токена.
// Опубликованные отзывы обзорщика — в api/reviewers/{id}/reviews.
[Authorize]
public class ReviewsController(IReviewAppService reviewAppService) : BaseController
{
    #region Reviews

    // Неопубликованный отзыв видят только автор и модераторы, для остальных его как будто нет.
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReviewViewModel>> Get(Guid id)
    {
        var review = await reviewAppService.GetReviewAsync(id);
        if (review.Status != nameof(ReviewStatus.Published) && review.UserId != CurrentUserIdOrNull && !IsStaff)
            throw new KeyNotFoundException("Review not found.");

        return review;
    }

    // Свои отзывы в любом статусе.
    [HttpGet("my")]
    public async Task<ActionResult<List<ReviewViewModel>>> GetMy() =>
        await reviewAppService.GetUserReviewsAsync(CurrentUserId);

    [HttpPost]
    [ProducesResponseType<ReviewViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewViewModel>> Create(ReviewViewModel review)
    {
        review.Id = null;
        review.UserId = CurrentUserId;
        await reviewAppService.SaveReviewAsync(review);

        return CreatedAtAction(nameof(Get), new { id = review.Id }, await reviewAppService.GetReviewAsync(review.Id!.Value));
    }

    // Править может только автор; после правки отзыв снова уходит на модерацию.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReviewViewModel>> Update(Guid id, ReviewViewModel review)
    {
        review.Id = id;
        review.UserId = CurrentUserId;
        await reviewAppService.SaveReviewAsync(review);

        return await reviewAppService.GetReviewAsync(id);
    }

    // Удалить может только автор.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await reviewAppService.DeleteReviewAsync(id, CurrentUserId);

        return NoContent();
    }

    #endregion

    #region Moderation

    // Очередь модерации: первым — отзыв, который ждёт дольше всех.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpGet("moderation")]
    public async Task<ActionResult<PagedViewModel<ReviewViewModel>>> GetModerationQueue(int page = 1, int pageSize = 20) =>
        await reviewAppService.GetModerationQueueAsync(page, pageSize);

    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id)
    {
        await reviewAppService.ModerateReviewAsync(id, approve: true);

        return NoContent();
    }

    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id)
    {
        await reviewAppService.ModerateReviewAsync(id, approve: false);

        return NoContent();
    }

    #endregion
}