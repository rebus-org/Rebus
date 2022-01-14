using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Encryption;

/// <summary>
/// Configuration extensions for enabling encrypted message bodies
/// </summary>
public static class EncryptionConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to encrypt outgoing messages and be able to decrypt incoming messages. 
    /// Uses the default "Rijndael" algorithm which is 256 bit AES encryption.
    /// Please note that it's only the message bodies that are encrypted, thus everything included in the message headers will be visible to eavesdroppers.
    /// </summary>
    public static void EnableEncryption(this OptionsConfigurer configurer, string key)
    {
        EnableCustomEncryption(configurer).Register(c => new RijndaelEncryptor(key));
    }

    /// <summary>
    /// Configures Rebus to encrypt outgoing messages and be able to decrypt incoming messages using custom encryption provider.
    /// Please note that it's only the message bodies that are encrypted, thus everything included in the message headers will be visible to eavesdroppers.
    /// Custom encrypotion providers are configured by building on the returned configurer, e.g. like so:
    /// <code>
    /// Configure.With(...)
    ///     .(...)
    ///     .Options(o => {
    ///         o.EnableCustomEncryption()
    ///             .Use***();
    ///     })
    ///     .Start();
    /// </code>
    /// </summary>
    public static StandardConfigurer<IEncryptor> EnableCustomEncryption(this OptionsConfigurer configurer)
    {
        configurer.EnableCustomAsyncEncryption().Register(c => new DefaultAsyncEncryptor(c.Get<IEncryptor>()));
            
        return StandardConfigurer<IEncryptor>.GetConfigurerFrom(configurer);
    }
        
    /// <inheritdoc cref="EnableCustomEncryption" />
    public static StandardConfigurer<IAsyncEncryptor> EnableCustomAsyncEncryption(this OptionsConfigurer configurer)
    {
        configurer.Register(c => new EncryptMessagesOutgoingStep(c.Get<IAsyncEncryptor>()));
        configurer.Register(c => new DecryptMessagesIncomingStep(c.Get<IAsyncEncryptor>()));

        configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
            .OnReceive(c.Get<DecryptMessagesIncomingStep>(), PipelineRelativePosition.Before, typeof(DeserializeIncomingMessageStep))
            .OnSend(c.Get<EncryptMessagesOutgoingStep>(), PipelineRelativePosition.After, typeof(SerializeOutgoingMessageStep)));

        return StandardConfigurer<IAsyncEncryptor>.GetConfigurerFrom(configurer);
    }
}