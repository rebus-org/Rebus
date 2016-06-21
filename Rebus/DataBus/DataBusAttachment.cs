using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Pipeline;

namespace Rebus.DataBus
{
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
            if (id == null) throw new ArgumentNullException(nameof(id));
            Id = id;
        }

        /// <summary>
        /// Gets the ID of the attachment
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Opens the attachment for reading, using the data bus of the bus that is handling the current message to read it.
        /// Is only available for calling inside message handlers.
        /// </summary>
        public Task<Stream> OpenRead()
        {
            return OpenRead(Id);
        }

        /// <summary>
        /// Opens the attachment for reading, using the data bus of the bus that is handling the current message to read it.
        /// Is only available for calling inside message handlers.
        /// </summary>
        public static Task<Stream> OpenRead(string id)
        {
            var messageContext = MessageContext.Current;

            if (messageContext == null)
            {
                throw new InvalidOperationException(
                    "No message context is available - did you try to open a data bus attachment for reading OUTSIDE of a message handler?");
            }

            var storage = messageContext.IncomingStepContext
                .Load<IDataBusStorage>(DataBusIncomingStep.DataBusStorageKey);

            if (storage == null)
            {
                throw new InvalidOperationException(
                    $"Could not find data bus storage under the '{DataBusIncomingStep.DataBusStorageKey}' key in the current message context - did you remember to configure the data bus?");
            }

            return storage.Read(id);
        }
    }
}
