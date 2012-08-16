using System.Configuration;

namespace Rebus.Configuration
{
    public class RijndaelSection : ConfigurationElement
    {
        const string KeyAttributeName = "key";

        [ConfigurationProperty(KeyAttributeName)]
        public string Key
        {
            get { return (string)this[KeyAttributeName]; }
            set { this[KeyAttributeName] = value; }
        }
    }
}