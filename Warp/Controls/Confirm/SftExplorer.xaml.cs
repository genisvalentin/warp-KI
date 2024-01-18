using System;
using System.Collections.Generic;
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
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Warp.Controls.Confirm
{
    /// <summary>
    /// Interaction logic for SftExplorer.xaml
    /// </summary>
    public partial class SftExplorer : Window
    {
        SftpClient client;
        Options Options;
        public string SelectedFile { get; set; }
        public SftExplorer(Options options, string host, int port, string username, PrivateKeyFile key)
        {
            Options = options;
            InitializeComponent();
            GetRemoteFiles(host, port, username,key);
            Closing += Dialog_Closing;
        }

        public void GetRemoteFiles(string host, int port, string username, PrivateKeyFile key)
        {
            if (host.Contains("@"))
            {
                var spilt = host.Split('@');
                host = spilt[1];
            }

            Console.WriteLine($"sftpclient {host} {port} {username} {key}");
            client = new SftpClient(host, port, username, key);
            isLoading.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                try
                {
                    client.Connect();
                    var path = client.ListDirectory("/").ToList().Where(x => !x.FullName.Split('/').Last().StartsWith(".")).Where(x => x.IsDirectory);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var file in path)
                        {
                            var head = new TreeViewItem();
                            head.Tag = file;
                            head.Header = file.Name;
                            //head.PreviewMouseDoubleClick += file_MouseDoubleClick;
                            head.Selected += file_Selected;
                            if (file.IsDirectory == true)
                            {
                                head.Expanded += Item_Expanded;
                                var tempItem = new TreeViewItem();
                                tempItem.Tag = "null";
                                head.Items.Add(tempItem);
                            }
                            RFiles.Items.Add(head);
                        }
                    });
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => isLoading.Visibility = Visibility.Collapsed);
                }
            });
        }

        private void Item_Expanded(object sender, RoutedEventArgs e)
        {
            var item = sender as TreeViewItem;
            var fileInfo = item.Tag as SftpFile;

            if (item.Items.Count > 0)
            {
                var tempres = item.Items[0] as TreeViewItem;
                if (tempres?.Tag?.ToString() == "null")
                {
                    item.Items.RemoveAt(0);

                    Task.Run(() =>
                    {
                        try
                        {
                            var path = client.ListDirectory(fileInfo.FullName).ToList().Where(x => !x.FullName.Split('/').Last().StartsWith(".")).Where(x => x.IsDirectory).Where(x => x.FullName != fileInfo.FullName);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                foreach (var file in path)
                                {
                                    var head = new TreeViewItem();
                                    head.Tag = file;
                                    head.Header = file.Name;
                                    //head.PreviewMouseDoubleClick += file_MouseDoubleClick;
                                    head.Selected += file_Selected;
                                    if (file.IsDirectory == true)
                                    {
                                        head.Expanded += Item_Expanded;
                                        var tempItem = new TreeViewItem();
                                        tempItem.Tag = "null";
                                        head.Items.Add(tempItem);
                                    }
                                    item.Items.Add(head);
                                }
                            });
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show(err.Message);
                        }
                    });
                }
            }
        }

        private void file_Selected(object sender, RoutedEventArgs e)
        {
            var item = sender as TreeViewItem;
            var fileInfo = item.Tag as SftpFile;
            SelectedFile = fileInfo.FullName;
            e.Handled = true;
        }

        private void Dialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            client?.Disconnect();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
