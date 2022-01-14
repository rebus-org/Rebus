using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.Pipeline;
using Rebus.Testing;

// ReSharper disable UnusedMember.Local

namespace Rebus.DataBus;

/// <summary>
/// Model that represents a data bus attachment. Only the <see cref="Id"/> is significant, as all the
/// other pieces of information are not required in order to retrieve the attachment from the database.
/// </summary>
[Serializable]
public class DataBusAttachment
{
    /// <summary>
    /// Creates a data bus attachment with the given ID
    /// </summary>
    public DataBusAttachment(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// Gets the ID of the attachment
    /// </summary>
    public string Id
    {
        get;
        protected set; // protected setter to make the JIL serializer happy
    }

    // ctor added to make the JIL serializer happy
    DataBusAttachment() { }

    /// <summary>
    /// Opens the attachment for reading, using the data bus of the bus that is handling the current message to read it.
    /// Is only available for calling inside message handlers.
    /// </summary>
    public async Task<Stream> OpenRead() => await OpenRead(Id);

    /// <summary>
    /// Gets the metadata associated with the attachment, using the data bus of the bus that is handling the current message to read it.
    /// Is only available for calling inside message handlers.
    /// </summary>
    public async Task<Dictionary<string, string>> GetMetadata() => await GetMetadata(Id);

    /// <summary>
    /// Opens the attachment for reading, using the data bus of the bus that is handling the current message to read it.
    /// Is only available for calling inside message handlers.
    /// </summary>
    public static async Task<Stream> OpenRead(string id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));

        var storage = GetDataBusStorage();

        return await storage.Read(id);
    }

    /// <summary>
    /// Gets the metadata associated with the attachment, using the data bus of the bus that is handling the current message to read it.
    /// Is only available for calling inside message handlers.
    /// </summary>
    public static async Task<Dictionary<string, string>> GetMetadata(string id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));

        var storage = GetDataBusStorage();

        return await storage.ReadMetadata(id);
    }

    static IDataBusStorage GetDataBusStorage()
    {
        return GetDataBusStorageForTesting()
               ?? GetDataBusStorageFromMessageContext();
    }

    static IDataBusStorage GetDataBusStorageForTesting()
    {
        return TestBackdoor.TestDataBusStorage;
    }

    static IDataBusStorage GetDataBusStorageFromMessageContext()
    {
        var messageContext = MessageContext.Current;

        if (messageContext == null)
        {
            const string message =
                "No message context is available - did you try to open a data bus attachment for reading OUTSIDE of a message handler?";

            throw new InvalidOperationException(message);
        }

        var storage = messageContext.IncomingStepContext
            .Load<IDataBusStorage>(DataBusIncomingStep.DataBusStorageKey);

        if (storage == null)
        {
            var message =
                $"Could not find data bus storage under the '{DataBusIncomingStep.DataBusStorageKey}' key in the current message context - did you remember to configure the data bus?";

            throw new InvalidOperationException(message);
        }

        return storage;
    }
}