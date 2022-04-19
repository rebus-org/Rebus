using System.Threading.Tasks;

namespace Rebus.Encryption;


/// <summary>
/// Describes a provider capable of returning keys used for encrypting and decrypting messages 
/// </summary>
public interface IEncryptionKeyProvider
{
    /// <summary>
    /// Returns the default key that should be used for encryption 
    /// </summary>
    /// <returns>An<see cref="EncryptionKey"/> containing the key and its identifier</returns>
    public Task<EncryptionKey> GetCurrentKey();
    
    /// <summary>
    /// Returns an <see cref="EncryptionKey"/> if the provider is capable finding one matching the <paramref name="identifier"/>. 
    /// </summary>
    /// <param name="identifier">The identifier unique for this key</param>
    /// <returns>An<see cref="EncryptionKey"/> containing the key and its identifier</returns>
    public Task<EncryptionKey> GetSpecificKey(string identifier);
}