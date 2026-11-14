using System.Text.Json;
using Building_Blocks.Extensions;
using Building_Blocks.SharedResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalog.Tests;

public class ResultTests
{
    [Fact]
    public void Success_contains_a_value_and_no_errors()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(42, result.GetValueOrThrow());
    }

    [Fact]
    public void Failure_contains_errors_and_cannot_expose_a_value()
    {
        var error = Error.NotFound("Product.NotFound", "Product was not found.");
        Result<int> result = error;

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(error, Assert.Single(result.Errors));
        Assert.Throws<InvalidOperationException>(() => result.GetValueOrThrow());
    }

    [Fact]
    public void Failure_requires_at_least_one_non_null_error()
    {
        Assert.Throws<ArgumentException>(() => Result.Fail(Array.Empty<Error>()));
        Assert.Throws<ArgumentException>(() => Result<int>.Fail(Array.Empty<Error>()));
        Assert.Throws<ArgumentException>(() => Result.Fail((Error)null!));
        Assert.Throws<ArgumentException>(() => Result<int>.Fail((Error)null!));
        Assert.Throws<ArgumentNullException>(() => Result.Fail((IEnumerable<Error>)null!));
    }

    [Fact]
    public void Failure_copies_errors_and_does_not_allow_mutation()
    {
        var errors = new List<Error> { Error.BadRequest("Request.Invalid", "Invalid request.") };
        var result = Result<int>.Fail(errors);
        errors.Clear();

        Assert.Single(result.Errors);
        Assert.Throws<NotSupportedException>(() => ((IList<Error>)result.Errors).Clear());
    }

    [Theory]
    [InlineData(ErrorType.Failure, 500)]
    [InlineData(ErrorType.Validation, 422)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.InternalServerError, 500)]
    [InlineData(ErrorType.BadRequest, 400)]
    [InlineData(ErrorType.InvalidCredentials, 401)]
    public async Task Errors_map_to_http_status_codes(ErrorType type, int statusCode)
    {
        var error = type switch
        {
            ErrorType.Validation => Error.Validation("Test.Error", "Description"),
            ErrorType.NotFound => Error.NotFound("Test.Error", "Description"),
            ErrorType.Unauthorized => Error.Unauthorized("Test.Error", "Description"),
            ErrorType.Forbidden => Error.Forbidden("Test.Error", "Description"),
            ErrorType.InternalServerError => Error.InternalServerError("Test.Error", "Description"),
            ErrorType.BadRequest => Error.BadRequest("Test.Error", "Description"),
            ErrorType.InvalidCredentials => Error.InvalidCredentials("Test.Error", "Description"),
            _ => Error.Failure("Test.Error", "Description")
        };
        var called = false;
        var response = await Execute(Result<int>.Fail(error).ToHttpResult(value =>
        {
            called = true;
            return Results.Ok(value);
        }));

        Assert.Equal(statusCode, response.StatusCode);
        Assert.False(called);
        Assert.StartsWith("application/problem+json", response.ContentType);
    }

    [Fact]
    public async Task Validation_groups_all_messages_by_code()
    {
        var result = Result.Fail(new[]
        {
            Error.Validation("Name", "Name is required."),
            Error.Validation("Name", "Name is too short."),
            Error.Validation("Price", "Price must be positive.")
        });
        var response = await Execute(result.ToHttpResult());
        using var json = JsonDocument.Parse(response.Body);
        var errors = json.RootElement.GetProperty("errors");

        Assert.Equal(422, response.StatusCode);
        Assert.Equal(2, errors.GetProperty("Name").GetArrayLength());
        Assert.Equal("Price must be positive.", errors.GetProperty("Price")[0].GetString());
    }

    [Fact]
    public async Task Mixed_errors_use_the_first_status_and_preserve_every_error()
    {
        var result = Result.Fail(new[]
        {
            Error.NotFound("Product.NotFound", "Product was not found."),
            Error.Validation("Name", "Name is required.")
        });
        var response = await Execute(result.ToHttpResult());
        using var json = JsonDocument.Parse(response.Body);

        Assert.Equal(404, response.StatusCode);
        Assert.Equal("Product.NotFound", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(2, json.RootElement.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task Success_without_a_value_returns_no_content()
    {
        var response = await Execute(Result.Ok().ToHttpResult());

        Assert.Equal(204, response.StatusCode);
        Assert.Empty(response.Body);
    }

    [Fact]
    public async Task Generic_success_returns_the_value_as_json()
    {
        var response = await Execute(Result<int>.Ok(42).ToHttpResult());

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("42", response.Body);
    }

    [Fact]
    public async Task Custom_success_can_return_created_with_a_location()
    {
        var response = await Execute(Result<int>.Ok(42)
            .ToHttpResult(value => Results.Created($"/products/{value}", new { Id = value })));

        Assert.Equal(201, response.StatusCode);
        Assert.Equal("/products/42", response.Location);
        Assert.Equal("{\"id\":42}", response.Body);
    }

    private static async Task<(int StatusCode, string? ContentType, string Body, string? Location)> Execute(IResult result)
    {
        using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await result.ExecuteAsync(context);
        body.Position = 0;
        var json = await new StreamReader(body).ReadToEndAsync();

        return (context.Response.StatusCode, context.Response.ContentType, json, context.Response.Headers.Location);
    }
}
