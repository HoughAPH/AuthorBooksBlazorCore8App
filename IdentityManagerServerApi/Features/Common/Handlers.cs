namespace IdentityManagerServerApi.Features.Common;

public interface ICommandHandler<in TCommand>
{
    Task<Result> Handle(TCommand command);
}

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query);
}
