using MediatR;
using VRNotify.Domain.Configuration;

namespace VRNotify.Application.Configuration.Queries;

public sealed record GetSettingsQuery : IRequest<UserSettings>;

public sealed class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, UserSettings>
{
    private readonly ISettingsRepository _repository;

    public GetSettingsQueryHandler(ISettingsRepository repository)
    {
        _repository = repository;
    }

    public Task<UserSettings> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
        => _repository.LoadAsync(cancellationToken);
}
