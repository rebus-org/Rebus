namespace Rebus.Snoop.ViewModel.Models
{
    public class Message : ViewModel
    {
        int bytes;
        string messageHeader;

        public string MessageHeader
        {
            get { return messageHeader; }
            set { SetValue("MessageHeader", value); }
        }

        public int Bytes
        {
            get { return bytes; }
            set { SetValue("Bytes", value); }
        }
    }
}