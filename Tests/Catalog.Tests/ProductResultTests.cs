using System.Reflection;
using Building_Blocks.SharedResponse;
using Catalog.APi.Product.CreateProduct;
using Catalog.APi.Product.DeleteProduct;
using Catalog.APi.Product.GetProductById;
using Catalog.APi.Product.UpdateProduct;
using Marten;
using Xunit;
using Product = Catalog.APi.Models.Product;

namespace Catalog.Tests;

public class ProductResultTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_by_id_returns_a_value_or_a_typed_not_found_error(bool exists)
    {
        var product = NewProduct();
        var session = DispatchProxy.Create<IQuerySession, Stub>();
        ((Stub)(object)session).Call = (method, args) => Task.FromResult(exists ? product : null);
        var handler = new GetProductByIdQueryHandler(session);

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.Equal(exists, result.IsSuccess);
        if (exists)
            Assert.Same(product, result.GetValueOrThrow().Product);
        else
            AssertNotFound(result, product.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_saves_existing_products_and_does_not_write_missing_products(bool exists)
    {
        var product = NewProduct();
        var calls = new List<string>();
        using var cancellation = new CancellationTokenSource();
        var session = Session(product, exists, calls, cancellation.Token);
        var handler = new UpdateProductCommandHandler(session);
        var command = new UpdateProductCommand(product.Id, "Updated", "New description", 42, ["Phones"], "new.png");

        var result = await handler.Handle(command, cancellation.Token);

        Assert.Equal(exists, result.IsSuccess);
        if (exists)
        {
            Assert.True(result.GetValueOrThrow().IsSuccess);
            Assert.Equal(new[] { "LoadAsync", "Update", "SaveChangesAsync" }, calls);
            Assert.Equal(command.Name, product.Name);
            Assert.Equal(command.Description, product.Description);
            Assert.Equal(command.Price, product.Price);
            Assert.Equal(command.Category, product.Category);
            Assert.Equal(command.ImageFile, product.ImageFile);
            Assert.Equal(command.Id, product.Id);
        }
        else
        {
            AssertNotFound(result, product.Id);
            Assert.Equal(new[] { "LoadAsync" }, calls);
            Assert.Equal("Original", product.Name);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_saves_existing_products_and_does_not_write_missing_products(bool exists)
    {
        var product = NewProduct();
        var calls = new List<string>();
        using var cancellation = new CancellationTokenSource();
        var session = Session(product, exists, calls, cancellation.Token);

        var result = await new DeleteProductCommandHandler(session)
            .Handle(new DeleteProductCommand(product.Id), cancellation.Token);

        Assert.Equal(exists, result.IsSuccess);
        if (exists)
        {
            Assert.True(result.GetValueOrThrow().IsSuccess);
            Assert.Equal(new[] { "LoadAsync", "Delete", "SaveChangesAsync" }, calls);
        }
        else
        {
            AssertNotFound(result, product.Id);
            Assert.Equal(new[] { "LoadAsync" }, calls);
        }
    }

    [Fact]
    public async Task Create_returns_the_stored_product_id_inside_a_success_result()
    {
        var id = Guid.NewGuid();
        Product? stored = null;
        var session = DispatchProxy.Create<IDocumentSession, Stub>();
        ((Stub)(object)session).Call = (method, args) =>
        {
            if (method.Name == "Store")
            {
                stored = Assert.Single((Product[])args![0]!);
                stored.Id = id;
                return null;
            }

            Assert.Equal("SaveChangesAsync", method.Name);
            return Task.CompletedTask;
        };

        var result = await new CreateProductCommandHandler(session).Handle(
            new CreateProductCommand("Phone", "Description", 42, ["Phones"], "phone.png"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.GetValueOrThrow().Id);
        Assert.Equal("Phone", stored!.Name);
    }

    private static Product NewProduct() => new()
    {
        Id = Guid.NewGuid(), Name = "Original", Description = "Description",
        Price = 1, Category = ["Old"], ImageFile = "old.png"
    };

    private static IDocumentSession Session(Product product, bool exists, List<string> calls, CancellationToken token)
    {
        var session = DispatchProxy.Create<IDocumentSession, Stub>();
        ((Stub)(object)session).Call = (method, args) =>
        {
            calls.Add(method.Name);
            switch (method.Name)
            {
                case "LoadAsync":
                    Assert.Equal(product.Id, args![0]);
                    Assert.Equal(token, args[1]);
                    return Task.FromResult(exists ? product : null);
                case "SaveChangesAsync":
                    Assert.Equal(token, args![0]);
                    return Task.CompletedTask;
                case "Update":
                case "Delete":
                    return null;
                default:
                    throw new InvalidOperationException(method.Name);
            }
        };
        return session;
    }

    private static void AssertNotFound(Result result, Guid id)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("Product.NotFound", error.Code);
        Assert.Contains(id.ToString(), error.Description);
    }
}

public class Stub : DispatchProxy
{
    public Func<MethodInfo, object?[]?, object?> Call { get; set; } = default!;
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!, args);
}
