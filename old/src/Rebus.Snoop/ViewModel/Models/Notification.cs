namespace Rebus.Snoop.ViewModel.Models
{
    public class Notification
    {
        readonly string headline;
        readonly string fullText;

        public Notification(string headline)
            : this(headline, null)
        {
        }

        public Notification(string headline, string fullText)
        {
            this.headline = headline;
            this.fullText = fullText;
        }

        public string Headline
        {
            get { return headline; }
        }

        public string FullText
        {
            get { return fullText; }
        }
    }
}