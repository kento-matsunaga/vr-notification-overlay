using VRNotify.Domain.SourceConnection;

namespace VRNotify.Domain.Configuration;

public interface ICredentialStore
{
    Task<EncryptedCredential> EncryptAsync(string plainText, CancellationToken ct = default);
    Task<string> DecryptAsync(EncryptedCredential credential, CancellationToken ct = default);
}
