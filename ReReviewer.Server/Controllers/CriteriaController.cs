using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Читать может кто угодно, менять — администратор или модератор.
[Authorize(Roles = RoleNames.Staff)]
public class CriteriaController(ICriterionAppService criterionAppService) : BaseController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<CriterionViewModel>>> GetAll() =>
        await criterionAppService.GetCriteriaAsync();

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CriterionViewModel>> Get(int id) =>
        await criterionAppService.GetCriterionAsync(id);

    [HttpPost]
    [ProducesResponseType<CriterionViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CriterionViewModel>> Create(CriterionViewModel criterion)
    {
        criterion.Id = null;
        await criterionAppService.SaveCriterionAsync(criterion);

        return CreatedAtAction(nameof(Get), new { id = criterion.Id }, await criterionAppService.GetCriterionAsync(criterion.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CriterionViewModel>> Update(int id, CriterionViewModel criterion)
    {
        criterion.Id = id;
        await criterionAppService.SaveCriterionAsync(criterion);

        return await criterionAppService.GetCriterionAsync(id);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await criterionAppService.DeleteCriterionAsync(id);

        return NoContent();
    }
}