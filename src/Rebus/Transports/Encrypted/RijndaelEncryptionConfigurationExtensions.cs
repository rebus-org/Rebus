using System.Configuration;
using Rebus.Transports.Encrypted;

namespace Rebus.Configuration
{
    public static class RijndaelEncryptionConfigurationExtensions
    {
        public static void EncryptMessageBodies(this DecoratorsConfigurer configurer, string keyBase64)
        {
            configurer.AddDecoration(b =>
                {
                    var sendMessages = b.SendMessages;
                    var receiveMessages = b.ReceiveMessages;

                    var decorator = new RijndaelEncryptionTransportDecorator(sendMessages, receiveMessages, keyBase64);

                    b.SendMessages = decorator;
                    b.ReceiveMessages = decorator;
                });
        }

        public static void EncryptMessageBodies(this DecoratorsConfigurer configurer)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                var rijndael = section.RijndaelSection;

                if (rijndael == null)
                {
                    throw new ConfigurationErrorsException(string.Format(@"Could not find encryption settings in Rebus configuration section. Did you forget the 'rijndael' element?

{0}", GenerateRijndaelHelp()));
                }

                if (string.IsNullOrEmpty(rijndael.Key))
                {
                    throw new ConfigurationErrorsException(string.Format(@"Could not find key settings in Rijndael element - did you forget the 'key' attribute?

{0}", GenerateRijndaelHelp()));
                }

                EncryptMessageBodies(configurer, rijndael.Key);
            }
            catch (ConfigurationErrorsException e)
            {
                throw new ConfigurationException(
                    @"
An error occurred when trying to parse out the configuration of the RebusConfigurationSection:

{0}

-

For this way of configuring input queue to work, you need to supply a correct configuration
section declaration in the <configSections> element of your app.config/web.config - like so:

    <configSections>
        <section name=""rebus"" type=""Rebus.Configuration.RebusConfigurationSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <rebus> element some place further down the app.config/web.config,
like so:

    <rebus InputQueue=""my.service.input.queue"">
        <rijndael iv=""base64 encoded initialization vector"" key=""base64 encoded key""/>
    </rebus>

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }
        }

        static string GenerateRijndaelHelp()
        {
            return
                string.Format(
                    @"To help you get started, here's a valid rijndael element, including a fresh and newly generated
key - and yes, I promise that it has been generated just this moment :) you can try running your app
again if you don't believe me.

    <rijndael key=""{0}""/>

The key has been generated with the biggest valid size, so it should be pretty secure.
",
                    RijndaelEncryptionTransportDecorator.GenerateKeyBase64());
        }
    }
}