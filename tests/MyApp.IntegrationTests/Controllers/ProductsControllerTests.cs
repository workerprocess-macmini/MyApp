using MyApp.IntegrationTests;

namespace MyApp.IntegrationTests.Controllers;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var email = $"prod-{Guid.NewGuid()}@test.com";
        var auth = await AuthHelper.RegisterAsync(_client, email);
        var client = _factory.CreateClient();
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        return client;
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new { name = "Test", description = "Desc", price = 9.99m });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/products ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithAuth_Returns200WithSeededProducts()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        products.Should().NotBeEmpty(); // seeded 5 products
    }

    // ── POST /api/products ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201WithId()
    {
        var client = await CreateAuthenticatedClientAsync();
        var name = $"Product-{Guid.NewGuid()}";

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name, description = "A test product", price = 49.99m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        body!.Id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "",       // invalid
            description = "desc",
            price = -10m     // invalid
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        body!.Errors.Should().Contain(e => e.PropertyName == "Name");
        body.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    // ── GET /api/products/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithExistingId_Returns200()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create a product first
        var created = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"Laptop-{Guid.NewGuid()}", description = "desc", price = 999m
        });
        var body = await created.Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await client.GetAsync($"/api/products/{body!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Id.Should().Be(body.Id);
        product.Price.Should().Be(999m);
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/products/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithExistingId_Returns204()
    {
        var client = await CreateAuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"Old-{Guid.NewGuid()}", description = "old", price = 10m
        });
        var body = await created.Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await client.PutAsJsonAsync($"/api/products/{body!.Id}", new
        {
            name = "New Name", description = "new desc", price = 20m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update was persisted
        var get = await client.GetAsync($"/api/products/{body.Id}");
        var product = await get.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Name.Should().Be("New Name");
        product.Price.Should().Be(20m);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", new
        {
            name = "Name", description = "desc", price = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/products/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithExistingId_Returns204AndProductIsGone()
    {
        var client = await CreateAuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"ToDelete-{Guid.NewGuid()}", description = "desc", price = 5m
        });
        var body = await created.Content.ReadFromJsonAsync<CreatedResponse>();

        var delete = await client.DeleteAsync($"/api/products/{body!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await client.GetAsync($"/api/products/{body.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithUnknownId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
