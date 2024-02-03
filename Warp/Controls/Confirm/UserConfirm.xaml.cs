using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;
using Warp.Tools;

namespace Warp.Controls
{
    /// <summary>
    /// Interaction logic for BoxNetSelect.xaml
    /// </summary>
    public partial class UserConfirm : UserControl, INotifyPropertyChanged
    {
        private string PreviousModelName;
        
        public bool Confirm;
        public Options Options;
        public event Action Close;
        public event Action DeleteAndClose;
        public event PropertyChangedEventHandler PropertyChanged;

        private string _Info = "Info message";
        public string Info
        {
            get => _Info;
            set
            {
                if (_Info != value)
                {
                    _Info = value;
                    this.InfoLabel.Content = _Info;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Info)));
                }
            }
        }

        public UserConfirm(string dialogText, Options options)
        {
            InitializeComponent();
            DialogText.Text = dialogText;
            Options = options;
        }
        

        private void ButtonConfirm_OnClick(object sender, RoutedEventArgs e)
        {
            Confirm = true;
            Close?.Invoke();
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Confirm = false;
            Close?.Invoke();
        }
    }
}
