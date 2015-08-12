using System.Collections.Generic;
using Rebus.Snoop.ViewModel.Models;

namespace Rebus.Snoop.Events
{
    public class MessageSelectionWasMade
    {
        readonly IEnumerable<Message> selectedMessages;

        public MessageSelectionWasMade(IEnumerable<Message> selectedMessages)
        {
            this.selectedMessages = selectedMessages;
        }

        public IEnumerable<Message> SelectedMessages
        {
            get { return selectedMessages; }
        }
    }
}