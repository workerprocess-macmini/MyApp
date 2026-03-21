using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Products.Queries.GetProducts;
using MyApp.Application.Tests.Common;
using MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Products;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetProductByIdQueryHandler CreateHandler() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_WithExistingId_ReturnsDto()
    {
        var product = MockHelpers.CreateProduct("Laptop", "Great laptop", 999m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, default)).ReturnsAsync(product);

        var result = await CreateHandler().Handle(new GetProductByIdQuery(product.Id), default);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Laptop");
        result.Price.Should().Be(999m);
    }

    [Fact]
    public async Task Handle_WithUnknownId_ReturnsNull()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var result = await CreateHandler().Handle(new GetProductByIdQuery(Guid.NewGuid()), default);

        result.Should().BeNull();
    }
}
