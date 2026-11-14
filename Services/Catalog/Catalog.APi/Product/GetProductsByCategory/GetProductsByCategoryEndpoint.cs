using Building_Blocks.Extensions;
using Carter;
using Mapster;
using MediatR;

namespace Catalog.APi.Product.GetProductsByCategory;

public record GetProductsByCategoryResponse(IEnumerable<Models.Product> Products);

public class GetProductsByCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/products/category/{category}", async (string category, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetProductsByCategoryQuery(category), cancellationToken);
                return result.ToHttpResult(value => Results.Ok(value.Adapt<GetProductsByCategoryResponse>()));
            })
            .WithTags("Product")
            .WithName("GetProductsByCategory")
            .WithDescription("Get products by category")
            .Produces<GetProductsByCategoryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
