using MediatR;
using MyApp.Application.Features.Auth;

namespace MyApp.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
