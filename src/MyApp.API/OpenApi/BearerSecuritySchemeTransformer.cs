using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyApp.API.OpenApi;

/// <summary>
/// Adds the JWT Bearer security scheme to the OpenAPI document and marks every
/// operation that carries <see cref="AuthorizeAttribute"/> with a security requirement.
/// Register as a singleton so both transformer callbacks share state.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    // Populated by the operation transformer; consumed by the document transformer.
    private readonly ConcurrentDictionary<string, bool> _securedOperationIds = new();

    // ── Operation transformer ────────────────────────────────────────────────

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        bool requiresAuth = metadata.OfType<IAuthorizeData>().Any()
                         && !metadata.OfType<IAllowAnonymous>().Any();

        if (requiresAuth)
        {
            var operationId = operation.OperationId
                              ?? context.Description.ActionDescriptor.Id;
            _securedOperationIds.TryAdd(operationId, true);
        }

        return Task.CompletedTask;
    }

    // ── Document transformer ─────────────────────────────────────────────────

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Add the Bearer security scheme definition.
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter the JWT access token obtained from `/api/auth/login`."
        };

        // Add the security requirement to every secured operation.
        if (_securedOperationIds.IsEmpty) return Task.CompletedTask;

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        };

        foreach (var path in document.Paths.Values)
        {
            foreach (var operation in (path.Operations ?? []).Values)
            {
                var id = operation.OperationId;
                if (id is not null && _securedOperationIds.ContainsKey(id))
                {
                    operation.Security ??= [];
                    operation.Security.Add(requirement);
                }
            }
        }

        return Task.CompletedTask;
    }
}
