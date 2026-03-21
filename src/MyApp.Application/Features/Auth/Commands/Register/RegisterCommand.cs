using MediatR;
using MyApp.Application.Features.Auth;

namespace MyApp.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Email, string Password, string FirstName, string LastName) : IRequest<AuthResponseDto>;
