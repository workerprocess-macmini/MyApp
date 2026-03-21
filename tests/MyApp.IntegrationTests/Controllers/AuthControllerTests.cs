using System.Net.Http.Headers;
using MyApp.IntegrationTests;

namespace MyApp.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns200WithTokens()
    {
        var email = $"reg-{Guid.NewGuid()}@test.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Test1234!", firstName = "Jane", lastName = "Doe"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(email);
        body.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Test1234!", firstName = "Jane", lastName = "Doe"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidData_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "weak",          // too short, no digit, no uppercase
            firstName = "",
            lastName = "Doe"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        body!.Errors.Should().NotBeEmpty();
        body.Errors.Should().Contain(e => e.PropertyName == "Email");
        body.Errors.Should().Contain(e => e.PropertyName == "Password");
        body.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        var email = $"login-{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email, password = "Test1234!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"loginbad-{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email, password = "WrongPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "ghost@nowhere.com", password = "Test1234!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidToken_Returns200WithNewTokens()
    {
        var email = $"refresh-{Guid.NewGuid()}@test.com";
        var auth = await AuthHelper.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(auth.RefreshToken); // rotated
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "completely-invalid-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_AfterTokenRotation_OldTokenIsRejected()
    {
        var email = $"rotate-{Guid.NewGuid()}@test.com";
        var auth = await AuthHelper.RegisterAsync(_client, email);
        var oldRefreshToken = auth.RefreshToken;

        // Use the token once to rotate it
        await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = oldRefreshToken });

        // Old token should now be rejected
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = oldRefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_WithValidTokenAndAuth_Returns204()
    {
        var email = $"revoke-{Guid.NewGuid()}@test.com";
        var auth = await AuthHelper.RegisterAsync(_client, email);

        var client = _factory.CreateClient();
        AuthHelper.SetBearerToken(client, auth.AccessToken);

        var response = await client.PostAsJsonAsync("/api/auth/revoke", new
        {
            refreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new
        {
            refreshToken = "some-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_ThenRefresh_Returns401()
    {
        var email = $"revoke-refresh-{Guid.NewGuid()}@test.com";
        var auth = await AuthHelper.RegisterAsync(_client, email);

        var authedClient = _factory.CreateClient();
        AuthHelper.SetBearerToken(authedClient, auth.AccessToken);
        await authedClient.PostAsJsonAsync("/api/auth/revoke", new { refreshToken = auth.RefreshToken });

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
