using VRNotify.Domain.Configuration;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Infrastructure.Security;

public sealed class DpapiCredentialStore : ICredentialStore
{
    public Task<EncryptedCredential> EncryptAsync(string plainText, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<string> DecryptAsync(EncryptedCredential credential, CancellationToken ct = default)
        => throw new NotImplementedException();
}
