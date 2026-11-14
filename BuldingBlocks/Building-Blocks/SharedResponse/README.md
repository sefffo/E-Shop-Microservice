# Shared results

Adapted from [ConnectO's SharedResponse](https://github.com/sefffo/ConnectO/tree/main/Social-Media-Chatting-APP-SharedLibrary/SharedResponse) for CQRS handlers and Carter endpoints.

Handlers return `Result<T>` for expected success/failure outcomes. Return a value or an `Error` directly using implicit conversions:

```csharp
public record GetProductByIdQuery(Guid Id) : IQuery<Result<GetProductResult>>;

// Inside the handler:
if (product is null)
    return ProductErrors.NotFound(query.Id);

return new GetProductResult(product);
```

Endpoints delegate HTTP conversion to `Building_Blocks.Extensions.ResultExtensions`:

```csharp
var result = await sender.Send(new GetProductByIdQuery(id));
return result.ToHttpResult(value => Results.Ok(value.Adapt<GetProductByIdResponse>()));
```

`Result<T>.Ok(value)` and `Result<T>.Fail(error)` are also available. Pass multiple errors to `Fail(IEnumerable<Error>)`. A failed result must have at least one error; its value cannot be read. Error collections are copied and read-only.

`Result.Ok().ToHttpResult()` returns 204. Generic success defaults to 200 with the value as the body. The optional success callback supports response DTO mapping and 201 responses with a Location header. Existing product success bodies are preserved.

| Error type | HTTP status |
| --- | --- |
| Validation | 422 |
| BadRequest | 400 |
| Unauthorized / InvalidCredentials | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Failure / InternalServerError | 500 |

Failures use Problem Details. Validation errors are grouped by code with all descriptions retained. Mixed error collections use the first error's status and include every error in the response. Expected application failures use results. `AddBuildingBlocks(assembly)` registers FluentValidation and the MediatR logging/validation behaviors, plus `CustomExceptionHandler` through `AddExceptionHandler`. Call `app.UseExceptionHandler()` before mapping endpoints.

Each product slice has a validator. Create/update require a name, description, positive price, image file, and at least one non-empty category; ID queries/commands reject empty IDs. Category queries reject blank categories. The get-all query currently has no input rules.

The pipeline validates requests before handlers execute. Validation exceptions are converted to the same 422 response by the exception handler. Unexpected exceptions are logged and return a generic 500 response with a trace ID; exception details stay in server logs. Request logging records the request type and elapsed time without serializing request data.

Run the result and Catalog regression tests with:

```shell
dotnet test Tests/Catalog.Tests/Catalog.Tests.csproj
```
