using System;

namespace Rebus.Sagas.Ids;

/// <summary>
/// Implementation of <see cref="ISagaDataIdFactory"/> that is useful when sagas are to be stored in SQL Server, because it uses
/// the last 6 bytes of each GUID to store a timestamp
/// </summary>
public class SqlServerSagaIdFactory : ISagaDataIdFactory
{
    /// <inheritdoc />
    public Guid NewId()
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);

        // SQL Server expects the timestamp to be in the last 6 bytes
        guidBytes[10] = timestamp[2];
        guidBytes[11] = timestamp[3];
        guidBytes[12] = timestamp[4];
        guidBytes[13] = timestamp[5];
        guidBytes[14] = timestamp[6];
        guidBytes[15] = timestamp[7];

        return new Guid(guidBytes);
    }
}