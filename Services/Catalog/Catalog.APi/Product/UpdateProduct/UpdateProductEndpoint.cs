using Building_Blocks.Extensions;
using Carter;
using Mapster;
using MediatR;

namespace Catalog.APi.Product.UpdateProduct;

public record UpdateProductRequest(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    List<string> Category,
    string ImageFile);

public record UpdateProductResponse(bool IsSuccess);

public class UpdateProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/products", async (UpdateProductRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = request.Adapt<UpdateProductCommand>();
                var result = await sender.Send(command, cancellationToken);

                return result.ToHttpResult(value => Results.Ok(value.Adapt<UpdateProductResponse>()));
            })
            .WithTags("Product")
            .WithName("UpdateProduct")
            .WithDescription("Update a product")
            .Produces<UpdateProductResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
