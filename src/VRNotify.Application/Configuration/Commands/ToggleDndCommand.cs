using MediatR;
using VRNotify.Domain.Configuration;

namespace VRNotify.Application.Configuration.Commands;

public sealed record ToggleDndCommand(DndMode Mode) : IRequest;

public sealed class ToggleDndCommandHandler : IRequestHandler<ToggleDndCommand>
{
    public Task Handle(ToggleDndCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
