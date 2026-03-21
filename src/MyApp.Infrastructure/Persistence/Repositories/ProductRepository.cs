using Microsoft.EntityFrameworkCore;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Persistence.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(p => p.Name == name, cancellationToken);
}
