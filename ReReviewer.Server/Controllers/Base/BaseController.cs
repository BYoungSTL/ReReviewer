using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Security;

namespace ReReviewer.Controllers.Base;

// Основа всех API-контроллеров: маршрут api/<имя контроллера без "Controller">,
// автоматический ответ 400 при невалидной модели ([ApiController]) и описание ошибок для Swagger.
// Исключения из сервисов здесь не ловятся — их переводит в ответы ExceptionHandlingMiddleware.
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public abstract class BaseController : ControllerBase
{
    // Id пользователя из JWT (claim sub / NameIdentifier). Брать автора отзыва и т. п. только отсюда,
    // а не из тела запроса. Без валидного Id — AuthenticationException, middleware ответит 401.
    protected Guid CurrentUserId =>
        CurrentUserIdOrNull ?? throw new AuthenticationException("User is not authenticated.");

    // Для методов, открытых без входа: null, если запрос пришёл без токена.
    protected Guid? CurrentUserIdOrNull =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)
            ? userId
            : null;

    // Администратор или модератор.
    protected bool IsStaff =>
        User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Moderator);
}