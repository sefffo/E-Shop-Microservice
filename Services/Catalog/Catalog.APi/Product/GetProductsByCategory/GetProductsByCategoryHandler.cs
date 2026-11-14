using Building_Blocks.SharedResponse;
using Building_Blocks.CQRS;
using Marten;

namespace Catalog.APi.Product.GetProductsByCategory;

public record GetProductsByCategoryQuery(string Category) : IQuery<Result<GetProductsByCategoryResult>>;

public record GetProductsByCategoryResult(IEnumerable<Models.Product> Products);

internal class GetProductsByCategoryQueryHandler(
    IQuerySession session)
    : IQueryHandler<GetProductsByCategoryQuery, Result<GetProductsByCategoryResult>>
{
    public async Task<Result<GetProductsByCategoryResult>> Handle(
        GetProductsByCategoryQuery query, CancellationToken cancellationToken)
    {
        var products = await session.Query<Models.Product>()
            .Where(product => product.Category.Contains(query.Category))
            .ToListAsync(cancellationToken);

        return new GetProductsByCategoryResult(products);
    }
}
