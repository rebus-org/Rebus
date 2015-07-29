using System.Configuration;

namespace Rebus.Configuration
{
    /// <summary>
    /// App.config configuration section that allows for configuring an encryption key to be used
    /// by the Rijndael encryption algorithm
    /// </summary>
    public class RijndaelSection : ConfigurationElement
    {
        const string KeyAttributeName = "key";

        /// <summary>
        /// Gets the configured key
        /// </summary>
        [ConfigurationProperty(KeyAttributeName, IsRequired = true)]
        public string Key
        {
            get { return (string)this[KeyAttributeName]; }
            set { this[KeyAttributeName] = value; }
        }
    }
}