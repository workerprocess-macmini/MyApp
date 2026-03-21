using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext context, IPasswordHasher passwordHasher, ILogger<DataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedUsersAsync();
        await SeedProductsAsync();
    }

    private async Task SeedUsersAsync()
    {
        if (await _context.Users.AnyAsync()) return;

        var admin = User.Create("admin@myapp.com", _passwordHasher.Hash("Admin1234!"), "Admin", "User");
        admin.SetRole("Admin");

        var users = new[]
        {
            admin,
            User.Create("john@myapp.com", _passwordHasher.Hash("User1234!"), "John", "Doe"),
            User.Create("jane@myapp.com", _passwordHasher.Hash("User1234!"), "Jane", "Smith"),
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} users.", users.Length);
    }

    private async Task SeedProductsAsync()
    {
        if (await _context.Products.AnyAsync()) return;

        var products = new[]
        {
            Product.Create("Laptop Pro 15",      "High-performance laptop with 15-inch display",            1299.99m),
            Product.Create("Wireless Mouse",     "Ergonomic wireless mouse with long battery life",           29.99m),
            Product.Create("Mechanical Keyboard","Compact mechanical keyboard with RGB backlighting",         89.99m),
            Product.Create("4K Monitor",         "27-inch 4K UHD monitor with HDR support",                 499.99m),
            Product.Create("USB-C Hub",          "7-in-1 USB-C hub with HDMI, USB-A, and SD card reader",    49.99m),
        };

        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} products.", products.Length);
    }
}
