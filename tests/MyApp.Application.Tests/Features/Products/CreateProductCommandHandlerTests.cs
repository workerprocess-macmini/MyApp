using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Products.Commands.CreateProduct;
using MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Products;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateProductCommandHandler CreateHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNewProductId()
    {
        var command = new CreateProductCommand("Laptop", "A laptop", 999m);
        Product? saved = null;

        _productRepo.Setup(r => r.AddAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => saved = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var id = await CreateHandler().Handle(command, default);

        id.Should().NotBeEmpty();
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Laptop");
        saved.Price.Should().Be(999m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
