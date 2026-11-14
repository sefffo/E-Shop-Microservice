using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Building_Blocks.Extensions;
using Carter;
using Catalog.APi.Product.CreateProduct;
using Catalog.APi.Product.DeleteProduct;
using Catalog.APi.Product.GetProductById;
using Catalog.APi.Product.GetProducts;
using Catalog.APi.Product.GetProductsByCategory;
using Catalog.APi.Product.UpdateProduct;
using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Product = Catalog.APi.Models.Product;

namespace Catalog.Tests;

public class PipelineEndpointTests : IAsyncLifetime
{
    private WebApplication _app = default!;
    private HttpClient _client = default!;
    private int _databaseCalls;
    private bool _throwOnDatabaseCall;

    public async Task InitializeAsync()
    {
        var session = DispatchProxy.Create<IDocumentSession, Stub>();
        ((Stub)(object)session).Call = (method, args) =>
        {
            _databaseCalls++;
            if (_throwOnDatabaseCall)
                throw new InvalidOperationException("Private database connection details");

            if (method.Name == "LoadAsync")
                return Task.FromResult<Product?>(null);
            if (method.Name == "Store")
            {
                ((Product[])args![0]!)[0].Id = Guid.NewGuid();
                return null;
            }
            if (method.Name == "SaveChangesAsync")
                return Task.CompletedTask;
            throw new InvalidOperationException(method.Name);
        };
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddBuildingBlocks(typeof(CreateProductCommand).Assembly);
        builder.Services.AddSingleton(session);
        builder.Services.AddSingleton<IQuerySession>(session);
        _app = builder.Build();
        _app.UseExceptionHandler();
        ICarterModule[] modules = [new CreateProductEndPoint(), new GetProductsEndpoint(), new GetProductByIdEndpoint(),
            new GetProductsByCategoryEndpoint(), new UpdateProductEndpoint(), new DeleteProductEndpoint()];
        foreach (var module in modules)
            module.AddRoutes(_app);
        _app.MapGet("/bad-request", (Func<IResult>)(() => throw new BadHttpRequestException("Too large", 413)));
        _app.MapGet("/validation", (Func<IResult>)(() => throw new ValidationException("Invalid input")));
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [Theory]
    [InlineData("POST", "/products", "Name")]
    [InlineData("PUT", "/products", "Id")]
    [InlineData("DELETE", "/products/00000000-0000-0000-0000-000000000000", "Id")]
    [InlineData("GET", "/product/00000000-0000-0000-0000-000000000000", "Id")]
    [InlineData("GET", "/products/category/%20", "Category")]
    public async Task Invalid_requests_return_422_without_accessing_the_database(string method, string route, string field)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new Product
            {
                Id = Guid.Empty, Name = "", Description = "", Price = 0, Category = [], ImageFile = ""
            });

        using var response = await _client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.NotEmpty(json.RootElement.GetProperty("errors").GetProperty(field).EnumerateArray());
        Assert.Equal(0, _databaseCalls);
    }

    [Fact]
    public async Task A_valid_create_reaches_the_handler_and_returns_created()
    {
        using var response = await _client.PostAsJsonAsync("/products",
            new CreateProductRequest("Phone", "Description", 42, ["Phones"], "phone.png"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, _databaseCalls);
    }

    [Fact]
    public async Task A_valid_missing_product_still_returns_the_shared_404_response()
    {
        using var response = await _client.GetAsync($"/product/{Guid.NewGuid()}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Product.NotFound", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(1, _databaseCalls);
    }

    [Fact]
    public async Task Unexpected_handler_exceptions_return_a_safe_500_with_a_trace_id()
    {
        _throwOnDatabaseCall = true;
        using var response = await _client.GetAsync($"/product/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Server.Error", json.RootElement.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("Private database", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    [Fact]
    public async Task Bad_http_request_exceptions_keep_their_status_code()
    {
        using var response = await _client.GetAsync("/bad-request");
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Validation_exceptions_without_field_errors_still_return_422()
    {
        using var response = await _client.GetAsync("/validation");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
