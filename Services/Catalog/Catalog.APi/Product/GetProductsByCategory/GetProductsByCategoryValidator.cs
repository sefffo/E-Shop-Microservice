using FluentValidation;

namespace Catalog.APi.Product.GetProductsByCategory;

public class GetProductsByCategoryValidator : AbstractValidator<GetProductsByCategoryQuery>
{
    public GetProductsByCategoryValidator()
    {
        RuleFor(query => query.Category).NotEmpty();
    }
}
