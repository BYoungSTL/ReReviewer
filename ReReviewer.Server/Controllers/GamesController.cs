using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Читать может кто угодно, менять — администратор или модератор.
[Authorize(Roles = RoleNames.Staff)]
public class GamesController(IGameAppService gameAppService, IMaterialAppService materialAppService) : BaseController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<GameViewModel>>> GetAll() =>
        await gameAppService.GetGamesAsync();

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GameViewModel>> Get(Guid id) =>
        await gameAppService.GetGameAsync(id);

    // Обзоры этой игры от всех обзорщиков.
    [AllowAnonymous]
    [HttpGet("{id:guid}/materials")]
    public async Task<ActionResult<List<MaterialViewModel>>> GetMaterials(Guid id) =>
        await materialAppService.GetGameMaterialsAsync(id);

    [HttpPost]
    [ProducesResponseType<GameViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GameViewModel>> Create(GameViewModel game)
    {
        game.Id = null;
        await gameAppService.SaveGameAsync(game);

        return CreatedAtAction(nameof(Get), new { id = game.Id }, await gameAppService.GetGameAsync(game.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GameViewModel>> Update(Guid id, GameViewModel game)
    {
        game.Id = id;
        await gameAppService.SaveGameAsync(game);

        return await gameAppService.GetGameAsync(id);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await gameAppService.DeleteGameAsync(id);

        return NoContent();
    }
}