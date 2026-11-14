using Building_Blocks.SharedResponse;
using Building_Blocks.CQRS;
using Marten;

namespace Catalog.APi.Product.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    List<string> Category,
    string ImageFile) : ICommand<Result<UpdateProductResult>>;

public record UpdateProductResult(bool IsSuccess);

public class UpdateProductCommandHandler(IDocumentSession session)
    : ICommandHandler<UpdateProductCommand, Result<UpdateProductResult>>
{
    public async Task<Result<UpdateProductResult>> Handle(
        UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await session.LoadAsync<Models.Product>(command.Id, cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(command.Id);
        }

        product.Name = command.Name;
        product.Description = command.Description;
        product.Price = command.Price;
        product.Category = command.Category;
        product.ImageFile = command.ImageFile;

        session.Update(product);
        await session.SaveChangesAsync(cancellationToken);

        return new UpdateProductResult(true);
    }
}
