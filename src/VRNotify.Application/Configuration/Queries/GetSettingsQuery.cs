using MediatR;
using VRNotify.Domain.Configuration;

namespace VRNotify.Application.Configuration.Queries;

public sealed record GetSettingsQuery : IRequest<UserSettings>;

public sealed class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, UserSettings>
{
    public Task<UserSettings> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
