using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyApp.Infrastructure.Persistence;

namespace MyApp.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "integration-test-secret-key-minimum-32-chars!!",
                ["JwtSettings:Issuer"]    = "TestIssuer",
                ["JwtSettings:Audience"]  = "TestAudience",
                ["JwtSettings:AccessTokenExpiryMinutes"] = "15",
                ["JwtSettings:RefreshTokenExpiryDays"]   = "7",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations to avoid the
            // "multiple providers registered" error from EF's internal service provider.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Re-register AppDbContext using a direct factory that owns its own
            // fresh DbContextOptions — completely isolated from the SQL Server config.
            var dbPath = _dbPath;
            services.AddScoped<AppDbContext>(_ =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    // SQL Server-specific column types (e.g. decimal(18,2)) cause a
                    // false-positive "pending changes" warning when running against SQLite.
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                    .Options;
                return new AppDbContext(options);
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
