using MediatR;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Auth;
using DomainEntities = MyApp.Domain.Entities;

namespace MyApp.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = DomainEntities.User.Create(request.Email, hash, request.FirstName, request.LastName);
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var (refreshTokenValue, refreshTokenExpiry) = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = DomainEntities.RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiry);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(accessToken, refreshTokenValue, user.Email, user.FirstName, user.LastName);
    }
}
