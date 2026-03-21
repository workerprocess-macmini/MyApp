using FluentAssertions;
using MyApp.Domain.Entities;

namespace MyApp.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsProduct()
    {
        var product = Product.Create("Laptop", "A great laptop", 999.99m);

        product.Name.Should().Be("Laptop");
        product.Description.Should().Be("A great laptop");
        product.Price.Should().Be(999.99m);
        product.Id.Should().NotBeEmpty();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        product.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("", "desc", 10)]
    [InlineData("  ", "desc", 10)]
    public void Create_WithBlankName_Throws(string name, string desc, decimal price)
    {
        var act = () => Product.Create(name, desc, price);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithNonPositivePrice_Throws(decimal price)
    {
        var act = () => Product.Create("Laptop", "desc", price);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_WithValidInputs_UpdatesFields()
    {
        var product = Product.Create("Laptop", "Old desc", 999.99m);

        product.Update("Laptop Pro", "New desc", 1299.99m);

        product.Name.Should().Be("Laptop Pro");
        product.Description.Should().Be("New desc");
        product.Price.Should().Be(1299.99m);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "desc", 10)]
    [InlineData("  ", "desc", 10)]
    public void Update_WithBlankName_Throws(string name, string desc, decimal price)
    {
        var product = Product.Create("Laptop", "desc", 10m);

        var act = () => product.Update(name, desc, price);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Update_WithNonPositivePrice_Throws(decimal price)
    {
        var product = Product.Create("Laptop", "desc", 10m);

        var act = () => product.Update("Laptop", "desc", price);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
