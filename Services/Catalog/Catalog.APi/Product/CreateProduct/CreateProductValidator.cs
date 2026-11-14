using FluentValidation;

namespace Catalog.APi.Product.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Description).NotEmpty();
        RuleFor(command => command.Price).GreaterThan(0);
        RuleFor(command => command.Category).NotEmpty();
        RuleForEach(command => command.Category).NotEmpty();
        RuleFor(command => command.ImageFile).NotEmpty();
    }
}
