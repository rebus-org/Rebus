using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rebus.Snoop.Annotations;

namespace Rebus.Snoop.ViewModel.Models
{
    public class Message : ViewModel
    {
#pragma warning disable 649
        readonly EditableDictionary<string, string> headers = new EditableDictionary<string, string>();
        readonly string bodyPropertyName;
        string body;
        int bytes;
        string id;
        string label;
        string queuePath;
        DateTime time;
        bool bodyChanged;
        bool couldDeserializeBody;
        bool couldDeserializeHeaders;
#pragma warning restore 649

        public Message()
        {
            PropertyChanged += TrackBodyChanged;
            bodyPropertyName = ExtractPropertyName(() => Body);
        }

        void TrackBodyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != bodyPropertyName) return;

            BodyChanged = true;
        }

        public void ResetDirtyFlags()
        {
            BodyChanged = false;
        }

        public string Body
        {
            get { return body; }
            set { SetValue(() => Body, value); }
        }

        public bool BodyChanged
        {
            get { return bodyChanged; }
            set { SetValue(() => BodyChanged, value); }
        }

        public EditableDictionary<string, string> Headers
        {
            get { return headers; }
            set
            {
                SetValue(() => Headers, value,
                    ExtractPropertyName(() => HeadersExceptError),
                    ExtractPropertyName(() => ErrorDetails));
            }
        }

        public IEnumerable<EditableKeyValuePair<string, string>> HeadersExceptError
        {
            get { return Headers.Where(h => h.Key != Shared.Headers.ErrorMessage).ToArray(); }
        }

        public string Label
        {
            get { return label; }
            set { SetValue(() => Label, value); }
        }

        public int Bytes
        {
            get { return bytes; }
            set { SetValue(() => Bytes, value); }
        }

        public DateTime Time
        {
            get { return time; }
            set { SetValue(() => Time, value); }
        }

        public string Id
        {
            get { return id; }
            set { SetValue(() => Id, value); }
        }

        public string QueuePath
        {
            get { return queuePath; }
            set { SetValue(() => QueuePath, value); }
        }

        public string ErrorDetails
        {
            get
            {
                return Headers.ContainsKey(Shared.Headers.ErrorMessage)
                           ? Headers[Shared.Headers.ErrorMessage]
                           : null;
            }
        }

        public bool CouldDeserializeBody
        {
            get { return couldDeserializeBody; }
            set { SetValue(() => CouldDeserializeBody, value); }
        }

        public bool CouldDeserializeHeaders
        {
            get { return couldDeserializeHeaders; }
            set { SetValue(() => CouldDeserializeHeaders, value); }
        }
    }

    public class EditableDictionary<TKey, TValue> : IEnumerable<EditableKeyValuePair<TKey, TValue>> where TKey : IEquatable<TKey>
    {
        readonly List<EditableKeyValuePair<TKey, TValue>> contents = new List<EditableKeyValuePair<TKey, TValue>>();

        public EditableDictionary()
        {
        }

        public EditableDictionary(Dictionary<TKey, TValue> headers)
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
    }

    public class EditableKeyValuePair<TKey, TValue> : INotifyPropertyChanged
    {
        TKey key;
        TValue value;

        public TKey Key
        {
            get { return key; }
            set
            {
                key = value;
                OnPropertyChanged("Key");
            }
        }

        public TValue Value
        {
            get { return value; }
            set
            {
                this.value = value;
                OnPropertyChanged("Value");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}