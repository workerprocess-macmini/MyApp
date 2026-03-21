using MyApp.Domain.Entities;

namespace MyApp.Application.Common.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
