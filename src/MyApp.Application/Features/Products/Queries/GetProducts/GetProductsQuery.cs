using MediatR;

namespace MyApp.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery : IRequest<List<ProductDto>>;

public record ProductDto(Guid Id, string Name, string Description, decimal Price, DateTime CreatedAt);
