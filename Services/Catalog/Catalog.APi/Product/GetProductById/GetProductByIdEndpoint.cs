using Building_Blocks.Extensions;
using Carter;
using Mapster;
using MediatR;

namespace Catalog.APi.Product.GetProductById;

public record GetProductByIdRequest(Guid Id);


public record GetProductByIdResponse(Models.Product Product);



public class GetProductByIdEndpoint:ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/product/{id}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
                return result.ToHttpResult(value => Results.Ok(value.Adapt<GetProductByIdResponse>()));
            }).WithTags("Product")
            .WithName("GetProductById")
            .WithDescription("Get a product by id")
            .Produces<GetProductByIdResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get a product by id");
    }
}