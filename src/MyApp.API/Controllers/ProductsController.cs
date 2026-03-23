using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyApp.Application.Features.Products.Commands.CreateProduct;
using MyApp.Application.Features.Products.Commands.DeleteProduct;
using MyApp.Application.Features.Products.Commands.UpdateProduct;
using MyApp.Application.Features.Products.Queries.GetProducts;

namespace MyApp.API.Controllers;

/// <summary>Products — CRUD operations (requires authentication).</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Tags("Products")]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Return all products.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products.</returns>
    [HttpGet]
    [ProducesResponseType<List<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var products = await _mediator.Send(new GetProductsQuery(), cancellationToken);
        return Ok(products);
    }

    /// <summary>Return a single product by its ID.</summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching product, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Create a new product.</summary>
    /// <param name="command">Product data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created product.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Update an existing product.</summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="request">Updated product data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(
            new UpdateProductCommand(id, request.Name, request.Description, request.Price),
            cancellationToken);
        return success ? NoContent() : NotFound();
    }

    /// <summary>Delete a product.</summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return success ? NoContent() : NotFound();
    }
}

/// <summary>Request body for updating a product.</summary>
/// <param name="Name">Product name.</param>
/// <param name="Description">Product description.</param>
/// <param name="Price">Unit price.</param>
public record UpdateProductRequest(string Name, string Description, decimal Price);
