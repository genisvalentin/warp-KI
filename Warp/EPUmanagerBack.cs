using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;
using Warp;
using Warp.Headers;
using Warp.Tools;
using System.Xml;
using System.Windows;
using System.Net.Http;
using Newtonsoft.Json;

namespace Warp {

    class EPUSession
    {
        private static readonly HttpClient client = new HttpClient();
        public List<Task> taskList = new List<Task>();
        private string sessionName = "";
        private string sessionParentFolder = "";
        public bool active = true;
        public bool ready = false;
        private bool finished = false;
        public bool NeedsRelaunchClass = true;

        public delegate void ProcessingEventHandler(object sender, RoutedEventArgs e);

        public event ProcessingEventHandler SessionStarted;
        public event ProcessingEventHandler Finished;

        private const string DefaultGlobalOptionsName = "global.settings";
        public static GlobalOptions GlobalOptions = new GlobalOptions();

        public string ImportFolder;

        public string StackerFolder;

        bool ProcessStacker;

        bool StackerGridScreening;

        bool ProcessClassification;

        string PickingMicroscopeXMLPath;

        bool PickingWriteOpticGroups;

        string Dim = "2D";

        string NClass = "2";

        int NewGroupsEveryHours = 1;

        public int WriteOpticGroupsN = 0;

        DateTime startTime;

        public EPUSession(string importFolder, string stackerFolder, bool processStacker, bool stackerGridScreening, string pickingMicroscopeXMLPath, bool pickingWriteOpticGroups, bool class2d, int nClass, bool processClassification, int newGroupsEveryHours, int writeOpticGroupsN)
        {
            ImportFolder = importFolder;
            StackerFolder = stackerFolder;
            ProcessStacker = processStacker;
            StackerGridScreening = stackerGridScreening;
            PickingMicroscopeXMLPath = pickingMicroscopeXMLPath;
            PickingWriteOpticGroups = pickingWriteOpticGroups;
            if (class2d)
            {
                Dim = "2D";
            } else
            {
                Dim = "3D";
            }
            NClass = nClass.ToString();
            ProcessClassification = processClassification;
            if (File.Exists(DefaultGlobalOptionsName))
                GlobalOptions.Load(DefaultGlobalOptionsName);
            startTime = DateTime.Now;
            NewGroupsEveryHours = newGroupsEveryHours;
            WriteOpticGroupsN = writeOpticGroupsN;
        }

        public async Task launchSession()
        {
            if (ProcessStacker)
            {
                setSessionName(StackerFolder);
                ready = true;
                if (StackerGridScreening)
                {
                    var watcher = new FileSystemWatcher(StackerFolder);

                    watcher.NotifyFilter = NotifyFilters.Attributes
                                         | NotifyFilters.CreationTime
                                         | NotifyFilters.DirectoryName
                                         | NotifyFilters.FileName
                                         | NotifyFilters.LastAccess
                                         | NotifyFilters.LastWrite
                                         | NotifyFilters.Security
                                         | NotifyFilters.Size;

                    watcher.Created += activateSession;
                    watcher.Filter = "*.tiff";
                    watcher.IncludeSubdirectories = true;
                    watcher.EnableRaisingEvents = true;
                } else
                {
                    active = true;
                }
            } else
            {
                setSessionName(ImportFolder);
                ready = true;
            }

            SessionStarted?.Invoke(null, null);

            //Check if the session is active
            while (!ready && active)
            {
                await Task.Delay(1000);
            }

            if (active)
            {
                //Import tiff files
                if (ProcessStacker)
                {
                    Console.WriteLine("Processing stacker");
                    Task tifTask = Task.Run(() =>
                    {
                        Stack(StackerFolder, ".tiff", true, true, true, ImportFolder, false, 1);
                    });
                    taskList.Add(tifTask);
                }

                //Import xml files and assign AFIS groups
                if (PickingWriteOpticGroups)
                {
                    Console.WriteLine("Processing XML");
                    string sessionXMLdir = Path.GetFullPath(PickingMicroscopeXMLPath).TrimEnd(Path.DirectorySeparatorChar) + "\\" + getSessionName();
                    string MicroscopePCpath = Path.GetFullPath(ImportFolder).TrimEnd(Path.DirectorySeparatorChar) + "\\" + "microscopeXMLfiles";
                    Directory.CreateDirectory(MicroscopePCpath);
                    Task xmlTask = Task.Run(() =>
                    {
                       StackXml(sessionXMLdir, "xml", true, MicroscopePCpath);
                    });
                    taskList.Add(xmlTask);
                    Task assignOpticGroupsTask = Task.Run(() =>
                    {
                       assignOpticGroups(ImportFolder, MicroscopePCpath, NewGroupsEveryHours);
                    });
                    taskList.Add(assignOpticGroupsTask);
                }

                //Launch classification
                if (ProcessClassification)
                {
                    Console.WriteLine("Processing classification");
                    Task parallelProcessingTask = launchParallelProcessing(getSessionName());
                    taskList.Add(parallelProcessingTask);
                }
                
                //Wait until all tasks are finished
                Task isRunningTask = Task.WhenAll(taskList);
                await isRunningTask;
            }

            finished = true;
            Finished?.Invoke(null, null);
        }

