using MediatR;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Application.SourceConnection.Commands;

public sealed record AddSourceCommand(
    SourceType SourceType,
    string DisplayName,
    string PlainTextToken) : IRequest<Guid>;

public sealed class AddSourceCommandHandler : IRequestHandler<AddSourceCommand, Guid>
{
    public Task<Guid> Handle(AddSourceCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
