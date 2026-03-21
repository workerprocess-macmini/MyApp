using MediatR;
using MyApp.Application.Common.Interfaces;

namespace MyApp.Application.Features.Products.Queries.GetProducts;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;

    public GetProductByIdQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null) return null;

        return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.CreatedAt);
    }
}
