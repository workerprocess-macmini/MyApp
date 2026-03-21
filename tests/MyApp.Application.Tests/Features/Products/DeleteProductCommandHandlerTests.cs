using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Products.Commands.DeleteProduct;
using MyApp.Application.Tests.Common;
using MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Products;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteProductCommandHandler CreateHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithExistingProduct_DeletesAndReturnsTrue()
    {
        var product = MockHelpers.CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, default)).ReturnsAsync(product);

        var result = await CreateHandler().Handle(new DeleteProductCommand(product.Id), default);

        result.Should().BeTrue();
        _productRepo.Verify(r => r.DeleteAsync(product, default), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownId_ReturnsFalse()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var result = await CreateHandler().Handle(new DeleteProductCommand(Guid.NewGuid()), default);

        result.Should().BeFalse();
        _productRepo.Verify(r => r.DeleteAsync(It.IsAny<Product>(), default), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
