using FluentAssertions;
using MyApp.Domain.Entities;

namespace MyApp.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsUser()
    {
        var user = User.Create("John@Example.com", "hashed", "John", "Doe");

        user.Email.Should().Be("john@example.com"); // normalized to lowercase
        user.PasswordHash.Should().Be("hashed");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Role.Should().Be("User");
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", "hash", "First", "Last")]
    [InlineData("  ", "hash", "First", "Last")]
    [InlineData("email@test.com", "", "First", "Last")]
    [InlineData("email@test.com", "  ", "First", "Last")]
    public void Create_WithInvalidInputs_Throws(string email, string hash, string first, string last)
    {
        var act = () => User.Create(email, hash, first, last);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetRole_UpdatesRole()
    {
        var user = User.Create("test@test.com", "hash", "Test", "User");

        user.SetRole("Admin");

        user.Role.Should().Be("Admin");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void SetRole_WithBlankRole_Throws(string role)
    {
        var user = User.Create("test@test.com", "hash", "Test", "User");

        var act = () => user.SetRole(role);

        act.Should().Throw<ArgumentException>();
    }
}
