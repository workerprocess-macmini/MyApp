using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Auth.Commands.RefreshToken;
using MyApp.Application.Tests.Common;
using DomainEntities = MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RefreshTokenCommandHandler CreateHandler() => new(
        _refreshTokenRepo.Object,
        _userRepo.Object,
        _jwtTokenService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidToken_ReturnsNewTokens()
    {
        var user = MockHelpers.CreateUser();
        var refreshToken = MockHelpers.CreateRefreshToken(user.Id);

        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("refresh-token", default)).ReturnsAsync(refreshToken);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        _jwtTokenService.Setup(j => j.GenerateAccessToken(user)).Returns("new-access");
        _jwtTokenService.Setup(j => j.GenerateRefreshToken()).Returns(("new-refresh", DateTime.UtcNow.AddDays(7)));

        var result = await CreateHandler().Handle(new RefreshTokenCommand("refresh-token"), default);

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        refreshToken.IsRevoked.Should().BeTrue(); // old token rotated
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ThrowsUnauthorized()
    {
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("bad-token", default))
            .ReturnsAsync((DomainEntities.RefreshToken?)null);

        var act = async () => await CreateHandler().Handle(new RefreshTokenCommand("bad-token"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsUnauthorized()
    {
        var expiredToken = MockHelpers.CreateExpiredRefreshToken();
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("expired-token", default)).ReturnsAsync(expiredToken);

        var act = async () => await CreateHandler().Handle(new RefreshTokenCommand("expired-token"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired or revoked*");
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ThrowsUnauthorized()
    {
        var revokedToken = MockHelpers.CreateRevokedRefreshToken();
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("revoked-token", default)).ReturnsAsync(revokedToken);

        var act = async () => await CreateHandler().Handle(new RefreshTokenCommand("revoked-token"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired or revoked*");
    }
}
