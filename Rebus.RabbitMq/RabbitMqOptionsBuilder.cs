using System;
using System.Collections.Generic;

namespace Rebus.RabbitMq
{
    /// <summary>
    /// Allows for fluently configuring RabbitMQ options
    /// </summary>
    public class RabbitMqOptionsBuilder
    {
        readonly Dictionary<string, string> _additionalClientProperties = new Dictionary<string, string>();

        /// <summary>
        /// Default name of the exchange of type DIRECT (used for point-to-point messaging)
        /// </summary>
        public const string DefaultDirectExchangeName = "RebusDirect";

        /// <summary>
        /// Default name of the exchange of type TOPIC (used for pub-sub)
        /// </summary>
        public const string DefaultTopicExchangeName = "RebusTopics";

        /// <summary>
        /// Configures which things to auto-declare and whether to bind the input queue. 
        /// Please note that you must be careful when you skip e.g. binding of the input queue as it may lead to lost messages
        /// if the direct binding is not established. 
        /// By default, two exchanges will be declared: one of the DIRECT type (for point-to-point messaging) and one of the
        /// TOPIC type (for pub-sub). Moreover, the endpoint's input queue will be declared, and a binding
        /// will be made from a topic of the same name as the input queue in the DIRECT exchange.
        /// </summary>
        public RabbitMqOptionsBuilder Declarations(bool declareExchanges = true, bool declareInputQueue = true, bool bindInputQueue = true)
        {
            DeclareExchanges = declareExchanges;
            DeclareInputQueue = declareInputQueue;
            BindInputQueue = bindInputQueue;
            return this;
        }

        /// <summary>
        /// Configures which names to use for the two types of necessary exchanges
        /// </summary>
        public RabbitMqOptionsBuilder ExchangeNames(
            string directExchangeName = DefaultDirectExchangeName,
            string topicExchangeName = DefaultTopicExchangeName)
        {
            if (directExchangeName == null) throw new ArgumentNullException(nameof(directExchangeName));
            if (topicExchangeName == null) throw new ArgumentNullException(nameof(topicExchangeName));

            if (directExchangeName == topicExchangeName)
            {
                throw new ArgumentException($"Exchange names for DIRECT and TOPIC are both set to '{directExchangeName}' - they must be different!");
            }

            DirectExchangeName = directExchangeName;
            TopicExchangeName = topicExchangeName;

            return this;
        }

        public RabbitMqOptionsBuilder AddClientProperties(IDictionary<string, string> additionalProperties)
        {
            foreach (var kvp in additionalProperties)
            {
                _additionalClientProperties[kvp.Key] = kvp.Value;
            }
            return this;
        }

        internal bool? DeclareExchanges { get; private set; }
        internal bool? DeclareInputQueue { get; private set; }
        internal bool? BindInputQueue { get; private set; }

        internal string DirectExchangeName { get; private set; }
        internal string TopicExchangeName { get; private set; }

        internal void Configure(RabbitMqTransport transport)
        {
            transport.AddClientProperties(_additionalClientProperties);

            if (DeclareExchanges.HasValue)
            {
                transport.SetDeclareExchanges(DeclareExchanges.Value);
            }

            if (DeclareInputQueue.HasValue)
            {
                transport.SetDeclareInputQueue(DeclareInputQueue.Value);
            }

            if (BindInputQueue.HasValue)
            {
                transport.SetBindInputQueue(BindInputQueue.Value);
            }

            if (DirectExchangeName != null)
            {
                transport.SetDirectExchangeName(DirectExchangeName);
            }

            if (TopicExchangeName != null)
            {
                transport.SetTopicExchangeName(TopicExchangeName);
            }
        }
    }
}