using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class UserChangeDisplayName : UserControl, INotifyPropertyChanged
    {
        private string PreviousModelName;

        public bool Confirm;
        public Options Options;
        private string _newName;

        public event Action Close;
        public event PropertyChangedEventHandler PropertyChanged;

        System.Windows.Threading.DispatcherTimer TickTimer = new System.Windows.Threading.DispatcherTimer();
        private bool _fileNameOk;
        private string _userMessage;

        public string TextBoxContent { get; set; }

        public string NewName
        {
            get => _newName;
            set
            {
                if (value != _newName)
                {
                    _newName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewName)));
                }
            }
        }

        public bool FileNameOk
        {
            get => _fileNameOk;
            set
            {
                if (value != _fileNameOk)
                {
                    _fileNameOk = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileNameOk)));
                }
            }
        }

        public string BasePath { get; set; }

        public string UserMessage
        {
            get => _userMessage;
            set
            {
                if (_userMessage != value)
                {
                    _userMessage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserMessage)));
                }
            }
        }

        List<string> OtherNames = new List<string>();

        public UserChangeDisplayName(string dialogText, string basePath, Options options)
        {
            InitializeComponent();
            DialogText.Text = dialogText;
            Options = options;
            NewName = "";
            BasePath = basePath;
            
            if (Options.TiltSeries.TiltSeriesList.Count > 0)
            {
                foreach (var ts in Options.TiltSeries.TiltSeriesList)
                {
                    OtherNames.Add(ts.DisplayName);
                }
            }

            TickTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            TickTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            TickTimer.Start();

        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            TickTimer.Stop();
            TextBox textbox = this.FindName("NewNameBox") as TextBox;
            var txt = textbox.Text;

            if (txt == "")
            {
                FileNameOk = false;
                return;
            }

            if (OtherNames.Contains(txt)) {
                FileNameOk = false;
                UserMessage = "Another tilt series with the same name already exists";
                return;
            }
            UserMessage = "";
            FileNameOk = true;
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

        private void textChangedEventHandler(object sender, TextChangedEventArgs e)
        {
            TickTimer.Start();
        }
    }
}
