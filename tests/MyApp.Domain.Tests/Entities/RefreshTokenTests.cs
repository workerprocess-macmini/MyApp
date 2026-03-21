using FluentAssertions;
using MyApp.Domain.Entities;

namespace MyApp.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsActiveToken()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var token = RefreshToken.Create(userId, "token-value", expiresAt);

        token.UserId.Should().Be(userId);
        token.Token.Should().Be("token-value");
        token.ExpiresAt.Should().Be(expiresAt);
        token.IsRevoked.Should().BeFalse();
        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(-1));

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsIsRevokedTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenRevokedAndNotExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        token.IsActive.Should().BeFalse();
    }
}
