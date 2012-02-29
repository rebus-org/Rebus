using System.Configuration;

namespace Rebus.Configuration
{
    public class RijndaelSection : ConfigurationElement
    {
        const string IvAttributeName = "Iv";
        const string KeyAttributeName = "Key";

        [ConfigurationProperty(IvAttributeName)]
        public string Iv
        {
            get { return (string)this[IvAttributeName]; }
            set { this[IvAttributeName] = value; }
        }

        [ConfigurationProperty(KeyAttributeName)]
        public string Key
        {
            get { return (string)this[KeyAttributeName]; }
            set { this[KeyAttributeName] = value; }
        }
    }
}