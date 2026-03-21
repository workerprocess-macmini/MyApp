using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using MyApp.API.Middleware;
using MyApp.API.OpenApi;
using MyApp.Application;
using MyApp.Infrastructure;
using MyApp.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth endpoints: fixed window — 10 requests per minute per IP.
    // Prevents credential-stuffing and brute-force attacks.
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    // Authenticated API endpoints: sliding window — 60 requests per minute.
    // Partitioned per user ID when authenticated, otherwise per IP.
    options.AddPolicy("api", context =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var key = userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ =>
            new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 60,
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
    });
});

// Register as singleton so the two transformer callbacks share state.
builder.Services.AddSingleton<BearerSecuritySchemeTransformer>();

builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "MyApp API";
        document.Info.Version = "v1";
        document.Info.Description =
            "Production-ready .NET 10 Web API — Clean Architecture · CQRS · JWT Auth.";
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddHttpAuthentication("Bearer", _ => { });
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();

public partial class Program { }
