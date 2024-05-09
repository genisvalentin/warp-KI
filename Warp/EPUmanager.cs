using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Warp.Headers;
using Warp.Tools;
using System.Xml;
using System.Windows;
using System.Net.Http;
using Newtonsoft.Json;
using System.Xml.XPath;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media;
using Accord.MachineLearning;
using System.Drawing;
using Color = System.Drawing.Color;
using System.Diagnostics;
using System.Globalization;

namespace Warp
{
    class EPUSession
    {
        public int NFrames { get; set; } = 0;
        public int NOpticsGroup { get; set; } = 0;

        public string WarpSubDir { get; set; } = "";

        private readonly Options Options;
        private const string DefaultGlobalOptionsName = "global.settings";
        private readonly GlobalOptions GlobalOptions;
        private static readonly HttpClient client = new HttpClient();
        public Dictionary<string,List<string>> OpticsGroupDict = new Dictionary<string, List<string>>();
        private DateTime oldestMicrographDt;
        private string[] XMLfiles = Array.Empty<string>();
        private Coord[] OpticGroupsCentroids;


        public EPUSession(Options options)
        {
            Options = options;
            GlobalOptions = new GlobalOptions();
            if (File.Exists(DefaultGlobalOptionsName))
                GlobalOptions.Load(DefaultGlobalOptionsName);
        }

