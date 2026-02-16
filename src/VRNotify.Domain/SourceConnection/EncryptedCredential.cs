namespace VRNotify.Domain.SourceConnection;

public sealed record EncryptedCredential(byte[] EncryptedData, byte[] Entropy);
