using FluentAssertions;
using MyApp.Application.Features.Auth.Commands.Register;

namespace MyApp.Application.Tests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(new RegisterCommand("user@test.com", "Password1!", "John", "Doe"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-an-email", "Password1!", "John", "Doe", "Email")]
    [InlineData("", "Password1!", "John", "Doe", "Email")]
    [InlineData("user@test.com", "short1A", "John", "Doe", "Password")]   // < 8 chars
    [InlineData("user@test.com", "nouppercase1!", "John", "Doe", "Password")]
    [InlineData("user@test.com", "NoDigits!", "John", "Doe", "Password")]
    [InlineData("user@test.com", "Password1!", "", "Doe", "FirstName")]
    [InlineData("user@test.com", "Password1!", "John", "", "LastName")]
    public void Validate_WithInvalidField_FailsOnExpectedProperty(
        string email, string password, string first, string last, string expectedProp)
    {
        var result = _validator.Validate(new RegisterCommand(email, password, first, last));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == expectedProp);
    }
}
