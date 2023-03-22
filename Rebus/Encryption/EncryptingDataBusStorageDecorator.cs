using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.DataBus;
using Rebus.Extensions;

namespace Rebus.Encryption;

/// <summary>
/// Decorator of <see cref="IDataBusStorage"/> that encrypts/decrypts data
/// </summary>
public class EncryptingDataBusStorageDecorator : IDataBusStorage
{
    readonly IDataBusStorage _dataBusStorage;
    readonly IAsyncEncryptor _asyncEncryptor;

    /// <summary>
    /// Creates the decorator
    /// </summary>
    public EncryptingDataBusStorageDecorator(IDataBusStorage dataBusStorage, IAsyncEncryptor asyncEncryptor)
    {
        _dataBusStorage = dataBusStorage ?? throw new ArgumentNullException(nameof(dataBusStorage));
        _asyncEncryptor = asyncEncryptor ?? throw new ArgumentNullException(nameof(asyncEncryptor));
    }

    /// <inheritdoc />
    public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
    {
        var metadataToSave = metadata?.Clone() ?? new Dictionary<string, string>();

        using var target = new MemoryStream();
        await source.CopyToAsync(target);

        var data = await _asyncEncryptor.Encrypt(target.ToArray());

        using var encryptedSource = new MemoryStream(data.Bytes);

        metadataToSave[MetadataKeys.ContentEncryption] = _asyncEncryptor.ContentEncryptionValue;
        metadataToSave[MetadataKeys.ContentInitializationVector] = Convert.ToBase64String(data.Iv);

        if (!string.IsNullOrEmpty(data.KeyId))
        {
            metadataToSave[MetadataKeys.ContentEncryptionKeyId] = data.KeyId;
        }

        await _dataBusStorage.Save(id, encryptedSource, metadataToSave);
    }

    /// <inheritdoc />
    public async Task<Stream> Read(string id)
    {
        var metadata = await _dataBusStorage.ReadMetadata(id);

        if (!metadata.TryGetValue(MetadataKeys.ContentEncryption, out var contentEncoding))
        {
            return await _dataBusStorage.Read(id);
        }

        if (!string.Equals(contentEncoding, _asyncEncryptor.ContentEncryptionValue, StringComparison.OrdinalIgnoreCase))
        {
            // unknown content encoding - the user must know best how to decode this!
            return await _dataBusStorage.Read(id);
        }

        if (!metadata.TryGetValue(MetadataKeys.ContentInitializationVector, out var iv))
        {
            throw new ArgumentException($"Cannot decrypt data from attachment with ID '{id}' - the '{MetadataKeys.ContentEncryption}' metadata key had the value '{_asyncEncryptor.ContentEncryptionValue}', which triggered decryption of the data, but there was no initialization vector to be found as the '{MetadataKeys.ContentInitializationVector}' metadata header");
        }

        using var source = await _dataBusStorage.Read(id);
        using var encryptedBytes = new MemoryStream();
        await source.CopyToAsync(encryptedBytes);

        var encryptedData = metadata.TryGetValue(MetadataKeys.ContentEncryptionKeyId, out var keyId)
            ? new EncryptedData(encryptedBytes.ToArray(), Convert.FromBase64String(iv), keyId)
            : new EncryptedData(encryptedBytes.ToArray(), Convert.FromBase64String(iv));

        var decryptedBytes = await _asyncEncryptor.Decrypt(encryptedData);

        return new MemoryStream(decryptedBytes);
    }

    /// <inheritdoc />
    public Task<Dictionary<string, string>> ReadMetadata(string id) => _dataBusStorage.ReadMetadata(id);
}