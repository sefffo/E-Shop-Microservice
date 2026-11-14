using Building_Blocks.SharedResponse;
using Building_Blocks.CQRS;
using Marten;

namespace Catalog.APi.Product.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    List<string> Category,
    string ImageFile) : ICommand<Result<CreateProductResult>>;

public record CreateProductResult(Guid Id);

public class CreateProductCommandHandler(IDocumentSession session) : ICommandHandler<CreateProductCommand, Result<CreateProductResult>>
{
    public async Task<Result<CreateProductResult>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
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
        // we gonna use the marten session we don't need to use repo pattern as the marten is abstraction already itself
        session.Store(product);
        await session.SaveChangesAsync(cancellationToken);

        return new CreateProductResult(product.Id); // from the database we get the id
    } 
    
}