using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Auth.Commands.RevokeToken;
using MyApp.Application.Tests.Common;
using DomainEntities = MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Auth;

public class RevokeTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RevokeTokenCommandHandler CreateHandler() =>
        new(_refreshTokenRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithActiveToken_RevokesAndSaves()
    {
        var token = MockHelpers.CreateRefreshToken();
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("refresh-token", default)).ReturnsAsync(token);

        await CreateHandler().Handle(new RevokeTokenCommand("refresh-token"), default);

        token.IsRevoked.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsUnauthorized()
    {
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("bad-token", default))
            .ReturnsAsync((DomainEntities.RefreshToken?)null);

        var act = async () => await CreateHandler().Handle(new RevokeTokenCommand("bad-token"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedToken_ThrowsUnauthorized()
    {
        var revoked = MockHelpers.CreateRevokedRefreshToken();
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("revoked-token", default)).ReturnsAsync(revoked);

        var act = async () => await CreateHandler().Handle(new RevokeTokenCommand("revoked-token"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
