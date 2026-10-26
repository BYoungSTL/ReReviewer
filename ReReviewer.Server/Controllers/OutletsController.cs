using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Читать может кто угодно, менять — администратор или модератор.
[Authorize(Roles = RoleNames.Staff)]
public class OutletsController(IOutletAppService outletAppService) : BaseController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<OutletViewModel>>> GetAll() =>
        await outletAppService.GetOutletsAsync();

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OutletViewModel>> Get(Guid id) =>
        await outletAppService.GetOutletAsync(id);

    [HttpPost]
    [ProducesResponseType<OutletViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OutletViewModel>> Create(OutletViewModel outlet)
    {
        outlet.Id = null;
        await outletAppService.SaveOutletAsync(outlet);

        return CreatedAtAction(nameof(Get), new { id = outlet.Id }, await outletAppService.GetOutletAsync(outlet.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OutletViewModel>> Update(Guid id, OutletViewModel outlet)
    {
        outlet.Id = id;
        await outletAppService.SaveOutletAsync(outlet);

        return await outletAppService.GetOutletAsync(id);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await outletAppService.DeleteOutletAsync(id);

        return NoContent();
    }

    // Тело запроса — массив Id обзорщиков. Уже привязанные пропускаются.
    [HttpPost("{id:guid}/reviewers")]
    public async Task<ActionResult<OutletViewModel>> LinkReviewers(Guid id, List<Guid> reviewerIds) =>
        await outletAppService.LinkReviewersAsync(id, reviewerIds);
}