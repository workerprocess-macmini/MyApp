using MediatR;
using MyApp.Application.Features.Auth;

namespace MyApp.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
