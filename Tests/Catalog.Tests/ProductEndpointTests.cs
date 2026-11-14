using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Building_Blocks.SharedResponse;
using Carter;
using Catalog.APi.Product.CreateProduct;
using Catalog.APi.Product.DeleteProduct;
using Catalog.APi.Product.GetProductById;
using Catalog.APi.Product.GetProducts;
using Catalog.APi.Product.GetProductsByCategory;
using Catalog.APi.Product.UpdateProduct;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Product = Catalog.APi.Models.Product;

namespace Catalog.Tests;

public class ProductEndpointTests
{
    [Theory]
    [InlineData("POST", "/products", "id", 201)]
    [InlineData("GET", "/products", "products", 200)]
    [InlineData("GET", "/product/{id}", "product", 200)]
    [InlineData("GET", "/products/category/Phones", "products", 200)]
    [InlineData("PUT", "/products", "isSuccess", 200)]
    [InlineData("DELETE", "/products/{id}", "isSuccess", 200)]
    public async Task Endpoints_preserve_success_bodies_and_use_central_failure_handling(
        string method, string route, string property, int successCode)
    {
        var id = Guid.NewGuid();
        var product = new Product { Id = id, Name = "Phone", Description = "Description", Price = 42, Category = ["Phones"], ImageFile = "phone.png" };
        var fail = false;
        object? sent = null;
        Task<Result<T>> Respond<T>(T value) => Task.FromResult(fail
            ? Result<T>.Fail(Error.NotFound("Product.NotFound", "Product was not found."))
            : Result<T>.Ok(value));
        var sender = DispatchProxy.Create<ISender, Stub>();
        ((Stub)(object)sender).Call = (called, args) =>
        {
            sent = args![0];
            return sent switch
            {
                CreateProductCommand => Respond(new CreateProductResult(id)),
                GetProductQuery => Respond(new GetProductsResult([product])),
                GetProductByIdQuery => Respond(new GetProductResult(product)),
                GetProductsByCategoryQuery => Respond(new GetProductsByCategoryResult([product])),
                UpdateProductCommand => Respond(new UpdateProductResult(true)),
                DeleteProductCommand => Respond(new DeleteProductResult(true)),
                _ => throw new InvalidOperationException("Unexpected request")
            };
        };
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(sender);
        await using var app = builder.Build();
        ICarterModule[] modules = [new CreateProductEndPoint(), new GetProductsEndpoint(), new GetProductByIdEndpoint(),
            new GetProductsByCategoryEndpoint(), new UpdateProductEndpoint(), new DeleteProductEndpoint()];
        foreach (var module in modules)
            module.AddRoutes(app);
        await app.StartAsync();
        using var client = app.GetTestClient();

        async Task<HttpResponseMessage> Send()
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), route.Replace("{id}", id.ToString()));
            if (method is "POST" or "PUT")
                request.Content = JsonContent.Create(product);
            return await client.SendAsync(request);
        }

        using var success = await Send();
        Assert.Equal(successCode, (int)success.StatusCode);
        using var successJson = JsonDocument.Parse(await success.Content.ReadAsStringAsync());
        var value = successJson.RootElement.GetProperty(property);
        if (property == "id")
        {
            Assert.Equal(id, value.GetGuid());
            Assert.Equal($"/product/{id}", success.Headers.Location!.ToString());
        }
        else if (property == "isSuccess")
            Assert.True(value.GetBoolean());
        else if (property == "products")
            Assert.Equal(id, value[0].GetProperty("id").GetGuid());
        else
            Assert.Equal(id, value.GetProperty("id").GetGuid());

        if (sent is GetProductsByCategoryQuery categoryQuery)
            Assert.Equal("Phones", categoryQuery.Category);
        if (sent is UpdateProductCommand updateCommand)
            Assert.Equal(id, updateCommand.Id);

        fail = true;
        using var failure = await Send();
        Assert.Equal(HttpStatusCode.NotFound, failure.StatusCode);
        Assert.Equal("application/problem+json", failure.Content.Headers.ContentType!.MediaType);
        using var failureJson = JsonDocument.Parse(await failure.Content.ReadAsStringAsync());
        Assert.Equal("Product.NotFound", failureJson.RootElement.GetProperty("title").GetString());
    }
}
