using MediatR;
using MyApp.Application.Common.Interfaces;

namespace MyApp.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price) : IRequest<bool>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null) return false;

        product.Update(request.Name, request.Description, request.Price);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
