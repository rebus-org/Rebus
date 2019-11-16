using System;
using System.Collections.Generic;
using System.Text;
using Rebus.Config;
using Rebus.Messages.MessageType;

namespace Rebus.Messages.MessageType
{

    public static class MessageTypeMapperConfigurationExtensions
    {

        public static void MapMessageType(this StandardConfigurer<IMessageTypeMapper> configurer, Action<MessageTypeMapper> mappingAction)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Register(c =>
            {
                var messagetypeConvetion = new MessageTypeMapper();
                mappingAction(messagetypeConvetion);
                return messagetypeConvetion;
            });
        }

        /// <summary>
        /// Configures Rebus to use type-based routing
        /// </summary>
        public static MessageTypeMapperConfigurationBuilder MapMessageType(this StandardConfigurer<IMessageTypeMapper> configurer)
        {
            var builder = new MessageTypeMapperConfigurationBuilder();
            configurer.Register(c => builder.Build());
            return builder;
        }

        /// <summary>
        /// Type-based routing configuration builder that can be called fluently to map message types to their owning endpoints
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
            /// Maps <paramref name="destinationAddress"/> as the owner of the <typeparamref name="TMessage"/> message type
            /// </summary>
            public MessageTypeMapperConfigurationBuilder Map<TMessage>(string destinationAddress)
            {
                _configurationActions.Add(r => r.Map<TMessage>(destinationAddress));
                return this;
            }

            /// <summary>
            /// Maps <paramref name="destinationAddress"/> as the owner of the <paramref name="messageType"/> message type
            /// </summary>
            public MessageTypeMapperConfigurationBuilder Map(Type messageType, string destinationAddress)
            {
                _configurationActions.Add(r => r.Map(messageType, destinationAddress));
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