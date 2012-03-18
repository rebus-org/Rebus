using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;

namespace Rebus.Snoop.Controls
{
    /// <summary>
    /// Interaction logic for LabelValue.xaml
    /// </summary>
    public partial class LabelValue : UserControl
    {
        public LabelValue()
        {
            InitializeComponent();
            DataContext = this;
            if (ViewModelBase.IsInDesignModeStatic)
            {
                Label = "Something";
                Text = "0.09 kW";
            }
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof (string), typeof (LabelValue), new PropertyMetadata(default(string)));

        public string Label
        {
            get { return (string) GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof (string), typeof (LabelValue), new PropertyMetadata(default(string)));

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
    }
}
