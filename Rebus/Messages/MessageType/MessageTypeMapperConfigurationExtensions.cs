using System;
using System.Collections.Generic;
using System.Text;
using Rebus.Config;
using Rebus.Messages.MessageType;

namespace Rebus.Messages.MessageType
{
    /// <summary>
    /// Configuration extensions for configuring message type mapper 
    /// (i.e. when you cannot reuse same class between application)
    /// </summary>
    public static class MessageTypeMapperConfigurationExtensions
    {

        /// <summary>
        /// Configures Rebus to use message type mapper configurable
        /// </summary>
        public static MessageTypeMapperConfigurationBuilder MapMessageType(this StandardConfigurer<IMessageTypeMapper> configurer)
        {
            var builder = new MessageTypeMapperConfigurationBuilder();
            configurer.Register(c => builder.Build());
            return builder;
        }

        /// <summary>
        /// Configuration Builder for mapping message header type to a specific class
        /// Can be use when you cannot reuse same class. or using differente application codes
        /// </summary>
        public class MessageTypeMapperConfigurationBuilder
        {
            /// <summary>
            /// We use this way of storing configuration actions in order to preserve the order
            /// </summary>
            readonly List<Action<MessageTypeMapper>> _configurationActions = new List<Action<MessageTypeMapper>>();

            internal MessageTypeMapperConfigurationBuilder()
            {
            }

            /// <summary>
            /// Maps <paramref name="typename"/> as the "type" of the <typeparamref name="TMessage"/> message type
            /// </summary>
            public MessageTypeMapperConfigurationBuilder Map<TMessage>(string typename)
            {
                _configurationActions.Add(r => r.Map<TMessage>(typename));
                return this;
            }

            /// <summary>
            /// Maps <paramref name="typename"/> as "type" of the <paramref name="messageType"/> message type
            /// </summary>
            public MessageTypeMapperConfigurationBuilder Map(Type messageType, string typename)
            {
                _configurationActions.Add(r => r.Map(messageType, typename));
                return this;
            }

            internal MessageTypeMapper Build()
            {
                var router = new MessageTypeMapper();

                foreach (var action in _configurationActions)
                {
                    action(router);
                }

                return router;
            }
        }


    }
}