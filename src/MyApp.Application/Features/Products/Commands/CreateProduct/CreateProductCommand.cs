using MediatR;

namespace MyApp.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(string Name, string Description, decimal Price) : IRequest<Guid>;
