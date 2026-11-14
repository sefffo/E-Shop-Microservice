using FluentValidation;

namespace Catalog.APi.Product.GetProductById;

public class GetProductByIdValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
