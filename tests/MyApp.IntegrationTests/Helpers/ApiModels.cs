namespace MyApp.IntegrationTests.Helpers;

// Mirror of application response DTOs for deserialization
public record AuthResponse(string AccessToken, string RefreshToken, string Email, string FirstName, string LastName);

public record ProductResponse(Guid Id, string Name, string Description, decimal Price, DateTime CreatedAt);

public record CreatedResponse(Guid Id);

public record ValidationErrorResponse(List<ValidationError> Errors);
public record ValidationError(string PropertyName, string ErrorMessage);

public record ErrorResponse(string Error);
