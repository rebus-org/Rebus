using System;
using System.IO;
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
        public DataBusAttachment(string id)
        {
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
        public Stream OpenRead()
        {
            var messageContext = MessageContext.Current;

            if (messageContext == null)
            {
                throw new InvalidOperationException("No message context is available - did you try to open a data bus attachment for reading OUTSIDE of a message handler?");
            }

            var storage = messageContext.IncomingStepContext
                .Load<IDataBusStorage>(DataBusIncomingStep.DataBusStorageKey);

            return storage.Read(Id);
        }
    }
}
