using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ReReviewer.ApiDocs;

// Замок и требуемые роли — только у эндпоинтов, которым нужен вход: [Authorize] на контроллере
// или методе и нет [AllowAnonymous]. Открытые эндпоинты остаются без замка.
public class AuthorizationOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var authorize = metadata.OfType<IAuthorizeData>().ToList();
        if (authorize.Count == 0 || metadata.OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(ApiDocumentTransformer.BearerSchemeName, context.Document)] = []
        });

        // Несколько [Authorize] с ролями (на контроллере и на методе) должны выполняться одновременно.
        var roles = authorize
            .Where(a => !string.IsNullOrEmpty(a.Roles))
            .Select(a => a.Roles!.Replace(",", " or "))
            .ToList();
        if (roles.Count > 0)
            operation.Description = $"{operation.Description} Requires role: {string.Join("; ", roles)}.".Trim();

        return Task.CompletedTask;
    }
}