using MediatR;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Auth;
using DomainEntities = MyApp.Domain.Entities;

namespace MyApp.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existing.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        var user = await _userRepository.GetByIdAsync(existing.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        // Rotate: revoke old, issue new
        existing.Revoke();

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var (newRefreshTokenValue, newRefreshTokenExpiry) = _jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = DomainEntities.RefreshToken.Create(user.Id, newRefreshTokenValue, newRefreshTokenExpiry);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(accessToken, newRefreshTokenValue, user.Email, user.FirstName, user.LastName);
    }
}
