using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using MyApp.API.HealthChecks;
using MyApp.API.Middleware;
using MyApp.API.OpenApi;
using MyApp.Application;
using MyApp.Infrastructure;
using MyApp.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger captures startup errors before host configuration.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MyApp API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId());

    builder.Services.AddControllers();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Auth endpoints: fixed window — 10 requests per minute per IP.
        options.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.PermitLimit = 10;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });

        // API endpoints: sliding window — 60 req/min per user ID or IP.
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
                "Production-ready .NET 10 Web API — Clean Architectures · CQRS · JWT Auth.";
            return Task.CompletedTask;
        });
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>(name: "database", tags: ["ready"]);

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

    // Log every HTTP request/response (method, path, status, elapsed).
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null) diagnosticContext.Set("UserId", userId);
        };
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();

    // Liveness — is the process running?
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    }).AllowAnonymous().DisableRateLimiting();

    // Readiness — DB reachable?
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteDetailedJson
    }).AllowAnonymous().DisableRateLimiting();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
