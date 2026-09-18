using Building_Blocks.CQRS;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Catalog.APi.Product.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    List<string> Category,
    string ImageFile) : ICommand<CreateProductResult>;

public record CreateProductResult(Guid Id);

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = new Models.Product
        {
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            Category = command.Category,
            ImageFile = command.ImageFile
        };

        // TODO: Save entity to DB via DbContext or Repository

        return new CreateProductResult(Guid.NewGuid());
    }
    
}