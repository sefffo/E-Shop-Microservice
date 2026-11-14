using Building_Blocks.Behaviors;
using Building_Blocks.Extensions;
using Building_Blocks.SharedResponse;
using Catalog.APi.Product.CreateProduct;
using Catalog.APi.Product.DeleteProduct;
using Catalog.APi.Product.GetProductById;
using Catalog.APi.Product.GetProducts;
using Catalog.APi.Product.GetProductsByCategory;
using Catalog.APi.Product.UpdateProduct;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalog.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("Name")]
    [InlineData("Description")]
    [InlineData("Price")]
    [InlineData("NegativePrice")]
    [InlineData("Category")]
    [InlineData("NullCategory")]
    [InlineData("Category[0]")]
    [InlineData("NullCategoryItem")]
    [InlineData("ImageFile")]
    public async Task Create_and_update_reject_invalid_product_fields(string field)
    {
        var valid = ValidProduct();
        var invalid = field switch
        {
            "Name" => valid with { Name = " " },
            "Description" => valid with { Description = null! },
            "Price" => valid with { Price = 0 },
            "NegativePrice" => valid with { Price = -1 },
            "Category" => valid with { Category = [] },
            "NullCategory" => valid with { Category = null! },
            "Category[0]" => valid with { Category = [" "] },
            "NullCategoryItem" => valid with { Category = [null!] },
            "ImageFile" => valid with { ImageFile = "" },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
        var expectedField = field switch
        {
            "NegativePrice" => "Price",
            "NullCategory" => "Category",
            "NullCategoryItem" => "Category[0]",
            _ => field
        };
        var createResult = await new CreateProductValidator().ValidateAsync(invalid);
        var update = new UpdateProductCommand(Guid.NewGuid(), invalid.Name, invalid.Description,
            invalid.Price, invalid.Category, invalid.ImageFile);
        var updateResult = await new UpdateProductValidator().ValidateAsync(update);

        Assert.Contains(createResult.Errors, error => error.PropertyName == expectedField);
        Assert.Contains(updateResult.Errors, error => error.PropertyName == expectedField);
    }

    [Fact]
    public async Task Every_slice_validator_is_registered_and_accepts_valid_input()
    {
        using var services = new ServiceCollection().AddLogging()
            .AddBuildingBlocks(typeof(CreateProductCommand).Assembly).BuildServiceProvider();
        var product = ValidProduct();
        var id = Guid.NewGuid();

        Assert.True((await services.GetRequiredService<IValidator<CreateProductCommand>>().ValidateAsync(product)).IsValid);
        Assert.True((await services.GetRequiredService<IValidator<UpdateProductCommand>>().ValidateAsync(
            new(id, product.Name, product.Description, product.Price, product.Category, product.ImageFile))).IsValid);
        Assert.True((await services.GetRequiredService<IValidator<DeleteProductCommand>>().ValidateAsync(new(id))).IsValid);
        Assert.True((await services.GetRequiredService<IValidator<GetProductByIdQuery>>().ValidateAsync(new(id))).IsValid);
        Assert.True((await services.GetRequiredService<IValidator<GetProductsByCategoryQuery>>().ValidateAsync(new("Phones"))).IsValid);
        Assert.True((await services.GetRequiredService<IValidator<GetProductQuery>>().ValidateAsync(new())).IsValid);
    }

    [Fact]
    public async Task Id_validators_reject_empty_guids()
    {
        var product = ValidProduct();
        Assert.False((await new GetProductByIdValidator().ValidateAsync(new GetProductByIdQuery(Guid.Empty))).IsValid);
        Assert.False((await new DeleteProductValidator().ValidateAsync(new DeleteProductCommand(Guid.Empty))).IsValid);
        Assert.False((await new UpdateProductValidator().ValidateAsync(new UpdateProductCommand(Guid.Empty,
            product.Name, product.Description, product.Price, product.Category, product.ImageFile))).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Category_queries_reject_blank_categories(string? category)
    {
        Assert.False((await new GetProductsByCategoryValidator().ValidateAsync(new GetProductsByCategoryQuery(category!))).IsValid);
    }

    [Fact]
    public async Task Validation_aggregates_multiple_validators_once_and_skips_the_handler()
    {
        var first = new InlineValidator<CreateProductCommand>();
        first.RuleFor(command => command.Name).NotEmpty();
        var second = new InlineValidator<CreateProductCommand>();
        second.RuleFor(command => command.Price).GreaterThan(0);
        var behavior = new ValidationBehavior<CreateProductCommand, Result<CreateProductResult>>([first, second]);
        var handled = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            ValidProduct() with { Name = "", Price = 0 }, token =>
            {
                handled = true;
                return Task.FromResult(Result<CreateProductResult>.Ok(new(Guid.NewGuid())));
            }, CancellationToken.None));

        Assert.Equal(new[] { "Name", "Price" }, exception.Errors.Select(error => error.PropertyName));
        Assert.False(handled);
    }

    [Fact]
    public async Task Validation_supports_async_rules_and_passes_cancellation_to_rules_and_handler()
    {
        using var cancellation = new CancellationTokenSource();
        var validator = new InlineValidator<GetProductsByCategoryQuery>();
        validator.RuleFor(query => query.Category).MustAsync((category, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            return Task.FromResult(category == "Phones");
        });
        var behavior = new ValidationBehavior<GetProductsByCategoryQuery, string>([validator]);

        var result = await behavior.Handle(new("Phones"), token =>
        {
            Assert.Equal(cancellation.Token, token);
            return Task.FromResult("handled");
        }, cancellation.Token);

        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Requests_without_validators_can_reach_the_handler()
    {
        var behavior = new ValidationBehavior<GetProductQuery, string>([]);
        Assert.Equal("handled", await behavior.Handle(new(), token => Task.FromResult("handled"), CancellationToken.None));
    }

    private static CreateProductCommand ValidProduct() => new("Phone", "Description", 42, ["Phones"], "phone.png");
}
