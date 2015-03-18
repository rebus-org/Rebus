using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Extensions.MessageOwnership
{
    /// <summary>
    /// Implementation of <see cref="IDetermineMessageOwnership"/> that uses other implementations
    /// of <see cref="IDetermineMessageOwnership"/> in the chained manner (chain-of-responsibility).
    ///
    /// It can be useful in some cases, for instance, when a custom implementation is combined with one
    /// of implementation shipped with Rebus so that latter is used for determining ownership of message
    /// types not handled by former.
    /// </summary>
    public class DetermineMessageOwnershipFromOtherDeterminers : IDetermineMessageOwnership
    {
        private readonly List<IDetermineMessageOwnership> messageOwnershipDeterminers;

        /// <summary>
        /// The series of <see cref="IDetermineMessageOwnership"/> implementations used in the chained manner.
        /// </summary>
        /// <param name="messageOwnershipDeterminers"></param>
        public DetermineMessageOwnershipFromOtherDeterminers(IEnumerable<IDetermineMessageOwnership> messageOwnershipDeterminers)
        {
            this.messageOwnershipDeterminers = messageOwnershipDeterminers.ToList();
        }

        /// <summary>
        /// Gets the name of the endpoint that is configured to be the owner of the specified message type.
        /// </summary>
        public string GetEndpointFor(Type messageType)
        {
            var innerExceptions = new List<Exception>();
            foreach (var messageOwnershipDeterminer in messageOwnershipDeterminers)
            {
                try
                {
                    var endpoint = messageOwnershipDeterminer.GetEndpointFor(messageType);
                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        return endpoint;
                    }
                }
                catch (Exception e)
                {
                    innerExceptions.Add(e);
                }
            }

            var message = string.Format(@"Could not find an endpoint mapping for the message type {0}.

Please ensure that you have mapped all message types, you wish to either Send or
Subscribe to, to an endpoint - a 'message owner' if you will.", messageType);

            throw new InvalidOperationException(message, new AggregateException(innerExceptions));
        }
    }
}