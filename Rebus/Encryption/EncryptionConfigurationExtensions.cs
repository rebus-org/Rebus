using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Encryption
{
    /// <summary>
    /// Configuration extensions for enabling encrypted message bodies
    /// </summary>
    public static class EncryptionConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to encrypt outgoing messages and be able to decrypt incoming messages. Please note that it's only the message bodies that are
        /// encrypted, thus everything included in the message headers will be visible to eavesdroppers.
        /// </summary>
        public static OptionsConfigurer EnableEncryption(this OptionsConfigurer configurer, string key)
        {
            configurer.Register(c => new Encryptor(key));

            configurer.Register(c => new EncryptMessagesOutgoingStep(c.Get<Encryptor>()));
            configurer.Register(c => new DecryptMessagesIncomingStep(c.Get<Encryptor>()));

            configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
                .OnReceive(c.Get<DecryptMessagesIncomingStep>(), PipelineRelativePosition.Before, typeof(DeserializeIncomingMessageStep))
                .OnSend(c.Get<EncryptMessagesOutgoingStep>(), PipelineRelativePosition.After, typeof(SerializeOutgoingMessageStep)));

            return configurer;
        }

    }
}