using System;
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

        public string ReturnAddress
        {
            get
            {
                return Headers.ContainsKey(Shared.Headers.ReturnAddress)
                           ? Headers[Shared.Headers.ReturnAddress]
                           : "";
            }
        }

        public Message Clone()
        {
            return new Message
                       {
                           Headers = Headers.Clone(),
                           Body = Body,
                           Bytes = Bytes,
                           Id = "(reload queue contents to get id)",
                           CouldDeserializeBody = CouldDeserializeBody,
                           CouldDeserializeHeaders = CouldDeserializeHeaders,
                           QueuePath = QueuePath,
                           Label = Label,
                           Time = Time,
                       };
        }
    }
}