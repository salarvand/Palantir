using MediatR;

namespace Palantir.Platform.CQRS;

public interface ICommand<out TResponse> : IRequest<TResponse>;

public interface ICommand : IRequest;
