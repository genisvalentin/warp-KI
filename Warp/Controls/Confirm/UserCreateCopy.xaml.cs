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
    public partial class UserCreateCopy : UserControl, INotifyPropertyChanged
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


        public UserCreateCopy(string dialogText, string basePath, Options options)
        {
            InitializeComponent();
            DialogText.Text = dialogText;
            Options = options;
            NewName = "";
            BasePath = basePath;
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
            if (!txt.EndsWith(".mdoc")) {
                FileNameOk = false;
                UserMessage = "New name must have mdoc extension";
                return;
            }
            if (File.Exists(System.IO.Path.Combine(BasePath, txt)))
            {
                FileNameOk = false;
                UserMessage = String.Format("File {0} already exists", txt);
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