        private void activateSession(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }
            ready = true;
        }

        public void deactivateSession()
        {
            active = false;
        }

        private void setSessionName(string EPUdir)
        {
            string fullPath = Path.GetFullPath(EPUdir).TrimEnd(Path.DirectorySeparatorChar);
            sessionParentFolder = Path.GetDirectoryName(fullPath);
            sessionName = Path.GetFileName(fullPath);
        }

        public string getSessionName() => sessionName;

        private int guessNFrames(string EPUdir)
        {
            Console.WriteLine("Guessing frames");
            IEnumerable<string> fileList = Directory.EnumerateFiles(EPUdir, "*.tiff", SearchOption.AllDirectories);
            int nFiles = fileList.Count();
            while (nFiles < 6)
            {
                Thread.Sleep(1000);
                fileList = Directory.EnumerateFiles(EPUdir, "*.tiff", SearchOption.AllDirectories);
                nFiles = fileList.Count();
                Console.WriteLine("Nfiles: " + nFiles.ToString());
            }
            List<int> nFrames = new List<int>();
            int i = 0;
            foreach (var filename in fileList)
            {
                MapHeader Header = MapHeader.ReadFromFile(filename);
                nFrames.Add(Header.Dimensions.Z);
                i++;
                Console.WriteLine("filename:" + filename);
                if (i == 5) { break; }
            }
            int maxNFrames = nFrames.Max();
            Console.WriteLine($"Expected number of frames: {maxNFrames}");
            return (maxNFrames);
        }

