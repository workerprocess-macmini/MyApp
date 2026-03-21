namespace MyApp.Application.Features.Auth;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string Email,
    string FirstName,
    string LastName);
