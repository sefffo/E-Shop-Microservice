using Building_Blocks.SharedResponse;
using Building_Blocks.CQRS;
using Marten;

namespace Catalog.APi.Product.DeleteProduct;

public record DeleteProductCommand(Guid Id) : ICommand<Result<DeleteProductResult>>;

public record DeleteProductResult(bool IsSuccess);

public class DeleteProductCommandHandler(IDocumentSession session)
    : ICommandHandler<DeleteProductCommand, Result<DeleteProductResult>>
{
    public async Task<Result<DeleteProductResult>> Handle(
        DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await session.LoadAsync<Models.Product>(command.Id, cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(command.Id);
        }

        session.Delete(product);
        await session.SaveChangesAsync(cancellationToken);

        return new DeleteProductResult(true);
    }
}
