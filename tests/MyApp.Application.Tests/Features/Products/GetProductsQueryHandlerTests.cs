using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Products.Queries.GetProducts;
using MyApp.Application.Tests.Common;

namespace MyApp.Application.Tests.Features.Products;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    [Fact]
    public async Task Handle_ReturnsAllProductsAsDtos()
    {
        var products = new[]
        {
            MockHelpers.CreateProduct("Laptop", "Great laptop", 999m),
            MockHelpers.CreateProduct("Mouse",  "Wireless mouse", 29m),
        };
        _productRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(products);

        var handler = new GetProductsQueryHandler(_productRepo.Object);
        var result = await handler.Handle(new GetProductsQuery(), default);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Laptop");
        result[0].Price.Should().Be(999m);
        result[1].Name.Should().Be("Mouse");
    }

    [Fact]
    public async Task Handle_WithNoProducts_ReturnsEmptyList()
    {
        _productRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var handler = new GetProductsQueryHandler(_productRepo.Object);
        var result = await handler.Handle(new GetProductsQuery(), default);

        result.Should().BeEmpty();
    }
}
