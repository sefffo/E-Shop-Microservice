using Carter;
using Mapster;
using MediatR;

namespace Catalog.APi.Product.CreateProduct;

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    List<string> Category,
    string ImageFile);

public record CreateProductResponse(Guid Id);

public class CreateProductEndPoint :ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/products", async (CreateProductRequest request, ISender sender) =>
        {
            // for the mapping use mapster => maps from command to the request object 
            var command = request.Adapt<CreateProductCommand>();
            var result = await sender.Send(command);
            // nfs el kalam kod mn el result fe el command handler w hoto fr el response el fe el endpoint 
            var response = result.Adapt<CreateProductResponse>();
            return Results.Created($"/products/{response.Id}", response);

        })
            .WithTags("Product")
            .WithName("CreateProduct")
            .WithDescription("Create a new product")
            .Produces<CreateProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
            
    }
}