        public void Stacker(string FolderPath, string Extension, string Ignore = "")
        {
            bool DoRecursiveSearch = true;
            bool DeleteWhenDone = true;
            bool DeleteExtraGain = true;
            var ignore = Ignore == "" ? $"\\{ Ignore}\\" : Ignore;

            //Copy also mdoc files if present
            foreach (var filename in Directory.EnumerateFiles(FolderPath, "*.mdoc",
                                                    DoRecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Where(
    f => !f.Contains(ignore)
    ))
            {
                string NameOut = Options.Import.Folder + "\\" + Helper.PathToNameWithExtension(filename);
                try
                {
                    if (!File.Exists(NameOut))
                    {
                        if (DeleteWhenDone)
                        {
                            File.Move(filename, NameOut);
                        }
                        else
                        {
                            File.Copy(filename, NameOut);
                            File.Move(filename, FolderPath + "original/" + Helper.PathToNameWithExtension(filename));
                        }
                    }
                    else
                    {
                        if (File.Exists(filename))
                        {
                            if (DeleteWhenDone)
                            {
                                File.Delete(filename);
                                LogToFile("Removing file " + filename);
                            }
                            else
                            {
                                File.Move(filename, FolderPath + "original/" + Helper.PathToNameWithExtension(filename));
                                LogToFile("Archiving file " + filename);
                            }
                        }
                    }
                    LogToFile("Done moving: " + Helper.PathToNameWithExtension(filename));
                }
                catch (Exception exc)
                {
                    LogToFile("Something went wrong moving mdoc file" + Helper.PathToNameWithExtension(filename) + ":\n" + exc.ToString());
                }
            }

            if (NFrames == 0) NFrames = GuessNFrames(FolderPath, Extension, Ignore);
            if (NFrames == 0) return;

            List<string> FrameNames = new List<string>();
            List<string> GainRefNames = new List<string>();

            List<string> HaveBeenProcessed = new List<string>();

            foreach (var filename in Directory.EnumerateFiles(FolderPath, "*" + Extension,
                                                                DoRecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Where(
                f => !f.Contains(ignore)
                ))
            {
                try
                {
                    MapHeader Header = MapHeader.ReadFromFile(filename);
                    if (Header.Dimensions.Z != NFrames)
                    {
                        if (Header.Dimensions.Z == 1)
                            GainRefNames.Add(filename);
                        continue;
                    }

                    if (HaveBeenProcessed.Contains(filename))
                        continue;

                    if (DeleteWhenDone)
                        FrameNames.Add(filename);
                    else if (!filename.Contains(@"\original\"))
                        FrameNames.Add(filename);
                }
                catch
                {
                }
                if (FrameNames.Count > 6) break; //We list up to 6 files to speed up listing.
            }

            int NFiles = FrameNames.Count;
            LogToFile("Stacker found " + NFiles.ToString() + " in directory " + FolderPath);

            if (NFiles == 0)
            {
                return;
            }
            
            //for increased responsiveness in slow hard drives, we import in blocks of 5 files
            //for (int f = 0; f < NFiles; f++)
            
            for (int f = 0; f < Math.Min(NFiles, 5); f++)
            {
                string FrameName = FrameNames[f];

                MapHeader Header = MapHeader.ReadFromFilePatient(500, 100, FrameName, new int2(1), 0, typeof(float));

                bool Success = false;

                while (!Success)
                {
                    LogToFile(Options.Import.Folder + " and " + Helper.PathToNameWithExtension(FrameName));
                    string NameOut = Options.Import.Folder + "\\" + Helper.PathToNameWithExtension(FrameName);
                    try
                    {
                        if (!File.Exists(NameOut)) {
                            if (DeleteWhenDone) {
                                File.Move(FrameName, NameOut);
                            }
                            else
                            {
                                File.Copy(FrameName, NameOut);
                                File.Move(FrameName, FolderPath + "original/" + Helper.PathToNameWithExtension(FrameName));
                            }
                        } else
                        {
                            if(File.Exists(FrameName))
                            {
                                if (DeleteWhenDone) { 
                                    File.Delete(FrameName);
                                    LogToFile("Removing file " + FrameName);
                                }
                                else
                                {
                                    File.Move(FrameName, FolderPath + "original/" + Helper.PathToNameWithExtension(FrameName));
                                    LogToFile("Archiving file " + FrameName);
                                }
                            }
                        }
                        HaveBeenProcessed.Add(FrameName);
                        Success = true;

                        LogToFile("Done moving: " + Helper.PathToNameWithExtension(FrameName));
                    }
                    catch (Exception exc)
                    {
                        LogToFile("Something went wrong moving " + Helper.PathToNameWithExtension(FrameName) + ":\n" + exc.ToString());
                        LogToFile($"Source: {FrameName}\nDest:{NameOut}");
                        LogToFile($"Current session folder: {Options.Import.Folder}");
                    }
                    
                }
            }



                if (DeleteExtraGain)
            {
                foreach (var gainRefName in GainRefNames)
                    File.Delete(gainRefName);
            }

        }

        private int GuessNFrames(string EPUdir, string Extension, string Ignore)
        {
            LogToFile("Enumerating all files...");
            IEnumerable<string> fileList = Directory.EnumerateFiles(EPUdir, "*"+Extension, SearchOption.AllDirectories).Where(f => !f.Contains($"/{Ignore}/"));

            int nFiles = fileList.Count();
            LogToFile("Guess Nframes counted " + nFiles.ToString());
            if (nFiles < 4) return(0);
            List<int> nFrames = new List<int>();
            int i = 0;
            foreach (var filename in fileList)
            {
                try
                {
                    MapHeader Header = MapHeader.ReadFromFile(filename);
                    nFrames.Add(Header.Dimensions.Z);
                } catch (System.IO.IOException e)
                {
                    /*LogToFile(
                    $"{e.GetType().Name}: The write operation could not " +
                    "be performed because the specified " +
                    "part of the file is locked.");*/
                    return (0);
                }
                i++;
                if (i == 3) { break; }
            }
            int maxNFrames = nFrames.Max();
            LogToFile($"NFrames: {maxNFrames}");
            return (maxNFrames);
        }

        private System.Numerics.Vector2 v1;
        private System.Numerics.Vector2 v2;

        public void EstimateNOpticGroups()
        {
            LogToFile("Estimating N Optic groups");
            if (OpticsGroupDict.Count() < (Options.Picking.WriteOpticGroupsN + 1))
            {
                LogToFile("Not enough micrographs to estimate N optics groups. " + OpticsGroupDict.Count().ToString() + "/" + Options.Picking.WriteOpticGroupsN.ToString());
                return;
            }

            List<Coord> coords = new List<Coord>();
            string[] movies;
            lock (OpticsGroupDict)
            {
                int counter = 0;
                movies = new string[Options.Picking.WriteOpticGroupsN];
                foreach (KeyValuePair<string, List<string>> m in OpticsGroupDict.Take(Options.Picking.WriteOpticGroupsN))
                {
                    coords.Add(new Coord(
                        double.Parse(m.Value[1], System.Globalization.CultureInfo.InvariantCulture),
                        double.Parse(m.Value[2], System.Globalization.CultureInfo.InvariantCulture)
                    ));
                    movies[counter] = m.Key;
                    counter++;
                }
            }

            int bin = 20;
            System.Numerics.Vector2 vector1 = new System.Numerics.Vector2(0, 0);
            System.Numerics.Vector2 vector2 = new System.Numerics.Vector2(0, 0);
            System.Numerics.Vector2[] a = new System.Numerics.Vector2[] { new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(0, 0) };
            //The bin factor is important to find the foil hole pattern. We try from bin=20 to bin=50 to find the right bin factor.
            int q = 0;
            while (bin < 60) {

                if (a[0].X == 0 && a[0].Y == 0)
                {
                    vector1 = OpticGroups.GetVectors(coords.ToArray(), 90, 0, bin: bin);
                    // Quantifoil grids have an almost square pattern. Therefore, the
                    // second vector will be at 90 +- 10 degrees from the first. That's why I substract 10 to the atan
                    var atan = -Math.Atan2(vector1.Y, vector1.X) * 180 / Math.PI - 10;
                    q = (int)atan;
                    LogToFile($"Start search angle: {q}");
                }

                if ((a[0].X + a[0].Y) * (a[1].X + a[1].Y) == 0) vector2 = OpticGroups.GetVectors(coords.ToArray(), 20, 1, bin: bin, start_angle: q);

                LogToFile($"Bin: {bin}");
                LogToFile($"Vectors {vector1.X},{vector1.Y}; {vector2.X},{vector2.Y}");

                //We don't accept stolutions were vectors are very different in length.
                //Assuming a max theoretical tilt of 45 degress, cos(45 degrees) = 0.7 approx
                //The vectors should be max 0.7 times smaller or 1.43 times bigger than the other.
                //Just to be safe, I used cos(50 degrees), i.e. 0.64 and 1.55
                var ratio = vector1.Length() / vector2.Length();
                if (ratio > 1.55 || ratio < 0.64) {
                    bin += 5;
                    continue;
                }

                a = OpticGroups.RefineVectors(coords.ToArray(), vector1, vector2);
                LogToFile($"Refined vectors {a[0].X},{a[0].Y}; {a[1].X},{a[1].Y}");

                if ( (a[0].X + a[0].Y) * (a[1].X + a[1].Y) == 0) //if any of the two vectors is (0,0), we try again with a higher bin.
                {
                    bin += 5;
                    continue;
                }

                break;
            }

            //Plot Optic Groups        
            var plt = new ScottPlot.Plot(400, 300);
            var coordx = coords.Select(x => x.X).ToArray();
            var coordy = coords.Select(y => y.Y).ToArray();
            plt.AddScatter(coordx, coordy, lineWidth: 0);

            if ((a[0].X + a[0].Y) * (a[1].X + a[1].Y) == 0) //Centroid refinement failed if any of the two vectors is (0,0)
            {
                NOpticsGroup = 0;
                var unrefIndexes = OpticGroups.GetCentroidIndexes(coords.ToArray(), vector1, vector2);
                OpticGroupsCentroids = OpticGroups.GetCentroids(unrefIndexes, vector1, vector2);
                var unrefcentroidX = OpticGroupsCentroids.Select(x => x.X).ToArray();
                var unrefcentroidY = OpticGroupsCentroids.Select(y => y.Y).ToArray();
                var unrefsp3 = plt.AddScatter(unrefcentroidX, unrefcentroidY, lineWidth: 0, markerSize: 15);
                unrefsp3.MarkerShape = ScottPlot.MarkerShape.openCircle;
                plt.SaveFig(Path.GetFullPath(Options.Import.Folder).TrimEnd(Path.DirectorySeparatorChar) + @"\opticsGroups.png");
                return;
            }

            v1 = a[0];
            v2 = a[1];

            var refIndexes = OpticGroups.GetCentroidIndexes(coords.ToArray(), v1, v2);
            OpticGroupsCentroids = OpticGroups.GetCentroids(refIndexes, v1, v2);
            NOpticsGroup = refIndexes.Length;

            var refcentroidX = OpticGroupsCentroids.Select(x => x.X).ToArray();
            var refcentroidY = OpticGroupsCentroids.Select(y => y.Y).ToArray();
            var sp3 = plt.AddScatter(refcentroidX, refcentroidY, lineWidth: 0, markerSize: 15);
            sp3.MarkerShape = ScottPlot.MarkerShape.openCircle;

            plt.SaveFig(Path.GetFullPath(Options.Import.Folder).TrimEnd(Path.DirectorySeparatorChar) + @"\opticsGroups.png");
        }

        public bool OpticsGroupsComplete = false;
        public void UpdateOpticsGroup()
        {
            Coord[] coords;
            int[][] indexes;
            Coord[] centroids;
            int[] groups;
            var g = OpticsGroupDict.Select((item) => item.Value[3]);
            if (!g.Contains("0"))
            {
                OpticsGroupsComplete = true;
                return;
            } else
            {
                OpticsGroupsComplete = false;
            }

            lock (OpticsGroupDict)
            {
                coords = OpticsGroupDict.Select(x => new Coord(double.Parse(x.Value[1]), double.Parse(x.Value[2]))).ToArray();
                indexes = OpticGroups.GetCentroidIndexes(coords, v1, v2);
                centroids = OpticGroups.GetCentroids(indexes, v1, v2);
                double[][] providedCentroids = centroids.Select(x => new double[] { x.X, x.Y }).ToArray();

                var kmeans = new KMeans(k: providedCentroids.Length);
                kmeans.Centroids = providedCentroids;
                groups = kmeans.Clusters.Decide(coords.Select(x => new double[] { x.X, x.Y }).ToArray());

                int counter = 0;
                if (groups.Length == OpticsGroupDict.Count)
                {
                    foreach (var mic in OpticsGroupDict)
                    {
                        mic.Value[3] = (groups[counter] + 1).ToString();
                        counter++;
                    }
                    Console.WriteLine($"Assigned optics group to {counter} micrographs");
                }
            }

            var distinct_groups = groups.Distinct().ToList();

            NOpticsGroup = indexes.Length;

            var plt = new ScottPlot.Plot(400, 300);
            int counter2 = 0;

            var colorArray = GenerateColorsArray(distinct_groups.Count);

            foreach (var c in coords)
            {
                plt.AddPoint(c.X, c.Y, colorArray[distinct_groups.IndexOf(groups[counter2])]);
                counter2++;
            }

            int groupcounter = 0;
            int totalAssignedMics = 0;
            foreach (var centroid in centroids)
            {
                var x = centroid.X - 0.01;
                var y = centroid.Y + 0.01;
                var c = groups.ToList().Where(group => group == groupcounter).Count();
                totalAssignedMics += c;
                plt.AddText(c.ToString(), x, y, size: 12, color: Color.Black);
                groupcounter++;
            }

            plt.XAxis2.Label(label: $"Total: {totalAssignedMics} mics", size: 12, color: Color.Black, bold: true);
            Console.WriteLine($"Total assigned mics: {totalAssignedMics}");


            var refcentroidX = centroids.Select(x => x.X).ToArray();
            var refcentroidY = centroids.Select(y => y.Y).ToArray();
            var sp3 = plt.AddScatter(refcentroidX, refcentroidY, lineWidth: 0, markerSize: 15);
            sp3.MarkerShape = ScottPlot.MarkerShape.openCircle;

            plt.SaveFig(Path.GetFullPath(Options.Import.Folder).TrimEnd(Path.DirectorySeparatorChar) + @"\opticsGroups.png");
        }

        static Color[] GenerateColorsArray(int n)
        {
            Color[] colors = new Color[n];
            Random random = new Random();
            for (int i = 0; i < n; i++)
            {
                colors[i] = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
            }
            return colors;
        }
     
        public string GetOpticsGroup(string movieName)
        {
            if (!OpticsGroupDict.ContainsKey(movieName))
            {
                if (!ImportXMLfile(movieName)) return "0";
            }
            return OpticsGroupDict[movieName][3];
        }

        public float2 GetBeamTilt (string movieName)
        {
            var val = new List<string>() { "0", "0", "0", "0" };
            OpticsGroupDict.TryGetValue(movieName, out val);
            return new float2 (float.Parse(val[1],CultureInfo.InvariantCulture), float.Parse(val[2], CultureInfo.InvariantCulture));
        }

        public void UpdateXmlFileList(string CurrentXMLFolder)
        {
            Console.WriteLine("Update XML file list");
            int NMovies = 0;
            //int nunprocessed=0;
            Application.Current.Dispatcher.Invoke(() => {
                var mw = (MainWindow)Application.Current.MainWindow;
                NMovies = mw.FileDiscoverer.GetImmutableFiles().Length;
            });

            if (OpticsGroupDict.Count == NMovies)
            {
                Console.WriteLine("All micrographs have an optics group");
                return;
            }

            try
            {
                XMLfiles = Directory.GetFiles(CurrentXMLFolder, "*.xml", SearchOption.AllDirectories);
            } catch (UnauthorizedAccessException ex)
            {
                LogToFile("UpdateXmlFileList UnauthorizedAccessException" + ex.Message);
            } catch (DirectoryNotFoundException ex)
            {
                LogToFile("UpdateXmlFileList DirectoryNotFoundException. " + ex.Message);
            } finally
            {
                LogToFile("Listed " + XMLfiles.Count() + " XML files.");
            }
        }

        private bool ImportXMLfile(string FrameName) {
            string XMLfile = default;
            int index = Helper.PathToName(FrameName).IndexOf("_fractions");
            if (index < 0) {
                LogToFile(FrameName + "does not contain the _fractions substring. Cannot look for XML file.");
                return false;
            }
            string searchFile = Options.Import.Folder + @"\" + "microscopeXMLfiles" + @"\" + Helper.PathToName(FrameName).Substring(0, index) + ".xml";
            if (File.Exists(searchFile))
            {
                XMLfile = searchFile;
            }
            else
            {
                searchFile = Helper.PathToName(FrameName).Substring(0, index) + ".xml";
                string match = default(string);
                if (XMLfiles.Count() > 0)
                {
                    match = Array.Find(XMLfiles, f => f.EndsWith(searchFile));
                }
                if (match != XMLfile)
                {
                    XMLfile = match;
                }
            }

            if (File.Exists(XMLfile)) {
                string file = Helper.PathToNameWithExtension(XMLfile);
                string XMLout = Options.Import.Folder + @"\" + "microscopeXMLfiles" + @"\" + file;
                if (!File.Exists(XMLout) && Directory.Exists(Options.Import.Folder + @"\" + "microscopeXMLfiles"))
                {
                    File.Copy(XMLfile, XMLout);
                }
                if (File.Exists(XMLout))
                {
                    List<string> metadata = new List<string>();
                    metadata = GetMovieMetadata(XMLout);
                    if (!OpticsGroupDict.ContainsKey(FrameName))
                    {
                        lock (OpticsGroupDict)
                        {
                            OpticsGroupDict.Add(FrameName, metadata);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private List<string> GetMovieMetadata(string XMLfile)  {
            List<string> metadatalist = new List<string>() { "0", "0", "0", "0" };

            if (File.Exists(XMLfile))
            {
                using (Stream SettingsStream = File.OpenRead(XMLfile))
                {
                    XPathDocument document = new XPathDocument(SettingsStream);
                    XPathNavigator navigator = document.CreateNavigator();

                    XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
                    manager.AddNamespace("mns", "http://schemas.datacontract.org/2004/07/Fei.SharedObjects");
                    manager.AddNamespace("i", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("a", "http://schemas.datacontract.org/2004/07/Fei.Types");

                    string xpath = "//mns:BeamShift/a:_x";
                    string xvalue = navigator.SelectSingleNode(xpath, manager).Value;
                    xpath = "//mns:BeamShift/a:_y";
                    string yvalue = navigator.SelectSingleNode(xpath, manager).Value;
                    xpath = "//mns:acquisitionDateTime";
                    string dt = navigator.SelectSingleNode(xpath, manager).Value;
                    metadatalist[0] = dt;
                    metadatalist[1] = xvalue;
                    metadatalist[2] = yvalue;
                    metadatalist[3] = "0"; //FindCenter(xvalue, yvalue);
                }
            }
            else
            {
                metadatalist[0] = "0";
                metadatalist[1] = "0";
                metadatalist[2] = "0";
                metadatalist[3] = "0";
            }
            return (metadatalist);
        }

        public DateTime GetAcquisitionDt(string movieName)
        {
            if (OpticsGroupDict.ContainsKey(movieName))
            {
                return (DateTime.Parse(OpticsGroupDict[movieName][0]));
            }
            else
            {
                return (DateTime.Now);
            }
        }

        private static void LogToFile(string s)
        {
            string LogFile = Path.Combine(AppContext.BaseDirectory, "log.txt");
            Application.Current.Dispatcher.InvokeAsync(async () => {
                try
                {
                    using (StreamWriter outputFile = new StreamWriter(LogFile, true))
                    {
                        await outputFile.WriteLineAsync(s);
                    }
                }
                catch { 
                    cryosparcClient.Client.LogToFile(s);
                };
            });
        }
    }

    class EPUSessionManager : INotifyPropertyChanged
    {
        private readonly Options Options;
        public bool active = false;
        private Task[] TaskArray = new Task[5];
        private EPUSession session;
        public FileSystemWatcher FileWatcher;
        public bool NeedsRelaunchClass = false;
        public bool NeedsCreateInputStar = false;
        private Dictionary<string, int[]> directoryIsActiveDict = new Dictionary<string, int[]>();

        public delegate void ProcessingEventHandler(object sender, RoutedEventArgs e);
        public event ProcessingEventHandler StartProcessingSignal;
        public event ProcessingEventHandler StopProcessingSignal;

        public event PropertyChangedEventHandler PropertyChanged;

        private DateTime oldestMicDt = DateTime.Now;

        public bool NeedsUpdateAllMics = false;

        public string CurrentXMLFolder = "";
        public string CurrentSessionName = "";
        public string PreviousSessionName = "";

        private string _currentStackerFolder = "";
        public string CurrentStackerFolder {
            get => _currentStackerFolder;
            set {
                if (_currentStackerFolder != value)
                {
                    _currentStackerFolder = value;
                }
            }
        }

        private string _switchToDirectory = "";
        public string SwitchToDirectory
        {
            get => _switchToDirectory;
            set
            {
                if (value != _switchToDirectory)
                {
                    _switchToDirectory = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SwitchToDirectory)));
                }
            }
        }

        private bool _IsClassificationRunning;
        public bool IsClassificationRunning {
            get => _IsClassificationRunning;
            set
            {
                if (_IsClassificationRunning != value)
                {
                    _IsClassificationRunning = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SwitchToDirectory)));
                }
            }
        }

        public bool Waiting { get; set; }

        public string XMLFolder = "";
        public string StackerFolder = "";
        public string ImportFolder = "";
        public string CurrentClassificationStatus = "Not ready";
        public string Countdown;
        public bool canaddopticgroups = false;
        public Star OpticsGroupTable;
        private int biggestShift = 0;

        private int _NGoodParticles = 0;
        public int NGoodParticles {
            get { return _NGoodParticles; }
            set {
                if (_NGoodParticles != value)
                {
                    _NGoodParticles = value;
                    LastParticleUpdate = DateTime.Now;
                }
            }
        }

        public string PathBoxNetFiltered = "";
        private DateTime latestClassLaunchTime;

        Task ClassificationTask;

        private int LastNGoodParticles = 0;

        private DateTime LastParticleUpdate; 

        private cryosparcClient.Client cryosparcclient = new cryosparcClient.Client();

        private int _NOpticsGroup = 0;
        public int NOpticsGroup
        {
            get => _NOpticsGroup;
            set
            {
                if (value != _NOpticsGroup)
                {
                    _NOpticsGroup = value;
                    NeedsUpdateAllMics = true;
                    //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NOpticsGroup)));
                }
            }
        }

        public bool SessionSwitchIndicator = false;

        public bool CanLoadDefaultOptions { get; set; } = true;

        MainWindow MainWindow;

        public EPUSessionManager(Options options, CancellationToken token)
        {
            MainWindow = (MainWindow)Application.Current.MainWindow;
            Options = options;
            session = new EPUSession(Options);
            PopulateOpticsGroupTable(0);
        }

        private void PopulateOpticsGroupTable(int nOpticsGroup)
        {
            OpticsGroupTable = new Star(new string[] { });

            OpticsGroupTable.AddColumn("rlnOpticsGroupName", CurrentSessionName+"_0");
            OpticsGroupTable.AddColumn("rlnOpticsGroup", "0");

            List<List<string>> NewRows = new List<List<string>>();
            //We always add a row for optics group 0 in case some micrographs could not be assigned to an optics group.
            for (int i = 0; i < ( nOpticsGroup + 1 ); i++)
            {
                string[] Row = Helper.ArrayOfConstant("0", OpticsGroupTable.ColumnCount);
                Row[OpticsGroupTable.GetColumnID("rlnOpticsGroupName")] = CurrentSessionName + "_" + i.ToString();
                Row[OpticsGroupTable.GetColumnID("rlnOpticsGroup")] = i.ToString();
                NewRows.Add(Row.ToList());
            }

            OpticsGroupTable.AddRow(NewRows);
            OpticsGroupTable.AddColumn("rlnMicrographPixelSize", Options.Runtime.BinnedPixelSizeMean.ToString());
            OpticsGroupTable.AddColumn("rlnVoltage", Options.CTF.Voltage.ToString());
            OpticsGroupTable.AddColumn("rlnSphericalAberration", Options.CTF.Cs.ToString());
            OpticsGroupTable.AddColumn("rlnAmplitudeContrast", Options.CTF.Amplitude.ToString());
        }

        public bool SessionLoopFinished { get; set; } = false;

        public bool NeedsNewOpticsGroup { get; set; } = false;

        private bool SessionLoopSemaphore { get; set; } = false;

        public async Task SessionLoop(CancellationToken cancellationToken, int task = -1, bool wait = false)
        {
            if (SessionLoopSemaphore) return;
            SessionLoopSemaphore = true;

            LogToFile("EPU session manager loop");
            Console.WriteLine("EPU session manager loop");

            Dictionary<string, string> classificationStatus = new Dictionary<string, string> { { "status", ""} };
            Dictionary<string, string> launchClassificationStatus = new Dictionary<string, string>();

            //We do nothing if we are still waiting for a Current Stacker Folder
            if (Options.ProcessStacker && Options.Stacker.GridScreening && !Directory.Exists(CurrentStackerFolder))
            {
                SessionLoopSemaphore = false;
                return;
            }

            for (int i = 0; i < TaskArray.Length; i++)
            {
                if (task > -1 && task != i ) { continue; }

                if (TaskArray[i] != null)
                {
                    if (TaskArray[i].Status == TaskStatus.Running) continue;
                }

                //Update XML file list
                if (i == 0)
                {
                    if (Options.ProcessPicking && Options.Picking.WriteOpticGroups && Directory.Exists(CurrentXMLFolder))
                    {
                        TaskArray[i] = Task.Run(() => session.UpdateXmlFileList(CurrentXMLFolder)).ContinueWith(_ =>
                        {
                            AggregateException ex = _.Exception;
                                LogToFile($"UpdateXmlFileList task exception {ex.GetType().Name}: {ex.Message}");
                        }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                    continue;
                }

                //Run Stacker
                if (i == 1)
                {
                    if (Options.ProcessStacker && Directory.Exists(CurrentStackerFolder))
                    {
                        string ext = Options.Import.Extension.Substring(1);
                        TaskArray[i] = Task.Run(() => session.Stacker(CurrentStackerFolder, ext, StackerIgnore)).ContinueWith(_ =>
                        {
                            AggregateException ex = _.Exception;
                            LogToFile($"Stacker task exception {ex.GetType().Name}: {ex.Message}");
                        }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                    continue;
                }

                //Assign optic groups
                if (i == 2)
                {

                    if (Options.Picking.WriteOpticGroups && NeedsNewOpticsGroup)
                    {
                        session.NOpticsGroup = 0;
                        NeedsNewOpticsGroup = false;
                    }

                    if (Options.Picking.WriteOpticGroups && session.NOpticsGroup == 0)
                    {
                        NeedsNewOpticsGroup = false;
                        NeedsUpdateAllMics = true;
                        TaskArray[i] = Task.Run(session.EstimateNOpticGroups).ContinueWith(_ =>
                        {
                            AggregateException ex = _.Exception;
                            LogToFile($"EstimateNOpticGroups task exception {ex.GetType().Name}: {ex.Message}");
                        }, TaskContinuationOptions.OnlyOnFaulted);
                        continue;
                    }

                    canaddopticgroups = session.NOpticsGroup > 0;
                    NOpticsGroup = session.NOpticsGroup;

                    if (canaddopticgroups)
                    {
                        PopulateOpticsGroupTable(biggestShift + NOpticsGroup);
                        TaskArray[i] = Task.Run(session.UpdateOpticsGroup).ContinueWith(_ =>
                        {
                            AggregateException ex = _.Exception;
                            LogToFile($"EstimateNOpticGroups task exception {ex.GetType().Name}: {ex.Message}");
                        }, TaskContinuationOptions.OnlyOnFaulted);
                        continue;
                    }

                    continue;
                }

                //Classification
                if (i == 3)
                {
                    if (Options.ProcessClassification)
                    {

                        classificationStatus = cryosparcclient.getClassificationResults(CurrentSessionName);
                        LogToFile($"Classification status {classificationStatus["success"]}, {classificationStatus["status"]} ");
                        if (Options.Classification.NParticles < NGoodParticles)
                        {
                            CurrentClassificationStatus = classificationStatus["status"];
                            TimeSpan ts;
                            if (latestClassLaunchTime != null)
                            {
                                ts = DateTime.Now - latestClassLaunchTime;
                            }
                            else
                            {
                                ts = TimeSpan.FromMinutes(0);
                            }

                            if (classificationStatus["success"] == "True")
                            {
                                if (Options.Classification.DoEveryHours)
                                {
                                    var RequestedWaitTime = int.Parse(Options.Classification.EveryHours);
                                    if (ts > TimeSpan.FromMinutes(RequestedWaitTime))
                                    {
                                        NeedsRelaunchClass = true;
                                    }
                                    else
                                    {
                                        int remainingTime = RequestedWaitTime - ts.Minutes + 1;
                                        CurrentClassificationStatus = classificationStatus["status"];
                                        Countdown = String.Format("Starting in {0} min", remainingTime.ToString());
                                    }
                                }
                                else if (Options.Classification.DoEveryNParticles)
                                {
                                    int RequestedParticles = int.Parse(Options.Classification.EveryNParticles);
                                    if ((LastNGoodParticles + RequestedParticles) < NGoodParticles || LastNGoodParticles == 0)
                                    {
                                        NeedsRelaunchClass = true;
                                    }
                                    else
                                    {
                                        int RemainingParticles = RequestedParticles - (NGoodParticles - LastNGoodParticles);
                                        CurrentClassificationStatus = classificationStatus["status"];
                                        Countdown = String.Format("Starting in {0} particles", RemainingParticles.ToString());
                                    }
                                }
                                else if (Options.Classification.DoImmediateClassification)
                                {
                                    NeedsRelaunchClass = true;
                                    Countdown = "";
                                }
                                else if (Options.Classification.DoManualClassification)
                                {
                                    Countdown = "";
                                } else if (Options.Classification.DoAtSessionEnd)
                                {
                                    Countdown = "Starting when changing session";
                                    if (SessionSwitchIndicator)
                                    {
                                        NeedsRelaunchClass = true;
                                    }
                                    //If idle for 10 min 
                                    if (DateTime.Now - LastParticleUpdate > TimeSpan.FromMinutes(10))
                                    {
                                        NeedsRelaunchClass = true;
                                    }
                                }
                            }
                            else
                            {
                                Countdown = "Running";
                            }
                        }
                        else
                        {
                            CurrentClassificationStatus = $"Waiting for {NGoodParticles} out of {Options.Classification.NParticles}";
                        }

                        if (NeedsRelaunchClass)
                        {
                            NeedsCreateInputStar = true;
                            LastNGoodParticles = NGoodParticles;
                            LogToFile("Launch classification");
                            latestClassLaunchTime = DateTime.Now;
                            cryosparcClient.SessionInfo sessionInfo;
                            if (SessionSwitchIndicator && PreviousSessionName != "" && Options.Classification.DoAtSessionEnd)
                            {
                                sessionInfo = OptionsToSessionInfo(PreviousSessionName);
                                SessionSwitchIndicator = false;
                            } else
                            {
                                sessionInfo = OptionsToSessionInfo(CurrentSessionName);
                            }
                            string BoxNetSuffix = Helper.PathToNameWithExtension(Options.Picking.ModelPath);
                            string particle_meta_path = Path.Combine(Options.Import.Folder, "goodparticles_cryosparc_input.star");
                            
                            TaskArray[i] = Task.Run( async () => {
                                if (File.Exists(particle_meta_path)) File.Delete(particle_meta_path);
                                while (!File.Exists(particle_meta_path)) Thread.Sleep(1000);
                                NeedsCreateInputStar = false;
                                await cryosparcclient.Run(sessionInfo, cancellationToken);
                                }
                            ).ContinueWith(_ =>
                            {
                                AggregateException ex = _.Exception;
                                LogToFile($"ClassificationTask task exception {ex.GetType().Name}: {ex.Message}");
                                CurrentClassificationStatus = "Clasification task exception";
                                classificationStatus["success"] = "False";
                                cryosparcclient.SetClassificationResults(CurrentSessionName, true);
                                cryosparcclient.SetClassificationResults(CurrentSessionName, "Clasification task exception");

                            }, TaskContinuationOptions.OnlyOnFaulted);
                            NeedsRelaunchClass = false;
                        }
                    }
                    continue;
                }

                //Import MDOC files
                if (i == 4)
                {
                    TaskArray[i] = Task.Run(() =>
                    {
                        if (Directory.Exists(Options.Import.MicroscopePCFolder))
                        {
                            var searchFolder = Path.Combine(Options.Import.MicroscopePCFolder, CurrentSessionName);
                            if (!Directory.Exists(searchFolder) || !IOHelper.CheckFolderPermission(searchFolder)) {
                                searchFolder = Options.Import.MicroscopePCFolder;
                            }
                            foreach (var MdocFile in Directory.EnumerateFiles(searchFolder, "*.mdoc", SearchOption.TopDirectoryOnly))
                            {
                                if (!File.Exists(Path.Combine(Options.TiltSeries.MdocFilesDirectory, Helper.PathToNameWithExtension(MdocFile))))
                                {
                                    File.Copy(MdocFile, Path.Combine(Options.TiltSeries.MdocFilesDirectory, Helper.PathToNameWithExtension(MdocFile)));
                                }
                            }
                        }
                    }).ContinueWith(_ =>
                    {
                        AggregateException ex = _.Exception;
                        LogToFile($"ImportMdocFile task exception {ex.GetType().Name}: {ex.Message}");
                    }, TaskContinuationOptions.OnlyOnFaulted);
                    continue;
                }

            }

            if (wait)
            {
                await Task.WhenAll(TaskArray);
            }

            //Update GUI
            Application.Current.Dispatcher.Invoke(() => {
                Options.Classification.Results = CurrentClassificationStatus;
                Options.Classification.Countdown = Countdown;
                var mw = (MainWindow)Application.Current.MainWindow;
                if (canaddopticgroups)
                {
                    mw.TextBlockNOpticsGroup.Text = NOpticsGroup.ToString();
                    if (session.OpticsGroupsComplete) {
                        mw.TextBlockNOpticsGroupG.Text = " g";
                    }
                    else 
                    {   
                        mw.TextBlockNOpticsGroupG.Text = " g..."; 
                    }
                    mw.TextBlockNOpticsGroupG.Visibility = Visibility.Visible;
                    mw.TextBlockNOpticsGroup.Visibility = Visibility.Visible;
                    mw.ButtonOpticGroups.IsEnabled = true;
                }
                else
                {
                    mw.TextBlockNOpticsGroup.Visibility = Visibility.Visible;
                    mw.TextBlockNOpticsGroup.Text = "🤔";
                    mw.TextBlockNOpticsGroupG.Visibility = Visibility.Hidden;
                    mw.ButtonOpticGroups.IsEnabled = false;
                }
            });

            SessionLoopSemaphore = false;
        }

        private cryosparcClient.SessionInfo OptionsToSessionInfo(string sessionName)
        {
            cryosparcClient.SessionInfo session = new cryosparcClient.SessionInfo();
            session.sessionPath = Options.Classification.ClassificationMountPoint.TrimEnd('/');
            if (Options.Stacker.GridScreening)
            {
                session.sessionPath += $"/{sessionName}";
            }
            if (StackerIgnore.Contains("WARP"))
            {
                session.sessionPath += $"/WARP";
            }
            var ps = session.sessionPath.Split('/').Reverse();
            foreach (var p in ps)
            {
                if (p == "WARP" || p == "Images-Disc1" || p == "") continue;
                session.sessionName = p;
                break;
            }
            if (session.sessionName == null || session.sessionName == "") session.sessionName = CurrentSessionName;
            session.voltage = Options.CTF.Voltage.ToString();
            session.cs = Options.CTF.Cs.ToString();
            session.contrast = Options.CTF.Amplitude.ToString();
            session.moviePixelSize = Options.PixelSizeX.ToString();
            double pixelSizeBinned = ((double)Options.PixelSizeX * Math.Pow(2, (double)Options.Import.BinTimes));
            session.pixelSize = pixelSizeBinned.ToString();
            session.boxSize = Options.Picking.BoxSize.ToString();
            session.diameter = Options.Picking.Diameter.ToString();
            if (Options.Classification.class2D)
            {
                session.TwoD = "True";
                session.ThreeD = "False"; 
            } else
            {
                session.TwoD = "False";
                session.ThreeD = "True";
            }
            session.NClasses = Options.Classification.NClasses.ToString();
            session.NParticles = Options.Classification.NParticles.ToString();
            session.warpSubDir = this.session.WarpSubDir;
            session.nFrames = this.session.NFrames.ToString();
            session.dosePerFrame = Options.Import.DosePerAngstromFrame.ToString();
            session.WindowsDir = Options.Import.Folder;
            session.ClassificationUrl = GetHost();
            session.UserEmail = GetCryosparcUserEmail();
            session.CryosparcLane = GetCryosparcLane();
            session.CryosparcLicense = GetCryosparcLicense();
            session.CryosparcProject = Options.Classification.CryosparcProject;
            session.CryosparcProjectName = Options.Classification.CryosparcProjectName;
            session.CryosparcProjectDir = Options.Classification.CryosparcProjectDir;
            session.particle_meta_path_windows = Path.Combine(Options.Import.Folder, "goodparticles_cryosparc_input.star");
            return (session); 
        } 

        public void RelaunchClass(object sender, RoutedEventArgs e)
        {
            NeedsRelaunchClass = true;
        }

        public CancellationTokenSource SwitchSessionCancellationTokenSource { get; set; } = new CancellationTokenSource();

        public async Task SwitchSession(string folder)
        {
            LogToFile("Switch session");
            LogToFile($"Active {active}");
            if (!active) return;

            LogToFile($"Watching {folder}");
            var directoryIsActive = await WaitForActiveFolder(SwitchSessionCancellationTokenSource.Token, folder);
            LogToFile($"Directory is {directoryIsActive}");
            if (!directoryIsActive) return;
            if (SwitchSessionCancellationTokenSource.IsCancellationRequested) return;
            if (Options.Stacker.GridScreening && !Directory.Exists(folder)) return;
            if (Options.Stacker.GridScreening && !directoryIsActiveDict.ContainsKey(folder)) return;
            if (directoryIsActiveDict[folder][1] == 0) return;

            if (active && !Waiting) { 
                SessionSwitchIndicator = true;
                Hibernate(); 
            }

            CurrentStackerFolder = folder;
            Application.Current.Dispatcher.Invoke(SetPaths);
            session = new EPUSession(Options);

            LastNGoodParticles = 0;
            Waiting = false;
            LogToFile("Start warp processing");
            NeedsNewOpticsGroup = true;
            NOpticsGroup = 0;

            Directory.CreateDirectory(Path.GetFullPath(Options.Import.Folder).TrimEnd(Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetFullPath(Options.Import.Folder).TrimEnd(Path.DirectorySeparatorChar) + @"\" + "microscopeXMLfiles");

            active = true;
            StartWARPProcessing();
        }

        public async void Activate()
        {
            //tokenSource = new CancellationTokenSource();
            session = new EPUSession(Options);
            active = true;
            LogToFile("Start processing");
            CanLoadDefaultOptions = false;
            

            if (Options.ProcessStacker && Options.Stacker.GridScreening)
            {
                var mw = (MainWindow)Application.Current.MainWindow;
                mw.ButtonStartProcessing.Content = "WAITING...";
                Waiting = true;
                mw.ButtonStartProcessing.Foreground = new LinearGradientBrush(Colors.DeepSkyBlue, Colors.DeepPink, 0);

                //First we start a file system watcher, in case there is a new potential stacker folder appearing
                FileWatcher = new FileSystemWatcher(Options.Stacker.Folder);
                FileWatcher.NotifyFilter = NotifyFilters.Attributes
                            | NotifyFilters.CreationTime
                            | NotifyFilters.DirectoryName
                            | NotifyFilters.FileName
                            | NotifyFilters.LastAccess
                            | NotifyFilters.LastWrite
                            | NotifyFilters.Security
                            | NotifyFilters.Size;
                FileWatcher.Created += OnCreated;
                FileWatcher.IncludeSubdirectories = false;
                FileWatcher.EnableRaisingEvents = true;

                //We set the current stacker folder to the newest directory
                try
                {
                    var newestDirectory = new DirectoryInfo(Options.Stacker.Folder).GetDirectories()
                           .OrderByDescending(d => d.CreationTimeUtc).First();
                    CurrentStackerFolder = newestDirectory.FullName;

                }
                catch (InvalidOperationException ex)
                {
                    return;
                }


                //Finally we wait until the directory is active
                SwitchSessionCancellationTokenSource = new CancellationTokenSource();
                var directoryIsActive = await WaitForActiveFolder(SwitchSessionCancellationTokenSource.Token, CurrentStackerFolder);
                if (!directoryIsActive)
                {
                    return;
                }
            } else
            {
                CurrentStackerFolder = Options.Stacker.Folder;
            }

            Application.Current.Dispatcher.Invoke(SetPaths);

            LogToFile("Activating!");
            LogToFile("ImportFolder: " + Options.Import.Folder + ". CurrentStackerFolder = " + CurrentStackerFolder + ". Options.Import.Folder = " + Options.Import.Folder + ". CurrentSessionName =  " + CurrentSessionName + ". CurrentXMLFolder = " + CurrentXMLFolder);
            NGoodParticles = 0;
            LastNGoodParticles = 0;
            Waiting = false;
            StartWARPProcessing();
        }

        public async void Hibernate()
        {
            try
            {
                SwitchSessionCancellationTokenSource.Cancel();
                SwitchSessionCancellationTokenSource.Dispose();
            } catch { }
            if (Options.ProcessClassification && TaskArray[3] != null) {
                if (TaskArray[3].Status == TaskStatus.Running)
                {
                    try
                    {
                        await TaskArray[3];
                    }
                    catch (AggregateException) { }
                }
            }
            active = false;
            StopWARPProcessing();
            CanLoadDefaultOptions = true;
        }

        async void OnCreated(object sender, FileSystemEventArgs e)
        {
            LogToFile($"Created {e.FullPath}");
            if (!Directory.Exists(e.FullPath)) return;

            try
            {
                SwitchSessionCancellationTokenSource.Cancel();
                SwitchSessionCancellationTokenSource.Dispose();
            }
            catch { };
            SwitchSessionCancellationTokenSource = new CancellationTokenSource();
            await SwitchSession(e.FullPath);
        }

        public void OnWatchedFileCreated(object sender, FileSystemEventArgs e)
        {
            var ext = Options.Import.Extension.Substring(Options.Import.Extension.IndexOf('.') + 1);
            LogToFile($"watched file created: {Path.GetFileName(e.FullPath)}. Starts with FoilHole: {Path.GetFileName(e.FullPath).StartsWith("FoilHole")}. Ends with {ext} is {Path.GetFileName(e.FullPath).EndsWith(ext)}");

            if (Path.GetFileName(e.FullPath).StartsWith("FoilHole") && Path.GetFileName(e.FullPath).EndsWith(ext)) {
                int[] vals = { 1, 1 };
                var watcher = (FileSystemWatcher)sender;
                directoryIsActiveDict[watcher.Path] = vals;
                LogToFile($"Adding {watcher.Path} to active dirs");
                try
                {
                    watcher.Created -= OnWatchedFileCreated;
                }
                catch (NullReferenceException) { };
            }
        }

        public string StackerIgnore { get; set; } = "";

        public void SetPaths() {
            var mw = (MainWindow)Application.Current.MainWindow;
            StackerIgnore = "";
            string importFolder = "";
            if (!Options.ProcessStacker)
            {
                importFolder = Options.Import.UserFolder;
                CurrentXMLFolder = Options.Import.MicroscopePCFolder;
                CurrentSessionName = GetSessionName(Options.Import.UserFolder);
                mw.CurrentSessionNameText.Text = "";
                if (Directory.Exists(Path.Combine(CurrentXMLFolder, CurrentSessionName)))
                {
                    if (IOHelper.CheckFolderPermission(Path.Combine(CurrentXMLFolder, CurrentSessionName)))
                    {
                        CurrentXMLFolder = Path.Combine(CurrentXMLFolder, CurrentSessionName);
                    }
                }
            }
            else if (Options.ProcessStacker && !Options.Stacker.GridScreening)
            {
                CurrentSessionName = GetSessionName(Options.Stacker.Folder);
                if (Options.Import.UserFolder.StartsWith(CurrentStackerFolder)) //To avoid futile cycles
                {
                    if (Path.GetFullPath(Options.Import.UserFolder) == Path.GetFullPath(CurrentStackerFolder))
                    {
                        importFolder = Path.Combine(Options.Import.UserFolder, "WARP");
                    } else
                    {
                        importFolder = Options.Import.UserFolder;
                    }
                    Uri fromUri = new Uri(CurrentStackerFolder);
                    Uri toUri = new Uri(importFolder);
                    var relUri = fromUri.MakeRelativeUri(toUri).ToString();
                    relUri = relUri.Replace('/', Path.DirectorySeparatorChar);
                    StackerIgnore = relUri.Trim(Path.DirectorySeparatorChar);
                    CurrentXMLFolder = Options.Import.MicroscopePCFolder;
                } else
                {
                    importFolder = Options.Import.UserFolder;
                    CurrentXMLFolder = Options.Import.MicroscopePCFolder;
                }
                mw.CurrentSessionNameText.Text = "";
                if (Directory.Exists(Path.Combine(CurrentXMLFolder, CurrentSessionName)))
                {
                    if (IOHelper.CheckFolderPermission(Path.Combine(CurrentXMLFolder, CurrentSessionName)))
                    {
                        CurrentXMLFolder = Path.Combine(CurrentXMLFolder, CurrentSessionName); 
                    }
                }
            } else
            {
                CurrentSessionName = GetSessionName(CurrentStackerFolder);
                importFolder = Path.Combine(Options.Import.UserFolder, CurrentSessionName);
                LogToFile($"Setting Options.Import.Folder to {importFolder}");
                CurrentXMLFolder = Path.Combine(Options.Import.MicroscopePCFolder, CurrentSessionName);
                if (importFolder.StartsWith(CurrentStackerFolder))
                {
                    if (Path.GetFullPath(importFolder) == Path.GetFullPath(CurrentStackerFolder)) {
                        importFolder = Path.Combine(importFolder, "WARP");
                    }
                    Uri fromUri = new Uri(CurrentStackerFolder);
                    Uri toUri = new Uri(importFolder);
                    var relUri = fromUri.MakeRelativeUri(toUri).ToString();
                    relUri = relUri.Replace('/', Path.DirectorySeparatorChar);
                    StackerIgnore = relUri.Trim(Path.DirectorySeparatorChar);
                }
                
                mw.CurrentSessionNameText.Text = $"  |  {CurrentSessionName}";
            }

            Directory.CreateDirectory(Path.GetFullPath(importFolder).TrimEnd(Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetFullPath(importFolder).TrimEnd(Path.DirectorySeparatorChar) + @"\" + "microscopeXMLfiles");

            Options.Import.Folder = importFolder;
            LogToFile("ImportFolder: " + Options.Import.Folder + ". CurrentStackerFolder = " + CurrentStackerFolder + ". StackerIgnore = " + StackerIgnore + ". CurrentSessionName =  " + CurrentSessionName + ". CurrentXMLFolder = " + CurrentXMLFolder);
        }

        public void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Import.UserFolder")
            {
                Application.Current.Dispatcher.Invoke(() => Options.Import.Folder = Options.Import.UserFolder);
            }
        }

        public string GetOpticsGroup(string movieName)
        {
            int shiftby = 0;
            string og = session.GetOpticsGroup(movieName);
            if (og != "0")
            {
                if (Options.Picking.NewOpticGroups && session.NOpticsGroup > 0)
                {
                    DateTime micDt = session.GetAcquisitionDt(movieName);
                    TimeSpan ts = micDt - oldestMicDt;
                    if (ts < TimeSpan.Zero)
                    {
                        oldestMicDt = micDt;
                        NeedsUpdateAllMics = true;
                    }
                    else
                    {
                        shiftby = session.NOpticsGroup * ((int)ts.TotalHours / Options.Picking.NewOpticGroupsEvery);
                        biggestShift = Math.Max(shiftby, biggestShift);
                    }
                }
                int newog = int.Parse(og) + shiftby;
                og = newog.ToString();
            }
            return (og);
        }

        public float2 GetBeamTilt(string movieName)
        {
            return session.GetBeamTilt(movieName);
        }

        private string GetSessionName(string EPUdir)
        {
            string fullPath = Path.GetFullPath(EPUdir).TrimEnd(Path.DirectorySeparatorChar);
            if (fullPath.EndsWith("WARP"))
            {
                fullPath = fullPath.Substring(0, fullPath.Length - "WARP".Length);
            }
            fullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar);
            return (Path.GetFileName(fullPath));
        }

        private void StartWARPProcessing()
        {
            StartProcessingSignal?.Invoke(null, null);
        }

        private void StopWARPProcessing()
        {
            StopProcessingSignal?.Invoke(null, null);
        }

        public async Task<bool> WaitForActiveFolder(CancellationToken cancellationToken, string folder)
        {
            LogToFile($"Watching directory {folder} to be active.");
            bool canAccessDir = false;
            if (Directory.Exists(folder))
            {
                try { Directory.GetAccessControl(folder); canAccessDir = true; } catch (UnauthorizedAccessException ex) { canAccessDir = false; }
                if (canAccessDir)
                {
                    var watcher = new FileSystemWatcher(folder);
                    watcher.NotifyFilter = NotifyFilters.Attributes
                             | NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size;
                    watcher.Created += OnWatchedFileCreated;
                    watcher.IncludeSubdirectories = true;
                    watcher.EnableRaisingEvents = true;
                } else
                {
                    return false;
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                //LogToFile($"Watching directory {folder} to be active.");
                if (directoryIsActiveDict.ContainsKey(folder))
                {
                    if (directoryIsActiveDict[folder][1] == 0)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                } else
                {
                    await Task.Delay(1000);
                    continue;
                }
                return true;
            }
            return false;
        }

        public string GetHost()
        {
            GlobalOptions GlobalOptions = new GlobalOptions();
            if (File.Exists("global.settings"))
            {
                GlobalOptions.Load("global.settings");
                return (GlobalOptions.ClassificationUrl);
            }
            return ("");
        }

        public string GetCryosparcUserEmail()
        {
            return Options.Classification.CryosparcUserEmail;
        }

        public string GetCryosparcLane()
        {
            return Options.Classification.CryosparcLane;
        }

        public string GetCryosparcLicense()
        {
            GlobalOptions GlobalOptions = new GlobalOptions();
            if (File.Exists("global.settings"))
            {
                GlobalOptions.Load("global.settings");
                return (GlobalOptions.CryosparcLicense);
            }
            return ("");
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        public bool IsOpticsGroupsComplete(int count = 0)
        {
            var g = session.OpticsGroupDict.Select((item) => item.Value[3]).ToList();
            count = count == 0 ? g.Count : count;
            if (!g.Contains("0") && g.Count == count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void LogToFile(string s)
        {
            string LogFile = Path.Combine(AppContext.BaseDirectory, "log.txt");
            Application.Current.Dispatcher.Invoke(() => {
                try
                {
                    using (StreamWriter outputFile = new StreamWriter(LogFile, true))
                    {
                        outputFile.WriteLine(s);
                    }
                }
                catch { 
                    LogToFile(s);
                };
            });
        }
    }
}
