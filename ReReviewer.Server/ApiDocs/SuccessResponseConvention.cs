using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ReReviewer.ApiDocs;

// ApiExplorer выводит ответ 200 из ActionResult<T>, только если у метода нет ни одного [ProducesResponseType].
// Ответы с ошибками объявлены на BaseController, поэтому без этой конвенции у GET-методов в документе
// не было схемы успешного ответа. [ProducesResponseType(200)] без типа берёт тип из возвращаемого значения.
public class SuccessResponseConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        var declared = action.Filters.Concat(action.Controller.Filters).OfType<IApiResponseMetadataProvider>();
        if (!declared.Any(response => response.StatusCode is >= 200 and < 300))
            action.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status200OK));
    }
}
