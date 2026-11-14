using Building_Blocks.Extensions;
using Carter;
using Mapster;
using MediatR;

namespace Catalog.APi.Product.GetProducts;

// later we gonna modify on the request when we add specification pattern

public record GetProductsResponse(
    IEnumerable<Models.Product> Products
);

public class GetProductsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetProductQuery(), cancellationToken
                );

                return result.ToHttpResult(value => Results.Ok(value.Adapt<GetProductsResponse>()));

            })
            .WithTags("Product")
            .WithName("GetProducts")
            .WithDescription("Get all products")
            .Produces<GetProductsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
