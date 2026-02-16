using MediatR;
using VRNotify.Domain.Configuration;

namespace VRNotify.Application.Configuration.Commands;

public sealed record ToggleDndCommand(DndMode Mode) : IRequest;

public sealed class ToggleDndCommandHandler : IRequestHandler<ToggleDndCommand>
{
    private readonly ISettingsRepository _repository;

    public ToggleDndCommandHandler(ISettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ToggleDndCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repository.LoadAsync(cancellationToken);
        var profile = settings.GetActiveProfile();
        profile.UpdateDnd(new DndSettings(request.Mode));
        await _repository.SaveAsync(settings, cancellationToken);
    }
}