        private void Stack(string FolderPath,
           string Extension,
           bool DoRecursiveSearch,
           bool DeleteExtraGain,
           bool DeleteWhenDone,
           string OutputPath,
           bool Compress,
           int NParallel)
        {
            Console.WriteLine("Stack");
            int NFrames = guessNFrames(FolderPath);

            if (!DeleteWhenDone)
                Directory.CreateDirectory(FolderPath + "original");

            List<string> HaveBeenProcessed = new List<string>();

            while (active)
            {
                Console.WriteLine("Stack active");
                List<string> FrameNames = new List<string>();
                List<string> GainRefNames = new List<string>();

                foreach (var filename in Directory.EnumerateFiles(FolderPath, "*" + Extension,
                                                                  DoRecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
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
                        else if (!filename.Contains("\\original\\"))
                            FrameNames.Add(filename);
                    }
                    catch
                    {
                    }
                }

                int NFiles = FrameNames.Count;

                if (NFiles == 0)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                Console.WriteLine("Found " + NFiles + " new stacks.");

                Thread.Sleep(1000);

                for (int f = 0; f < NFiles; f++)
                {
                    string FrameName = FrameNames[f];

                    MapHeader Header = MapHeader.ReadFromFilePatient(500, 100, FrameName, new int2(1), 0, typeof(float));

                     bool Success = false;

					while (!Success && active)
					{
						try
						{
							string NameOut = OutputPath + Helper.PathToNameWithExtension(FrameName);

							if (DeleteWhenDone)
								File.Move(FrameName, NameOut);
							else
							{
								File.Copy(FrameName, NameOut);
								File.Move(FrameName, FolderPath + "original/" + Helper.PathToNameWithExtension(FrameName));
							}

							HaveBeenProcessed.Add(FrameName);
							Success = true;

							Console.WriteLine("Done moving: " + Helper.PathToNameWithExtension(FrameName));
						}
						catch (Exception exc)
						{
							Console.WriteLine("Something went wrong moving " + Helper.PathToNameWithExtension(FrameName) + ":\n" + exc.ToString());
						}
					}
                }

                if (DeleteExtraGain)
                    foreach (var gainRefName in GainRefNames)
                        File.Delete(gainRefName);

                Thread.Sleep(1000);
            }
        }

        private void StackXml(string FolderPath, string Extension, bool DoRecursiveSearch, string OutputPath)
        {
            OutputPath = Path.GetFullPath(OutputPath).TrimEnd(Path.DirectorySeparatorChar) + "\\";

            List<string> HaveBeenProcessed = new List<string>();

            while (active)
            {
                List<string> FrameNames = new List<string>();
                List<string> GainRefNames = new List<string>();

                foreach (var filename in Directory.EnumerateFiles(FolderPath, "*." + Extension,
                                                                  DoRecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        if (HaveBeenProcessed.Contains(filename))
                            continue;

                        FrameNames.Add(filename);
                    }
                    catch
                    {
                    }
                }

                int NFiles = FrameNames.Count;

                if (NFiles == 0)
                {
                    //await Task.Delay(1000);
                    Thread.Sleep(1000);
                    continue;
                }

                //Console.WriteLine("Session " + getSessionName() + ": Found " + NFiles + " new stacks.");

                //await Task.Delay(1000);
                Thread.Sleep(1000);

                for (int f = 0; f < NFiles; f++)
                {
                    string FrameName = FrameNames[f];

                    bool Success = false;

                    while (!Success && active)
                    {
                        string NameOut = OutputPath + Path.GetFileName(FrameName);
                        try
                        {

                            File.Copy(FrameName, NameOut);

                            HaveBeenProcessed.Add(FrameName);
                            Success = true;

                            //Console.WriteLine("Done moving: " + NameOut);
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("Something went wrong moving " + NameOut + ":\n" + exc.ToString());
                        }
                    }
                }

                //await Task.Delay(1000);
                Thread.Sleep(1000);
            }
        }

