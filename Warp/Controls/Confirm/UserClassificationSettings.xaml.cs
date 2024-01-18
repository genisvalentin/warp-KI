using cryosparcClient;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace Warp.Controls
{
    /// <summary>
    /// Interaction logic for UserClassificationSettings.xaml
    /// </summary>
    
    public partial class UserClassificationSettings : UserControl, INotifyPropertyChanged
    {
        public event Action Close;
        public event PropertyChangedEventHandler PropertyChanged;
        Options Options;

        public UserClassificationSettings(Options options)
        {
            Options = options;
            DataContext = Options.Classification;
            InitializeComponent();
            if (Options.Classification.SshKey != null)
            {
                ButtonClassificationSshKeyText.Text = Options.Classification.SshKey.Trim() == "" ? "Select Ssh key..." : Options.Classification.SshKey;
            }
        }

        private void ButtonMountPoint_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Confirm.SftExplorer(Options, Options.Classification.Server, Options.Classification.Port, Options.Classification.UserName, Options.Classification.SshKeyObject);

            // Display the dialog box and read the response
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                // User accepted the dialog box
                Options.Classification.ClassificationMountPoint = dialog.SelectedFile;
            }
        }

        private void CryosparcBrowserButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"http://{Options.Classification.Server}:39000");
        }

        private void ButtonCryosparcProject_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Confirm.CryosparcProjectPicker(Options);

            // Display the dialog box and read the response
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                // User accepted the dialog box
                Options.Classification.CryosparcProject = dialog.ProjectID;
                Options.Classification.CryosparcProjectName = dialog.ProjectName;
                Options.Classification.CryosparcProjectDir = dialog.ProjectDir;
            }
        }

        private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close?.Invoke();
            BindingOperations.ClearBinding(Class2DRadioButton, RadioButton.IsCheckedProperty);
            BindingOperations.ClearBinding(Class3DRadioButton, RadioButton.IsCheckedProperty);
            BindingOperations.ClearBinding(ClassificationManualRadioButton, RadioButton.IsCheckedProperty);
            BindingOperations.ClearBinding(ClassificationParticlesRadioButton, RadioButton.IsCheckedProperty);
            BindingOperations.ClearBinding(ClassificationHoursRadioButton, RadioButton.IsCheckedProperty);
            BindingOperations.ClearBinding(ClassificationImmediateRadioButton, RadioButton.IsCheckedProperty);
        }

        private void ButtonClassificationSshKey_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Ssh key|*",
                Multiselect = false
            };
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                Options.Classification.SshKey = Dialog.FileName;
                if (File.Exists(Options.Classification.SshKey)) {
                    ButtonClassificationSshKeyText.Text = Options.Classification.SshKey; 
                }
            }

        }
    }

    public class TextToDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return 0;
            int intValue;
            int.TryParse(value.ToString(), out intValue);
            return intValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }
    }

}
