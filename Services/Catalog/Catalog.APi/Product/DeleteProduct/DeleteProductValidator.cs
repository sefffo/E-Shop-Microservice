using FluentValidation;

namespace Catalog.APi.Product.DeleteProduct;

public class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
