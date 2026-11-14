using FluentValidation;

namespace Catalog.APi.Product.UpdateProduct;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Description).NotEmpty();
        RuleFor(command => command.Price).GreaterThan(0);
        RuleFor(command => command.Category).NotEmpty();
        RuleForEach(command => command.Category).NotEmpty();
        RuleFor(command => command.ImageFile).NotEmpty();
    }
}
