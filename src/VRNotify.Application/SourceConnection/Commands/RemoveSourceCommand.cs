using MediatR;

namespace VRNotify.Application.SourceConnection.Commands;

public sealed record RemoveSourceCommand(Guid SourceId) : IRequest;

public sealed class RemoveSourceCommandHandler : IRequestHandler<RemoveSourceCommand>
{
    public Task Handle(RemoveSourceCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
