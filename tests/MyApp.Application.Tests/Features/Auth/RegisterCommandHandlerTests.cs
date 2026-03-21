using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Auth.Commands.Register;
using MyApp.Application.Tests.Common;
using MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RegisterCommandHandler CreateHandler() => new(
        _userRepo.Object,
        _refreshTokenRepo.Object,
        _passwordHasher.Object,
        _jwtTokenService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithNewEmail_ReturnsTokens()
    {
        var command = new RegisterCommand("new@test.com", "Password1!", "Jane", "Doe");

        _userRepo.Setup(r => r.ExistsByEmailAsync(command.Email, default)).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(command.Password)).Returns("hashed-password");
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), default)).ReturnsAsync((User u, CancellationToken _) => u);
        _jwtTokenService.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _jwtTokenService.Setup(j => j.GenerateRefreshToken()).Returns(("refresh-token", DateTime.UtcNow.AddDays(7)));

        var result = await CreateHandler().Handle(command, default);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Email.Should().Be("new@test.com");
        result.FirstName.Should().Be("Jane");
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        var command = new RegisterCommand("existing@test.com", "Password1!", "Jane", "Doe");

        _userRepo.Setup(r => r.ExistsByEmailAsync(command.Email, default)).ReturnsAsync(true);

        var act = async () => await CreateHandler().Handle(command, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*existing@test.com*");
    }

    [Fact]
    public async Task Handle_HashesPasswordBeforeSaving()
    {
        var command = new RegisterCommand("new@test.com", "PlainPassword1!", "Jane", "Doe");
        User? savedUser = null;

        _userRepo.Setup(r => r.ExistsByEmailAsync(command.Email, default)).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(command.Password)).Returns("hashed-pw");
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), default))
            .Callback<User, CancellationToken>((u, _) => savedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);
        _jwtTokenService.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("token");
        _jwtTokenService.Setup(j => j.GenerateRefreshToken()).Returns(("rt", DateTime.UtcNow.AddDays(7)));

        await CreateHandler().Handle(command, default);

        savedUser!.PasswordHash.Should().Be("hashed-pw");
        savedUser.PasswordHash.Should().NotBe(command.Password);
    }
}
