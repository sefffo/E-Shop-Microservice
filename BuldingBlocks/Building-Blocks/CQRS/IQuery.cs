using MediatR;

namespace Building_Blocks.CQRS;
//for non-generic queries    
public interface IQuery : IQuery<Unit>
{
}

public interface IQuery<out TResponse> : IRequest<TResponse> where TResponse : notnull
{
}