using MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Common;

internal static class MockHelpers
{
    public static User CreateUser(
        string email = "test@test.com",
        string passwordHash = "hashed",
        string firstName = "Test",
        string lastName = "User")
        => User.Create(email, passwordHash, firstName, lastName);

    public static Product CreateProduct(
        string name = "Test Product",
        string description = "A description",
        decimal price = 99.99m)
        => Product.Create(name, description, price);

    public static RefreshToken CreateRefreshToken(
        Guid? userId = null,
        string token = "refresh-token",
        DateTime? expiresAt = null)
        => RefreshToken.Create(
            userId ?? Guid.NewGuid(),
            token,
            expiresAt ?? DateTime.UtcNow.AddDays(7));

    public static RefreshToken CreateExpiredRefreshToken(Guid? userId = null)
        => RefreshToken.Create(userId ?? Guid.NewGuid(), "expired-token", DateTime.UtcNow.AddDays(-1));

    public static RefreshToken CreateRevokedRefreshToken(Guid? userId = null)
    {
        var token = CreateRefreshToken(userId);
        token.Revoke();
        return token;
    }
}
