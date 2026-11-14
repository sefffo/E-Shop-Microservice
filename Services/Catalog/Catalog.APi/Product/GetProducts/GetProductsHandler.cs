using Building_Blocks.SharedResponse;
using Building_Blocks.CQRS;
using Marten;

namespace Catalog.APi.Product.GetProducts;

public record GetProductQuery() : IQuery<Result<GetProductsResult>>;

public record GetProductsResult(
    IEnumerable<Models.Product> Products
);

internal class GetProductsQueryHandler(IQuerySession session) : IQueryHandler<GetProductQuery, Result<GetProductsResult>>
{
    public async Task<Result<GetProductsResult>> Handle(GetProductQuery Query, CancellationToken cancellationToken)
    {
        var products = await session.Query<Models.Product>().ToListAsync(cancellationToken);
        return new GetProductsResult(products);
    }
}