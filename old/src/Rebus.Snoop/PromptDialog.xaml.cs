using System.ComponentModel;
using System.Windows;
using Rebus.Snoop.Annotations;

namespace Rebus.Snoop
{
    /// <summary>
    /// Interaction logic for PromptDialog.xaml
    /// </summary>
    public partial class PromptDialog : INotifyPropertyChanged
    {
        string promptText;
        string resultText;

        public PromptDialog()
        {
            PromptText = "Please enter destination queue name (e.g. 'someQueue@someMachine', or just 'someQueue' for a local queue):";
            InitializeComponent();
            DataContext = this;

            Loaded += (w, ea) => InputTextBox.Focus();
        }

        public string PromptText
        {
            get { return promptText; }
            set
            {
                promptText = value;
                OnPropertyChanged("PromptText");
            }
        }

        public string ResultText
        {
            get { return resultText; }
            set
            {
                resultText = value;
                OnPropertyChanged("ResultText");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        void OkClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void CancelClicked(object sender, RoutedEventArgs e)
        {
            ResultText = null;
            Close();
        }
    }
}
