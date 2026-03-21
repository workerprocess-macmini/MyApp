using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;
using MyApp.Application.Common.Behaviors;
using MyApp.Application.Features.Products.Commands.CreateProduct;

namespace MyApp.Application.Tests.Common;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<CreateProductCommand, Guid>([]);
        var nextCalled = false;
        var expected = Guid.NewGuid();

        var result = await behavior.Handle(
            new CreateProductCommand("Name", "Desc", 10m),
            () => { nextCalled = true; return Task.FromResult(expected); },
            default);

        nextCalled.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsNext()
    {
        var validator = new CreateProductCommandValidator();
        var behavior = new ValidationBehavior<CreateProductCommand, Guid>([validator]);
        var nextCalled = false;

        await behavior.Handle(
            new CreateProductCommand("Valid Name", "Desc", 10m),
            () => { nextCalled = true; return Task.FromResult(Guid.NewGuid()); },
            default);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationException()
    {
        var validator = new CreateProductCommandValidator();
        var behavior = new ValidationBehavior<CreateProductCommand, Guid>([validator]);

        var act = async () => await behavior.Handle(
            new CreateProductCommand("", "Desc", -1m), // blank name, negative price
            () => Task.FromResult(Guid.NewGuid()),
            default);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.PropertyName == "Name")
                      && ex.Errors.Any(e => e.PropertyName == "Price"));
    }
}
