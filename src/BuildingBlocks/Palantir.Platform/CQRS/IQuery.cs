using MediatR;

namespace Palantir.Platform.CQRS;

public interface IQuery<out TResponse> : IRequest<TResponse>;
