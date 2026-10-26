using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Списки материалов — в api/reviewers/{id}/materials и api/games/{id}/materials.
// Читать может кто угодно, менять — администратор или модератор.
[Authorize(Roles = RoleNames.Staff)]
public class MaterialsController(IMaterialAppService materialAppService) : BaseController
{
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaterialViewModel>> Get(Guid id) =>
        await materialAppService.GetMaterialAsync(id);

    [HttpPost]
    [ProducesResponseType<MaterialViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MaterialViewModel>> Create(MaterialViewModel material)
    {
        material.Id = null;
        await materialAppService.SaveMaterialAsync(material);

        return CreatedAtAction(nameof(Get), new { id = material.Id }, await materialAppService.GetMaterialAsync(material.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется. Обзорщик у материала не меняется.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MaterialViewModel>> Update(Guid id, MaterialViewModel material)
    {
        material.Id = id;
        await materialAppService.SaveMaterialAsync(material);

        return await materialAppService.GetMaterialAsync(id);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await materialAppService.DeleteMaterialAsync(id);

        return NoContent();
    }
}