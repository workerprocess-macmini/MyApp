using MediatR;

namespace MyApp.Application.Features.Auth.Commands.RevokeToken;

public record RevokeTokenCommand(string RefreshToken) : IRequest;
