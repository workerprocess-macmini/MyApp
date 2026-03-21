using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Auth.Commands.Login;
using MyApp.Application.Tests.Common;
using MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LoginCommandHandler CreateHandler() => new(
        _userRepo.Object,
        _refreshTokenRepo.Object,
        _passwordHasher.Object,
        _jwtTokenService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokens()
    {
        var user = MockHelpers.CreateUser("john@test.com", "hashed-pw");
        var command = new LoginCommand("john@test.com", "Password1!");

        _userRepo.Setup(r => r.GetByEmailAsync(command.Email, default)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(command.Password, user.PasswordHash)).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateAccessToken(user)).Returns("access-token");
        _jwtTokenService.Setup(j => j.GenerateRefreshToken()).Returns(("refresh-token", DateTime.UtcNow.AddDays(7)));

        var result = await CreateHandler().Handle(command, default);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var act = async () => await CreateHandler().Handle(new LoginCommand("unknown@test.com", "pw"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsUnauthorized()
    {
        var user = MockHelpers.CreateUser();
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong-password", user.PasswordHash)).Returns(false);

        var act = async () => await CreateHandler().Handle(new LoginCommand(user.Email, "wrong-password"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }
}
