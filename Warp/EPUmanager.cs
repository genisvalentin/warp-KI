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
        private Dictionary<string,List<string>> OpticsGroupDict = new Dictionary<string, List<string>>();
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
                LogToFile($"File {filename} does not contain {ignore}");
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
                    LogToFile(
                    $"{e.GetType().Name}: The write operation could not " +
                    "be performed because the specified " +
                    "part of the file is locked.");
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
                movies = new string[OpticsGroupDict.Count()];
                foreach (KeyValuePair<string, List<string>> m in OpticsGroupDict)
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
            //The bin factor is important to find the foil hole pattern. We try from bin=25 to bin=50 to find the right bin factor.
            int q = 0;
            do {
                if (a[0].X == 0 && a[0].Y == 0)
                {
                    vector1 = OpticGroups.GetVectors(coords.ToArray(), 90, 0, bin: bin);
                    var atan = -Math.Atan2(vector1.Y, vector1.X) * 180 / Math.PI - 10;
                    q = (int)atan;
                    LogToFile($"Start search angle: {q}");
                }
                // Quantifoil grids have an almost square pattern. Therefore, the
                // second vector will be at 90 +- 10 degrees from the first
                if ((a[0].X + a[0].Y) * (a[1].X + a[1].Y) == 0) vector2 = OpticGroups.GetVectors(coords.ToArray(), 20, 1, bin: bin, start_angle: q); 

                LogToFile($"Bin: {bin}");
                LogToFile($"Vectors {vector1.X},{vector1.Y}; {vector2.X},{vector2.Y}");
                a = OpticGroups.RefineVectors(coords.ToArray(), vector1, vector2);
                LogToFile($"Refined vectors {a[0].X},{a[0].Y}; {a[1].X},{a[1].Y}");
                if (bin > 50) break;
                if ( (a[0].X + a[0].Y) * (a[1].X + a[1].Y) == 0) //if any of the two vectors is (0,0), we try again with a higher bin.
                {
                    bin += 5;
                    continue;
                }
                break;
            } while (true);

            //Plot Optic Groups        
            var plt = new ScottPlot.Plot(400, 300);
            var coordx = coords.Select(x => x.X).ToArray();
            var coordy = coords.Select(y => y.Y).ToArray();
            plt.AddScatter(coordx, coordy, lineWidth: 0);

            if ((a[0].X + a[0].Y) * (a[1].X + a[1].Y) == 0)
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

        public void UpdateOpticsGroup()
        {
            Coord[] coords;
            int[][] indexes;
            Coord[] centroids;
            lock (OpticsGroupDict)
            {
                coords = OpticsGroupDict.Select(x => new Coord(double.Parse(x.Value[1]), double.Parse(x.Value[2]))).ToArray();
                indexes = OpticGroups.GetCentroidIndexes(coords, v1, v2);
                centroids = OpticGroups.GetCentroids(indexes, v1, v2);
                double[][] providedCentroids = centroids.Select(x => new double[] { x.X, x.Y }).ToArray();

                var kmeans = new KMeans(k: providedCentroids.Length);
                kmeans.Centroids = providedCentroids;
                var groups = kmeans.Clusters.Decide(coords.Select(x => new double[] { x.X, x.Y }).ToArray());

                int counter = 0;
                if (groups.Length == OpticsGroupDict.Count)
                {
                    foreach (var mic in OpticsGroupDict)
                    {
                        mic.Value[3] = groups[counter].ToString();
                        counter++;
                    }
                }
            }

            NOpticsGroup = indexes.Length;

            //Plot Optic Groups        
            var plt = new ScottPlot.Plot(400, 300);
            var coordx = coords.Select(x => x.X).ToArray();
            var coordy = coords.Select(y => y.Y).ToArray();
            plt.AddScatter(coordx, coordy, lineWidth: 0);

            var refcentroidX = centroids.Select(x => x.X).ToArray();
            var refcentroidY = centroids.Select(y => y.Y).ToArray();
            var sp3 = plt.AddScatter(refcentroidX, refcentroidY, lineWidth: 0, markerSize: 15);
            sp3.MarkerShape = ScottPlot.MarkerShape.openCircle;

            plt.SaveFig(Path.GetFullPath(Options.Import.Folder).TrimEnd(Path.DirectorySeparatorChar) + @"\opticsGroups.png");

        }

        #region old plot optics groups
        private void PlotOpticsGroups()
        {
            Dictionary<int, List<double[]>> points = new Dictionary<int, List<double[]>>();
            lock(OpticsGroupDict)
            {
                foreach (KeyValuePair<string, List<string>> m in OpticsGroupDict)
                {
                    double[] shifts = new double[2];
                    shifts[0] = double.Parse(m.Value[1], System.Globalization.CultureInfo.InvariantCulture);
                    shifts[1] = double.Parse(m.Value[2], System.Globalization.CultureInfo.InvariantCulture);
                    int group = int.Parse(m.Value[3], System.Globalization.CultureInfo.InvariantCulture);
                    if (group == 0)
                    {
                        continue;
                    } 
                    if (!points.ContainsKey(group))
                    {
                        List<double[]> l = new List<double[]>();
                        l.Add(shifts);
                        points.Add(group, l);
                    }
                    else
                    {
                        points[group].Add(shifts);
                    }
                }
            }

            string[] colors = new string[39] { "#D9FFFF", "#CC80FF", "#C2FF00", "#FFB5B5", "#909090", "#3050F8", "#FF0D0D", "#90E050", "#B3E3F5", "#AB5CF2", "#8AFF00", "#BFA6A6", "#F0C8A0", "#FF8000", "#FFFF30", "#1FF01F", "#80D1E3", "#8F40D4", "#3DFF00", "#E6E6E6", "#BFC2C7", "#A6A6AB", "#8A99C7", "#9C7AC7", "#E06633", "#F090A0", "#50D050", "#C88033", "#7D80B0", "#C28F8F", "#668F8F", "#BD80E3", "#FFA100", "#A62929", "#5CB8D1", "#702EB0", "#00FF00", "#94FFFF", "#94E0E0" };
            var plt = new ScottPlot.Plot(400, 300);
            foreach (KeyValuePair<int,List<double[]>> point in points)
            {
                double[] shiftx = new double[point.Value.Count()];
                double[] shifty = new double[point.Value.Count()];
                int counter = 0;
                foreach (double[] shifts in points[point.Key])
                {
                    shiftx[counter] = shifts[0];
                    shifty[counter] = shifts[1];
                    counter++;
                }
                plt.AddScatter(shiftx, shifty, lineWidth: 0);
            }
            plt.SaveFig(Path.GetFullPath(Options.Import.Folder).TrimEnd(Path.DirectorySeparatorChar) + @"\opticsGroups.png");
        }
        #endregion

        #region old silhouette
        private double Silhouette(alglib.kmeansreport irep, double[,] shifts, int p)
        {
            double sum = 0;
            int c = 0;
            for (int i = 0; i<irep.npoints; i++)
            {
                if (irep.cidx[i] == irep.cidx[p])
                {
                    double dist = Math.Sqrt(Math.Pow((shifts[p, 0] - shifts[i, 0]), 2) + Math.Pow((shifts[p, 1] - shifts[i, 1]), 2));
                    sum += dist;
                    c++;
                }
            }
            double a = sum / (c - 1);

            Dictionary<int, double> minAvgDist = new Dictionary<int, double>();
            Dictionary<int, int> nPoints = new Dictionary<int, int>();
            for (int j = 0; j<irep.k; j++)
            {
                if (irep.cidx[p] != j) {
                    double sum2 = 0;
                    nPoints[j] = 0;
                    for (int i = 0; i < irep.npoints; i++)
                    {
                        if (irep.cidx[i] == j)
                        {
                            double dist = Math.Sqrt(Math.Pow((shifts[p, 0] - shifts[i, 0]), 2) + Math.Pow((shifts[p, 1] - shifts[i, 1]), 2));
                            sum2 = sum2 + dist;
                            nPoints[j] ++;
                        }
                    }
                    double tmpb = sum2 / nPoints[j];
                    minAvgDist.Add(j, tmpb);
                }
            }

            double b = minAvgDist.First().Value;
            foreach (var dist in minAvgDist)
            {
                if (dist.Value < b) {
                    b = dist.Value;
                }
            }

            double s = (b - a) / Math.Max(a, b);
            if (Double.IsNaN(s))
            {
                return (0);
            }
            return s;
        }

        private alglib.kmeansreport ClusterMicrographs(double[,] shifts, int ngroups = 10)
        {
            alglib.clusterizercreate(out alglib.clusterizerstate s);
            alglib.clusterizersetpoints(s, shifts, 2);
            alglib.clusterizersetkmeanslimits(s, 5, 0);
            alglib.clusterizerrunkmeans(s, ngroups, out alglib.kmeansreport irep);

            return (irep);
        }

        public void UpdateOldestMic()
        {
            lock (OpticsGroupDict)
            {
                oldestMicrographDt = DateTime.Parse(OpticsGroupDict.OrderBy(x => DateTime.Parse(x.Value[0])).First().Value[0]);
            }
        }
        #endregion

        public string GetOpticsGroup(string movieName)
        {
            lock (OpticsGroupDict)
            {
                if (OpticsGroupDict.ContainsKey(movieName))
                {
                    if (OpticsGroupDict[movieName][3] != "0")
                    {
                        return (OpticsGroupDict[movieName][3]);
                    }
                }
                ImportXMLfile(movieName);
                if (OpticsGroupDict.ContainsKey(movieName))
                {
                    if (OpticsGroupDict[movieName][3] != "0")
                    {
                        return (OpticsGroupDict[movieName][3]);
                    }
                    else
                    {
                        return ("0");
                    }
                }
                else
                {
                    return ("0");
                }
            }
        }

        private string FindCenter(string xshift, string yshift)
        {
            double x = double.Parse(xshift, System.Globalization.CultureInfo.InvariantCulture);
            double y = double.Parse(yshift, System.Globalization.CultureInfo.InvariantCulture);

            if (NOpticsGroup==0 || OpticGroupsCentroids == null)
            {
                return ("0");
            }

            if (OpticGroupsCentroids.Length == 0)
            {
                return ("0");
            }

            int clusterIndex = 1;
            int counter = 0;
            double minDist = 0;
            foreach (var p in OpticGroupsCentroids)
            {
                double dist = Math.Sqrt(Math.Pow((x - p.X), 2) + Math.Pow((y - p.Y), 2));
                if (minDist == 0) { minDist = dist; }
                if (dist < minDist)
                {
                    minDist = dist;
                    clusterIndex = counter + 1;
                }
                counter++;
            }

            return (clusterIndex.ToString());
        }

        public void UpdateXmlFileList(string CurrentXMLFolder)
        {
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

        private void ImportXMLfile(string FrameName) {
            string XMLfile = default;
            int index = Helper.PathToName(FrameName).IndexOf("_fractions");
            if (index < 0) {
                LogToFile(FrameName + "does not contain the _fractions substring. Cannot look for XML file.");
                return;
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
                    else
                    {
                        OpticsGroupDict[FrameName] = metadata;
                    }
                }
            }
            else
            {
                LogToFile(XMLfile + ".xml was not found.");
            }
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
                    metadatalist[3] = FindCenter(xvalue, yvalue);
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
            lock (OpticsGroupDict)
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
                catch { LogToFile(s); };
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
        private bool NeedsRelaunchClass = false;
        private Dictionary<string, int[]> directoryIsActiveDict = new Dictionary<string, int[]>();

        public delegate void ProcessingEventHandler(object sender, RoutedEventArgs e);
        public event ProcessingEventHandler StartProcessingSignal;
        public event ProcessingEventHandler StopProcessingSignal;

        public event PropertyChangedEventHandler PropertyChanged;

        private DateTime oldestMicDt = DateTime.Now;

        public bool NeedsUpdateAllMics = false;

        public string CurrentXMLFolder = "";
        public string CurrentSessionName = "";

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
        public int NGoodParticles = 0;
        public string PathBoxNetFiltered = "";
        private DateTime latestClassLaunchTime;

        //public CancellationTokenSource tokenSource = new CancellationTokenSource();
        Task ClassificationTask;

        private int LastNGoodParticles = 0;

        private cryosparcClient.Client cryosparcclient = new cryosparcClient.Client();

        public int NOpticsGroup = 0;

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

            if (!OpticsGroupTable.HasColumn("rlnOpticsGroupName"))
                OpticsGroupTable.AddColumn("rlnOpticsGroupName", "1");

            if (!OpticsGroupTable.HasColumn("rlnOpticsGroup"))
                OpticsGroupTable.AddColumn("rlnOpticsGroup", "1");

            List<List<string>> NewRows = new List<List<string>>();
            //We always add a row for optics group 0 in case some micrographs could not be assigned to an optics group.
            for (int i = 0; i < ( nOpticsGroup +1 ); i++)
            {
                string[] Row = Helper.ArrayOfConstant("0", OpticsGroupTable.ColumnCount);
                Row[OpticsGroupTable.GetColumnID("rlnOpticsGroupName")] = i.ToString();
                Row[OpticsGroupTable.GetColumnID("rlnOpticsGroup")] = i.ToString();
                NewRows.Add(Row.ToList());
            }
            OpticsGroupTable.AddRow(NewRows);
        }

        public bool SessionLoopFinished { get; set; } = false;

        public bool NeedsNewOpticsGroup { get; set; } = false;

        public void SessionLoop(CancellationToken cancellationToken)
        {
            LogToFile("EPU session manager loop");

            Dictionary<string, string> classificationStatus = new Dictionary<string, string> { { "status", ""} };
            Dictionary<string, string> launchClassificationStatus = new Dictionary<string, string>();

            //We do nothing if we are still waiting for a Current Stacker Folder
            if (Options.ProcessStacker && Options.Stacker.GridScreening && !Directory.Exists(CurrentStackerFolder))
            {
                return;
            }

            for (int i = 0; i < TaskArray.Length; i++)
            {

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

                    if (Options.Picking.WriteOpticGroups && session.NOpticsGroup > 0)
                    {
                        TaskArray[i] = Task.Run(() =>
                        {
                            session.UpdateOpticsGroup();
                            canaddopticgroups = session.NOpticsGroup > 0;
                            NOpticsGroup = session.NOpticsGroup;
                            PopulateOpticsGroupTable(biggestShift + session.NOpticsGroup);
                        }).ContinueWith(_ =>
                        {
                            AggregateException ex = _.Exception;
                            LogToFile($"UpdateOpticsGroup task exception {ex.GetType().Name}: {ex.Message}");
                        }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                    continue;
                }

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
                                    if ((LastNGoodParticles + RequestedParticles) < NGoodParticles)
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
                            LastNGoodParticles = NGoodParticles;
                            LogToFile("Launch classification");
                            latestClassLaunchTime = DateTime.Now;
                            cryosparcClient.SessionInfo sessionInfo = OptionsToSessionInfo();
                            string BoxNetSuffix = Helper.PathToNameWithExtension(Options.Picking.ModelPath);
                            string particle_meta_path = Path.Combine(Options.Import.Folder, "goodparticles_cryosparc_input.star");
                            if (File.Exists(particle_meta_path)) File.Delete(particle_meta_path);
                            if (File.Exists(Path.Combine(Options.Import.Folder, $"goodparticles_{BoxNetSuffix}.star")))
                            {
                                File.Copy(Path.Combine(Options.Import.Folder, $"goodparticles_{BoxNetSuffix}.star"), particle_meta_path);
                                ClassificationTask = Task.Run(() => cryosparcclient.Run(sessionInfo, cancellationToken)).ContinueWith(_ =>
                                {
                                    AggregateException ex = _.Exception;
                                    LogToFile($"UpdateOpticsGroup task exception {ex.GetType().Name}: {ex.Message}");
                                }, TaskContinuationOptions.OnlyOnFaulted);
                                NeedsRelaunchClass = false;
                            }
                        }
                    }
                    continue;
                }
            }

            //Update GUI
            Application.Current.Dispatcher.Invoke(() => {
                Options.Classification.Results = CurrentClassificationStatus;
                Options.Classification.Countdown = Countdown;
                var mw = (MainWindow)Application.Current.MainWindow;
                if (canaddopticgroups)
                {
                    mw.TextBlockNOpticsGroup.Text = NOpticsGroup.ToString();
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
        }

        private cryosparcClient.SessionInfo OptionsToSessionInfo()
        {
            cryosparcClient.SessionInfo session = new cryosparcClient.SessionInfo();
            session.sessionPath = Options.Classification.ClassificationMountPoint.TrimEnd('/');
            if (Options.Stacker.GridScreening) session.sessionPath += $"/{CurrentSessionName}";
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

            //tokenSource.Cancel();
            //tokenSource.Dispose();

            if (active && !Waiting) { Hibernate(); }

            CurrentStackerFolder = folder;
            Application.Current.Dispatcher.Invoke(SetPaths);
            session = new EPUSession(Options);
            LastNGoodParticles = 0;
            Waiting = false;
            LogToFile("Start warp processing");

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
            /*
            try
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }
            catch { }*/
            try
            {
                SwitchSessionCancellationTokenSource.Cancel();
                SwitchSessionCancellationTokenSource.Dispose();
            } catch { }
            if (Options.ProcessClassification && ClassificationTask != null) {
                if (ClassificationTask.Status == TaskStatus.Running)
                {
                    try
                    {
                        await ClassificationTask;
                    }
                    catch (AggregateException)
                    {
                        if (ClassificationTask.Status == TaskStatus.Canceled)
                        {
                            LogToFile("Classification task was cancelled");
                        }
                    }
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
                CurrentXMLFolder = Options.Picking.MicroscopeXMLPath;
                CurrentSessionName = GetSessionName(Options.Import.UserFolder);
                mw.CurrentSessionNameText.Text = "";
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
                    CurrentXMLFolder = Options.Picking.MicroscopeXMLPath;
                } else
                {
                    importFolder = Options.Import.UserFolder;
                    CurrentXMLFolder = Options.Picking.MicroscopeXMLPath;
                }
                mw.CurrentSessionNameText.Text = "";
            } else
            {
                CurrentSessionName = GetSessionName(CurrentStackerFolder);
                importFolder = Path.Combine(Options.Import.UserFolder, CurrentSessionName);
                LogToFile($"Setting Options.Import.Folder to {importFolder}");
                CurrentXMLFolder = Path.Combine(Options.Picking.MicroscopeXMLPath, CurrentSessionName);
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
                LogToFile($"Watching directory {folder} to be active.");
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
                catch { LogToFile(s); };
            });
        }
    }
}
