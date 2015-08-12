using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Snoop.ViewModel.Models
{
    public class EditableDictionary<TKey, TValue> : IEnumerable<EditableKeyValuePair<TKey, TValue>> where TKey : IEquatable<TKey>
    {
        readonly List<EditableKeyValuePair<TKey, TValue>> contents = new List<EditableKeyValuePair<TKey, TValue>>();

        public EditableDictionary()
        {
        }

        public EditableDictionary(IDictionary<TKey, TValue> headers)
        {
            foreach (var kvp in headers)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        public IEnumerator<EditableKeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(TKey key)
        {
            return contents.Any(i => i.Key.Equals(key));
        }

        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key)) throw new InvalidOperationException(string.Format("Dictionary already contains an item with the key {0}", key));

            contents.Add(new EditableKeyValuePair<TKey, TValue> { Key = key, Value = value });
        }

        public TValue this[TKey key]
        {
            get { return ContainsKey(key) ? contents.Single(c => c.Key.Equals(key)).Value : default(TValue); }
            set
            {
                var item = contents.FirstOrDefault(i => i.Key.Equals(key));
                if (item == null) Add(key, value);
                else item.Value = value;
            }
        }

        public EditableDictionary<TKey, TValue> Clone()
        {
            var dictionary = contents.ToDictionary(k => k.Key, v => v.Value);
            
            return new EditableDictionary<TKey, TValue>(dictionary);
        }
    }
}