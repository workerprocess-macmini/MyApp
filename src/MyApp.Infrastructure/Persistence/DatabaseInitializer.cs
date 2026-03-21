using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyApp.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // SQLite is used only in integration tests — EnsureCreated is faster
            // and avoids SQL Server-specific migration syntax (e.g. nvarchar(max)).
            if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                logger.LogInformation("SQLite detected — using EnsureCreated.");
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                logger.LogInformation("Applying pending migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }

            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Database seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
