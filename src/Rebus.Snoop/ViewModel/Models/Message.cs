using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Snoop.ViewModel.Models
{
    public class Message : ViewModel
    {
#pragma warning disable 649
        readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        string body;
        int bytes;
        string id;
        string label;
        string queuePath;
        DateTime time;
#pragma warning restore 649

        public string Body
        {
            get { return body; }
            set { SetValue(() => Body, value); }
        }

        public Dictionary<string, string> Headers
        {
            get { return headers; }
            set
            {
                SetValue(() => Headers, value,
                    ExtractPropertyName(() => HeadersExceptError),
                    ExtractPropertyName(() => ErrorDetails));
            }
        }

        public IEnumerable<KeyValuePair<string, string>> HeadersExceptError
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
    }
}