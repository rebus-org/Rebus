using System;

namespace Rebus.Messages
{
    /// <summary>
    /// Header attribute that can be used to automatically add some specific header to all outgoing messages
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HeaderAttribute : Attribute
    {
        /// <summary>
        /// Gets the key of the header
        /// </summary>
        public string Key { get; private set; }
        
        /// <summary>
        /// Gets the value of the header
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Creates the header attribute with the given key and value
        /// </summary>
        public HeaderAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}