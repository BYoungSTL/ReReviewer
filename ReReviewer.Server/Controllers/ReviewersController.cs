using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Страница обзорщика (профиль, материалы, опубликованные отзывы, рейтинг) открыта всем,
// менять профиль и привязывать материалы — администратор или модератор.
[Authorize(Roles = RoleNames.Staff)]
public class ReviewersController(
    IReviewerAppService reviewerAppService,
    IReviewAppService reviewAppService,
    IMaterialAppService materialAppService) : BaseController
{
    #region Profile

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<ReviewerViewModel>>> GetAll() =>
        await reviewerAppService.GetReviewersAsync();

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReviewerViewModel>> Get(Guid id) =>
        await reviewerAppService.GetReviewerAsync(id);

    [HttpPost]
    [ProducesResponseType<ReviewerViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewerViewModel>> Create(ReviewerViewModel reviewer)
    {
        reviewer.Id = null;
        await reviewerAppService.SaveReviewerAsync(reviewer);

        return CreatedAtAction(nameof(Get), new { id = reviewer.Id }, await reviewerAppService.GetReviewerAsync(reviewer.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReviewerViewModel>> Update(Guid id, ReviewerViewModel reviewer)
    {
        reviewer.Id = id;
        await reviewerAppService.SaveReviewerAsync(reviewer);

        return await reviewerAppService.GetReviewerAsync(id);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await reviewerAppService.DeleteReviewerAsync(id);

        return NoContent();
    }

    // Подтверждает, что пользователь userId — этот обзорщик, и привязывает профиль к его аккаунту.
    [HttpPost("{id:guid}/validate/{userId:guid}")]
    public async Task<ActionResult<ReviewerViewModel>> Validate(Guid id, Guid userId) =>
        await reviewerAppService.ValidateReviewerAsync(id, userId);

    #endregion

    #region Materials

    [AllowAnonymous]
    [HttpGet("{id:guid}/materials")]
    public async Task<ActionResult<List<MaterialViewModel>>> GetMaterials(Guid id) =>
        await materialAppService.GetReviewerMaterialsAsync(id);

    // Материалы без Id создаются, с Id — уже его материалы, пропускаются. Возвращает все материалы обзорщика.
    [HttpPost("{id:guid}/materials")]
    public async Task<ActionResult<List<MaterialViewModel>>> LinkMaterials(Guid id, List<MaterialViewModel> materials) =>
        await materialAppService.LinkMaterialsAsync(id, materials);

    #endregion

    #region Reviews

    // Только опубликованные отзывы, новые сверху.
    [AllowAnonymous]
    [HttpGet("{id:guid}/reviews")]
    public async Task<ActionResult<PagedViewModel<ReviewViewModel>>> GetReviews(Guid id, int page = 1, int pageSize = 20) =>
        await reviewAppService.GetReviewerReviewsAsync(id, page, pageSize);

    [AllowAnonymous]
    [HttpGet("{id:guid}/rating")]
    public async Task<ActionResult<ReviewerRatingViewModel>> GetRating(Guid id) =>
        await reviewAppService.GetReviewerRatingAsync(id);

    #endregion
}