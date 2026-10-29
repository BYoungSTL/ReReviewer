using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ReReviewer.ApiDocs;

// Заголовок документа и схема Bearer: в Scalar появляется поле для JWT из POST /api/auth/login.
public class ApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public const string BearerSchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "ReReviewer API",
            Version = "v1",
            Description = "Reviews of game reviewers. Sign in with POST /api/auth/login and send the token as Bearer."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT from POST /api/auth/login."
        };

        return Task.CompletedTask;
    }
}