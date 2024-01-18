using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using cryosparcClient;
using System.IO;
using System.ComponentModel;

namespace Warp.Controls.Confirm
{
    /// <summary>
    /// Interaction logic for CryosparcProjectPicker.xaml
    /// </summary>
    public partial class CryosparcProjectPicker : Window, INotifyPropertyChanged
    {
        private const string DefaultGlobalOptionsName = "global.settings";

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<CryosparcProject> CryosparcProjectsList { get; set; } = new ObservableCollection<CryosparcProject>();

        public string NewProjectName { get; set; } = "New project";

        public string ProjectName { get; set; }

        public string ProjectDir { get; set; }

        public string ProjectID { get; set; }

        public Options Options { get; set; }

        public GlobalOptions GlobalOptions { get; set; }

        public CryosparcProjectPicker(Options options)
        {
            GlobalOptions = new GlobalOptions();
            if (File.Exists(DefaultGlobalOptionsName))
                GlobalOptions.Load(DefaultGlobalOptionsName);
            Options = options;
            PopulateCryosparcProjectsList();
            InitializeComponent();
            Closing += Dialog_Closing;
            DataContext = this;
        }

        public async void PopulateCryosparcProjectsList()
        {
            var projectList = await Client.GetProjectsList(GlobalOptions.ClassificationUrl, GlobalOptions.CryosparcLicense);

            foreach (var project in projectList)
            {
                Console.WriteLine($"{project.ProjectName}, {project.ID}");
                CryosparcProjectsList.Add(project);
            }
            Console.WriteLine($"counted {CryosparcProjectsList.Count}");
        }

        private void Dialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var sel = CryosparcProjectsListView.SelectedItem as CryosparcProject;
            ProjectName = sel.ProjectName;
            ProjectID = sel.ID;
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async void createNewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Client.CryosparcIsOnline(GlobalOptions.ClassificationUrl)) { DialogResult = false; return; }
            var user = Client.GetCryosparcUserFromEmail(Options.Classification.CryosparcUserEmail, GlobalOptions.ClassificationUrl, GlobalOptions.CryosparcLicense);
            string apiUrl = "http://" + GlobalOptions.ClassificationUrl + ":39002/api";
            ProjectID = await Client.createEmptyProject(user, Options.Classification.ClassificationMountPoint, NewProjectName, apiUrl, GlobalOptions.CryosparcLicense);
            var response = await Client.GetOrCreateProjectDir(Options.Classification.ClassificationMountPoint, NewProjectName, user, GlobalOptions.ClassificationUrl, GlobalOptions.CryosparcLicense);
            ProjectName = NewProjectName;
            ProjectDir = response[0];
            DialogResult = true;
        }
    }

}
