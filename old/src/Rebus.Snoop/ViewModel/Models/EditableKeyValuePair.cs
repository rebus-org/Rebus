using System.ComponentModel;

namespace Rebus.Snoop.ViewModel.Models
{
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