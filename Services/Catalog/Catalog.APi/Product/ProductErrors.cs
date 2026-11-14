using Building_Blocks.SharedResponse;

namespace Catalog.APi.Product;

public static class ProductErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound("Product.NotFound", $"Product with ID {id} was not found.");
}
