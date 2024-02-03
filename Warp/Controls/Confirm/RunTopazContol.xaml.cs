using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using Renci.SshNet;
using System.Threading;
using Path = System.IO.Path;
using System.Windows.Threading;
using System.Diagnostics;

namespace Warp.Controls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class RunTopazControl : UserControl, INotifyPropertyChanged
    {
        public bool Confirm;

        public event Action Close;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _ProgressText;
        public string ProgressText
        {
            get => _ProgressText;
            set
            {
                if (_ProgressText != value)
                {
                    _ProgressText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressText)));
                }
            }
        }

        public OptionsSshSettings SshSettings { get; set; }

        public OptionsAretomoSettings AretomoSettings { get; set; }

        public string ImportFolder { get; set; }

        public string LogFile { get; set; }

        public OptionsTiltSeries TiltSeries { get; set; }

        private List<Task> RunningTasks { get; set; } = new List<Task>();

        private Dictionary<string, Stopwatch> Incubator { get; set; } = new Dictionary<string, Stopwatch>();

        public RunTopazControl(string importFolder, OptionsSshSettings sshSettings, OptionsAretomoSettings aretomoSettings, OptionsTiltSeries tiltSeries)
        {
            ImportFolder = importFolder;
            LogFile = Path.Combine(AppContext.BaseDirectory, "log.txt");
            SshSettings = sshSettings;
            TiltSeries = tiltSeries;
            AretomoSettings = aretomoSettings;
            InitializeComponent();
        }

        private async void ButtonConfirm_OnClick(object sender, RoutedEventArgs e)
        {
            Confirm = true;
            ButtonConfirm.IsEnabled = false;

            ProgressText = "Starting...";

            Directory.CreateDirectory(Path.Combine(ImportFolder, "topaz"));
            var watcher = new FileSystemWatcher(Path.Combine(ImportFolder, "topaz"))
            {
                EnableRaisingEvents = true,
                Filter = "*.mrc"
            };

            watcher.NotifyFilter = NotifyFilters.Attributes
                     | NotifyFilters.CreationTime
                     | NotifyFilters.DirectoryName
                     | NotifyFilters.FileName
                     | NotifyFilters.LastAccess
                     | NotifyFilters.LastWrite
                     | NotifyFilters.Security
                     | NotifyFilters.Size;

            watcher.Created += (watchersender, watchere) =>
            {
                Stopwatch timer;
                if (Incubator.TryGetValue(watchere.Name, out timer))
                {
                    timer.Restart();
                }
                else
                {
                    timer = new Stopwatch();
                    Incubator.Add(watchere.Name, timer);
                    timer.Start();
                }
            };

            watcher.Changed += (watchersender, watchere) =>
            {
                Stopwatch t;
                if (Incubator.TryGetValue(watchere.Name, out t))
                {
                    Console.WriteLine($"{watchere.Name} timer {t.ElapsedMilliseconds}");
                    if (t.IsRunning) t.Restart();
                }
            };


            var Timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 4, 0), DispatcherPriority.Background, (a, b) =>
            {
                foreach (var Item in Incubator)
                {
                    if (Item.Value.ElapsedMilliseconds > 4000 && Item.Value.IsRunning)
                    {
                        Dispatcher.Invoke(() => ProgressText = $"Denoised {Item.Key}");
                        Item.Value.Reset();
                        RunningTasks.Add(
                            Task.Run(() => UpdateMovie(Item.Key))
                        );
                    }
                }
            }, Dispatcher);

            await Task.Run(() =>
            {
                var reconstructedTs = TiltSeries.TiltSeriesList.Where(x => File.Exists(x.OutTomo) && !File.Exists(x.OutTomoDenoised));
                foreach (var ts in reconstructedTs)
                {
                    var files = TiltSeriesProcessor.ProcessingFiles(ts.MdocFile, SshSettings.LinuxPath, SshSettings.AretomoVersion);
                    ts.OutTomoLinux = files.OutTomoLinux;
                }
                var tomograms = String.Join(" ", reconstructedTs.Select(x => x.OutTomoLinux).ToList());
                var cmd = "source ~/.bashrc; ";
                cmd += $"conda activate {SshSettings.TopazEnv}; ";
                cmd += $"cd {SshSettings.LinuxPath}; ";
                cmd += $"topaz denoise3d -o {SshSettings.LinuxPath}/topaz {tomograms}; ";
                cmd += "echo DONE";
                try
                {
                    using (var sshclient = new SshClient(SshSettings.Server, SshSettings.Port, SshSettings.Username, SshSettings.SshKeyObject))
                    {
                        sshclient.Connect();
                        using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                        {
                            stream.WriteLine(cmd);
                            while (stream.CanRead)
                            {
                                var line = stream.ReadLine();
                                using (StreamWriter file = new StreamWriter(LogFile, true))
                                {
                                    file.WriteLine(line);
                                }
                                if (line.Trim().EndsWith("DONE") && !line.Trim().EndsWith("echo DONE"))
                                {
                                    break;
                                }

                                Dispatcher.Invoke(() => ProgressText = line);
                            }
                        }
                        sshclient.Disconnect();

                    }
                }
                catch { }
            });
            
            await Task.WhenAll(RunningTasks);

            Close?.Invoke();
        }

        private async void UpdateMovie(string name)
        {
            Console.WriteLine($"Updating movie {name}");
            TiltSeriesViewModel ts = TiltSeries.TiltSeriesList.Where(x => x.DisplayName == Helper.PathToName(name)).FirstOrDefault();

            if (ts == null)
            {
                Console.WriteLine($"{name} not found. Cannot update movie");
                return;
            }

            int counter = 0;
            while (!File.Exists(Path.Combine(ImportFolder,"topaz",name)))
            {
                Console.WriteLine($"File {Path.Combine(ImportFolder, "topaz", name)} does not exist. Counter {counter}");
                await Task.Delay(1000);
                counter++;
                if (counter == 10) return;
            }

            int nslices = AretomoSettings.GlobalVolZ / AretomoSettings.GlobalBinning > 999 ? 4 : 3;
            string baseName = Path.GetFileNameWithoutExtension(ts.MdocFile);
            string LinuxPath = SshSettings.LinuxPath.TrimEnd('/') + "/topaz";

            var files = TiltSeriesProcessor.ProcessingFiles(ts.MdocFile, SshSettings.LinuxPath, SshSettings.AretomoVersion);

            var cmd = $"mrc2tif -j -a 0,0 {files.OutTomoDenoisedLinux} {LinuxPath}/{baseName}; ";
            cmd += $"ffmpeg -y -i {LinuxPath}/{baseName}.%0{nslices}d.jpg -pix_fmt yuv420p -s 452x640 {files.AretomoOutTomogramMovLinux}; rm {LinuxPath}/{baseName}.*.jpg; ";
            cmd += "echo DONE";

            Console.WriteLine(cmd);

            //TiltSeriesProcessor.RunAretomo2Png(ts, SshSettings, AretomoSettings);
            using (var sshclient = new SshClient(SshSettings.Server, SshSettings.Port, SshSettings.Username, SshSettings.SshKeyObject))
            {
                sshclient.Connect();
                using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                {
                    stream.WriteLine(cmd);
                    while (stream.CanRead)
                    {
                        var line = stream.ReadLine();
                        Console.WriteLine(line);
                        using (StreamWriter file = new StreamWriter(files.LogFile, true))
                        {
                            file.WriteLine(line);
                        }
                        if (line == "DONE") break;
                    }
                }
                sshclient.Disconnect();
            }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Confirm = false;
            Close?.Invoke();
        }
    }
}