        public async Task launchParallelProcessing(string sessionName)
        {
            string sessionPath = GlobalOptions.ClassificationMountPoint;
            if (NeedsRelaunchClass)
            {
                NeedsRelaunchClass = false;
                string url = GlobalOptions.ClassificationUrl;
                Dictionary<string, string> sessionInfo = new Dictionary<string, string>();
                sessionInfo.Add("sessionPath", sessionPath);
                sessionInfo.Add("sessionName", sessionName);
                if (Dim == "2D")
                {
                    sessionInfo.Add("3D", "False");
                    sessionInfo.Add("2D", "True");
                }
                else
                {
                    sessionInfo.Add("3D", "True");
                    sessionInfo.Add("2D", "False");
                }
                sessionInfo.Add("NClasses", NClass);
                var objAsJson = JsonConvert.SerializeObject(sessionInfo);
                Console.WriteLine("Posting session info to preprocess1:");
                Console.WriteLine(objAsJson);
                try
                {
                    StringContent content = new StringContent(objAsJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }

        private void assignOpticGroups(string sessionDir, string microscopeXMLpath, int newGroupsEveryHours)
        {
            Console.WriteLine("Assigning optics groups task started");

            string starfile = Path.GetFullPath(sessionDir).TrimEnd(Path.DirectorySeparatorChar) + "\\" + "goodparticles_BoxNet2Mask_20180918.star";

            while (active && !File.Exists(starfile))
            {
                Thread.Sleep(5000);
            }

            Console.WriteLine("Assigning optics groups found star file");

            AFISstar s = new AFISstar(starfile);
            while (s.getMicrographList().Count() < (WriteOpticGroupsN+1) && active)
            {
                Thread.Sleep(5000);
                s = new AFISstar(starfile);
                Console.WriteLine("Not enough micrographs to assign optic groups");
            }

            if (active)
            {
                Console.WriteLine("Assigning optics groups has enough micrographs");
                int nclusters = s.estimateNumberOfOpticsGroups(WriteOpticGroupsN, microscopeXMLpath);
                Console.WriteLine("Nclusters: " + nclusters.ToString());

                int shiftby = 0;

                while (active)
                {
                    s = new AFISstar(starfile);
                    TimeSpan ts = DateTime.Now - startTime;
                    shiftby = nclusters * (int)ts.TotalHours / newGroupsEveryHours;
                    Console.WriteLine("Shiftby:" + shiftby.ToString());
                    s.AddOpticGroups(microscopeXMLpath, nclusters, shiftby);
                    s.Save(Path.GetFullPath(sessionDir).TrimEnd(Path.DirectorySeparatorChar) + "\\" + "goodparticles_BoxNet2Mask_20180918_opticgroups.star");
                    Thread.Sleep(5000);
                }
            }
        }

        public bool isActive()
        {
            return (active);
        }
    }

    public class EPUSessionManager
    {
        //string archiveDir;
        List<EPUSession> sessionList = new List<EPUSession>();
        private List<Task> taskList = new List<Task>();

        // Declare the delegate (if using non-generic pattern).
        public delegate void ProcessingEventHandler(object sender, RoutedEventArgs e);

        // Declare the event.
        public event ProcessingEventHandler StartProcessingSignal;
        public event ProcessingEventHandler StopProcessingSignal;

        private Options Options;

        public string ImportFolder = "";
        public string ImportFolderSession;
        string StackerFolder;
        bool ProcessStacker;
        bool StackerGridScreening;
        string PickingMicroscopeXMLPath;
        bool PickingWriteOpticGroups;
        int PickingWriteOpticGroupsN;

        public EPUSessionManager(Options options)
        {
            Options = options;
        }

        public int startManager()
        {
            if (ImportFolder == "")
            {
                ImportFolder = Options.Import.Folder;
            }
            ImportFolderSession = ImportFolder;
            StackerFolder = Options.Stacker.Folder;
            ProcessStacker = Options.ProcessStacker;
            StackerGridScreening = Options.Stacker.GridScreening;
            PickingMicroscopeXMLPath = Options.Picking.MicroscopeXMLPath;
            PickingWriteOpticGroups = Options.Picking.WriteOpticGroups;
            PickingWriteOpticGroupsN = Options.Picking.WriteOpticGroupsN;

            setStackerFolder(StackerFolder);

            if (Options.Stacker.GridScreening)
            {
                var watcher = new FileSystemWatcher(Options.Stacker.Folder);
                watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size;
                watcher.Created += OnCreated;
                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = true;

                DirectoryInfo[] directories = new DirectoryInfo(Options.Stacker.Folder).GetDirectories();
                if (directories.Length > 0)
                {
                    setStackerFolder(directories.OrderByDescending(d => d.LastWriteTimeUtc).First().FullName);
                }
            }

            bool NeedsNewSession = true;
            if (sessionList.Count() > 0)
            {
                foreach (EPUSession s in sessionList)
                {
                    if (s.ImportFolder == ImportFolderSession && s.StackerFolder == StackerFolder)
                    {
                        NeedsNewSession = false;
                        s.active = true;
                        s.WriteOpticGroupsN = Options.Picking.WriteOpticGroupsN;
                        Task t = s.launchSession();
                        break;
                    }
                }
            }
            if (NeedsNewSession)
            {
                EPUSession session = new EPUSession(ImportFolderSession, StackerFolder, ProcessStacker, StackerGridScreening, PickingMicroscopeXMLPath, PickingWriteOpticGroups, Options.Classification.class2D, Options.Classification.NClasses, Options.ProcessClassification, Options.Picking.NewOpticGroupsEvery, Options.Picking.WriteOpticGroupsN);
                session.SessionStarted += StartWARPProcessing;
                sessionList.Add(session);
                Task t = session.launchSession();
            }
            return (0);
        }

        public void setStackerFolder(string folder)
        {
            ImportFolder = Path.GetFullPath(ImportFolder).TrimEnd(Path.DirectorySeparatorChar) + "\\";
            ImportFolderSession = ImportFolder;
            if (Options.ProcessStacker)
            {
                string f = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
                f = f + "\\";
                StackerFolder = f;
            }
            if (Options.Stacker.GridScreening && Options.ProcessStacker)
            {
                string f2 = ImportFolder + getSessionName(folder);
                f2 = f2 + "\\";
                Directory.CreateDirectory(f2);
                ImportFolderSession = f2;
            }
        }

        public int stopManager()
        {
            StopWARPProcessing();
            foreach (EPUSession session in sessionList)
            {
                session.deactivateSession();
            }
            return (0);
        }

        public void StartWARPProcessing(object sender, RoutedEventArgs e)
        {
            StartProcessingSignal?.Invoke(null, null);
        }

        public void StopWARPProcessing()
        {
            StopProcessingSignal?.Invoke(null, null);
        }

        private async void OnCreated(object sender, FileSystemEventArgs e)
        {
            foreach (EPUSession s in sessionList)
            {
                s.deactivateSession();
            }
            //Task isRunningTask = Task.WhenAll(taskList);
            //await isRunningTask;
            StopWARPProcessing();
            setStackerFolder(e.FullPath);
            EPUSession session = new EPUSession(ImportFolderSession, StackerFolder, ProcessStacker, StackerGridScreening, PickingMicroscopeXMLPath, PickingWriteOpticGroups, Options.Classification.class2D, Options.Classification.NClasses, Options.ProcessClassification, Options.Picking.NewOpticGroupsEvery, Options.Picking.WriteOpticGroupsN);
            session.SessionStarted += StartWARPProcessing;
            Task t = session.launchSession();
            sessionList.Add(session);
            taskList.Add(t);
            //startManager();
            //EPUSession session = new EPUSession(Options);  
            //session.SessionStarted += StartWARPProcessing;
            //Task t = session.launchSession();
            //sessionList.Add(session);
            //taskList.Add(t);
            //Then we create a new session and add it to the 
            //Task sessionTask = session.launch(UserInput, EPUdir);
        }

        private string getSessionName(string EPUdir)
        {
            string fullPath = Path.GetFullPath(EPUdir).TrimEnd(Path.DirectorySeparatorChar);
            return (Path.GetFileName(fullPath));

        }

        public void RelaunchClass(object sender, RoutedEventArgs e)
        {
            foreach (EPUSession s in sessionList)
            {
                if (s.isActive())
                {
                    if (Options.ProcessClassification)
                    {
                        s.NeedsRelaunchClass = true;
                        Console.WriteLine("Relaunch 2d: " + s.ImportFolder);
                    }

                }
            }
        }
    }
}
