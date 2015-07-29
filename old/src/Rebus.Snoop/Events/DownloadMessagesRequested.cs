using System.Collections.Generic;
using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class DownloadMessagesRequested
    {
        readonly List<Message> messagesToDownload;

        public DownloadMessagesRequested(List<Message> messagesToDownload)
        {
            this.messagesToDownload = messagesToDownload;
        }

        public List<Message> MessagesToDownload
        {
            get { return messagesToDownload; }
        }
    }
}