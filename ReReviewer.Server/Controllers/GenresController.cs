using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Читать может кто угодно, менять — администратор или модератор.
[Authorize(Roles = RoleNames.Staff)]
public class GenresController(IGenreAppService genreAppService) : BaseController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<GenreViewModel>>> GetAll() =>
        await genreAppService.GetGenresAsync();

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenreViewModel>> Get(int id) =>
        await genreAppService.GetGenreAsync(id);

    [HttpPost]
    [ProducesResponseType<GenreViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GenreViewModel>> Create(GenreViewModel genre)
    {
        genre.Id = null;
        await genreAppService.SaveGenreAsync(genre);

        return CreatedAtAction(nameof(Get), new { id = genre.Id }, await genreAppService.GetGenreAsync(genre.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GenreViewModel>> Update(int id, GenreViewModel genre)
    {
        genre.Id = id;
        await genreAppService.SaveGenreAsync(genre);

        return await genreAppService.GetGenreAsync(id);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await genreAppService.DeleteGenreAsync(id);

        return NoContent();
    }
}