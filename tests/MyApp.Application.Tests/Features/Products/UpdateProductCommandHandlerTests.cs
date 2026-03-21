using FluentAssertions;
using Moq;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Features.Products.Commands.UpdateProduct;
using MyApp.Application.Tests.Common;
using MyApp.Domain.Entities;

namespace MyApp.Application.Tests.Features.Products;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateProductCommandHandler CreateHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithExistingProduct_UpdatesAndReturnsTrue()
    {
        var product = MockHelpers.CreateProduct();
        var command = new UpdateProductCommand(product.Id, "Updated Name", "Updated Desc", 199m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, default)).ReturnsAsync(product);

        var result = await CreateHandler().Handle(command, default);

        result.Should().BeTrue();
        product.Name.Should().Be("Updated Name");
        product.Price.Should().Be(199m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownId_ReturnsFalse()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var result = await CreateHandler().Handle(
            new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", 10m), default);

        result.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
