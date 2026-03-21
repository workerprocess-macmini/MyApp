using System.Net.Http.Headers;

namespace MyApp.IntegrationTests.Helpers;

public static class AuthHelper
{
    public static async Task<AuthResponse> RegisterAsync(
        HttpClient client,
        string email,
        string password = "Test1234!",
        string firstName = "Test",
        string lastName = "User")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password, firstName, lastName
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static async Task<AuthResponse> LoginAsync(HttpClient client, string email, string password = "Test1234!")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static void SetBearerToken(HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
