using Building_Blocks.SharedResponse;
using Building_Blocks.CQRS;
using Marten;

namespace Catalog.APi.Product.GetProductById;

public record GetProductByIdQuery(Guid Id) : IQuery<Result<GetProductResult>>;

public record GetProductResult(
    Models.Product Product
    
    );




public class GetProductByIdQueryHandler (IQuerySession session) : IQueryHandler<GetProductByIdQuery,Result<GetProductResult>>
{
    public async Task<Result<GetProductResult>> Handle(GetProductByIdQuery Query, CancellationToken cancellationToken)
    {
        var product= await session.LoadAsync<Models.Product>(
            Query.Id,
            cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(Query.Id);
        }

        return new GetProductResult(product);
    }
}