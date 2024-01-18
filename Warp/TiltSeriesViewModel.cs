using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Warp.Tools;

namespace Warp
{
    public enum jobStatus
    {
        Finished,
        Waiting,
        Started,
        Failed
    }

    public class TiltSeriesViewModel : WarpBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region ICommands
        public ICommand ProcessCommand { get; private set; }
        public ICommand CopyTomoToClipboardDelegate { get; private set; }
        public ICommand OpenLogCommand { get; private set; }
        public ICommand OpenStMovieCommand { get; private set; }
        public ICommand OpenTomoMovieCommand { get; private set; }
        public ICommand ClearSeriesCommand { get; private set; }

        #endregion

        #region Properties

        public OptionsAretomoSettings LocalAretomoSettings { get; set; }
        public OptionsAretomoSettings GlobalAretomoSettings;
        public OptionsSshSettings Settings;


        private bool overrideGlobalSettings;
        [WarpSerializable]
        public bool OverrideGlobalSettings
        {
            get => overrideGlobalSettings;
            set
            {
                if (overrideGlobalSettings != value)
                {
                    overrideGlobalSettings = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OverrideGlobalSettings)));
                }
            }
        }

        private bool isTomogramAvailable;

        private bool isNotProcessing;
        public bool IsNotProcessing
        {
            get => isNotProcessing;
            set
            {
                if (isNotProcessing != value)
                {
                    isNotProcessing = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotProcessing)));
                }

            }
        }

        public bool IsTomogramAvailable
        {
            get => isTomogramAvailable;
            set
            {
                if (isTomogramAvailable != value)
                {
                    isTomogramAvailable = value;
                    if (!value) { TomoPngSource = ""; }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTomogramAvailable)));
                }
            }
        }

        private bool isQueued;
        [WarpSerializable]
        public bool IsQueued
        {
            get => isQueued;
            set
            {
                if (value != isQueued)
                {
                    isQueued = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsQueued)));
                    WriteAretomoSettingsToXml();
                }
            }
        }

        public string Name { get; set; }

        private string displayName;

        [WarpSerializable]
        public string DisplayName
        {
            get => displayName;
            set
            {
                if (value != displayName)
                {
                    displayName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                    WriteAretomoSettingsToXml();
                }
            }
        }

        private string aretomoRefinedTiltAxis;

        [WarpSerializable]
        public string AretomoRefinedTiltAxis
        {
            get => aretomoRefinedTiltAxis;
            set
            {
                if (value != aretomoRefinedTiltAxis)
                {
                    aretomoRefinedTiltAxis = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AretomoRefinedTiltAxis)));
                    WriteAretomoSettingsToXml();
                }
            }
        }

        [WarpSerializable]
        public decimal PixelSpacing { get; set; }

        [WarpSerializable]
        public decimal PixelSpacingUnbinned { get; set; }

        public bool SuccessfullyReadXmlProperties = false;

        public bool IsInitialized { get; set; } = false;

        private string mdocFile;
        public string MdocFile
        {
            get => mdocFile;
            set
            {
                mdocFile = value;
                Name = Path.GetFileNameWithoutExtension(value);
                DisplayName = Path.GetFileNameWithoutExtension(value);
            }
        }

        private jobStatus warpStatus = jobStatus.Waiting;
        public jobStatus WarpStatus
        {
            get => warpStatus;
            set
            {
                if (warpStatus != value)
                {
                    warpStatus = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WarpStatus)));
                }
            }
        }

        private jobStatus newstackStatus = jobStatus.Waiting;
        public jobStatus NewstackStatus
        {
            get => newstackStatus;
            set
            {
                if (value != newstackStatus)
                {
                    newstackStatus = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewstackStatus)));
                }
            }
        }

        private jobStatus aretomoStatus = jobStatus.Waiting; 
        public jobStatus AretomoStatus
        {
            get => aretomoStatus;
            set
            {
                if (aretomoStatus != value)
                {
                    aretomoStatus = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AretomoStatus)));
                }
            }
        }
        private jobStatus denoiseStatus = jobStatus.Waiting;
        public jobStatus DenoiseStatus
        {
            get => denoiseStatus;
            set
            {
                if (denoiseStatus != value)
                {
                    denoiseStatus = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DenoiseStatus)));
                }
            }
        }

        private jobStatus aretomo2PngStatus = jobStatus.Waiting;
        public jobStatus Aretomo2PngStatus
        {
            get => aretomo2PngStatus;
            set
            {
                aretomo2PngStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Aretomo2PngStatus)));

            }
        }

        private int _aretomoHasProcessed = 0;
        public int AretomoHasProcessed
        {
            get => _aretomoHasProcessed;
            set
            {
                if (value != _aretomoHasProcessed)
                {
                    _aretomoHasProcessed = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AretomoHasProcessed)));
                }
            }
        }

        public int _aretomoHasToProcess = 1;
        public int AretomoHasToProcess
        {
            get => _aretomoHasToProcess;
            set
            {
                if (value != _aretomoHasToProcess)
                {
                    _aretomoHasToProcess = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AretomoHasToProcess)));
                }
            }
        }

        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        private BitmapImage _tomoBitmap;
        public BitmapImage TomoBitmap
        {
            get => _tomoBitmap;
            set
            {
                _tomoBitmap = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TomoBitmap)));
            }
        }

        private string _tomoPngSource;
        private float aretomoTiltOffset;

        public string TomoPngSource
        {
            get => _tomoPngSource;
            set
            {
                if (_tomoPngSource != value)
                {
                    _tomoPngSource = value;
                    if (File.Exists(value))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(value);
                        //load the image now so we can immediately dispose of the stream
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.Rotation = Rotation.Rotate90;
                        bitmapImage.EndInit();
                        //clean up the stream to avoid file access exceptions when attempting to delete images
                        bitmapImage.Freeze();
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => TomoBitmap = bitmapImage);
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TomoPngSource)));
                        IsTomogramAvailable = true;
                    }
                }
            }
        }

        [WarpSerializable]
        public float AretomoTiltOffset
        {
            get => aretomoTiltOffset;
            set
            {
                if (value != aretomoTiltOffset)
                {
                    aretomoTiltOffset = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AretomoTiltOffset)));
                    WriteAretomoSettingsToXml();
                }
            }
        }

        public ObservableCollection<float> DarkImagesRemoved { get; set; }

        public ObservableCollection<int> AretomoIncludedImages { get; set; } = new ObservableCollection<int>();

        public ObservableCollection<int> FinalImages { get; set; } = new ObservableCollection<int>();

        public Dictionary<int, TiltImage> TiltImages { get; set; } = new Dictionary<int, TiltImage>();
        public int DataMode { get; set; }
        public ImageSize ImageSize { get; set; }
        public string ImageFile { get; set; }
        public float Voltage { get; set; }
        public float TiltAxisAngle { get; set; }
        public int Binning { get; set; }
        public int SpotSize { get; set; }
        public float DosePerImage { get; set; }

        private string _TopazProgress = jobStatus.Waiting.ToString();
        public string TopazProgress
        {
            get => _TopazProgress;
            set
            {
                if (value != _TopazProgress)
                {
                    if (DenoiseStatus == jobStatus.Started)
                    {
                        _TopazProgress = value;
                    }
                    else
                    {
                        _TopazProgress = DenoiseStatus.ToString();
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopazProgress)));
                }
            }
        }

        public string OutTomoLinux { get; set; }

        public string OutTomo { get; set; }

        public string OutTomoDenoised { get; private set; }
        #endregion

        public TiltSeriesViewModel(string mdoc_file, OptionsSshSettings settings, OptionsAretomoSettings aretomoSettings, decimal RuntimePixelSize, decimal PixelSizeMean)
        {
            PixelSpacing = RuntimePixelSize;
            PixelSpacingUnbinned = PixelSizeMean;
            IsNotProcessing = true;

            ProcessCommand = new DelegateCommand(async (param) => await TiltSeriesProcess(0));
            CopyTomoToClipboardDelegate = new DelegateCommand(CopyTomoToClipboard);
            OpenLogCommand = new DelegateCommand(OpenLog);
            OpenStMovieCommand = new DelegateCommand(OpenStMovie);
            OpenTomoMovieCommand = new DelegateCommand(OpenTomoMovie);
            ClearSeriesCommand = new DelegateCommand((param) => ClearSeries() );

            MdocFile = mdoc_file;
            GlobalAretomoSettings = aretomoSettings;
            LocalAretomoSettings = new OptionsAretomoSettings();
            Settings = settings;
            LocalAretomoSettings.PropertyChanged += OnLocalAretomoPropertyChanged;
            DarkImagesRemoved = new ObservableCollection<float>();
            DarkImagesRemoved.CollectionChanged += WriteAretomoSettingsToXml_CollectionChanged;
            FinalImages.CollectionChanged += WriteAretomoSettingsToXml_CollectionChanged;
            AretomoIncludedImages.CollectionChanged += WriteAretomoSettingsToXml_CollectionChanged;
            Task.Run(() => {
                ReadAretomoSettingsFromXml();
                ParseMdocFile();
                IsInitialized = true;
            });
        }

        public void OnLocalAretomoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            WriteAretomoSettingsToXml();
        }

        public void WriteAretomoSettingsToXml()
        {
            if (!SuccessfullyReadXmlProperties) return;
            var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, "");
            System.Xml.XmlTextWriter Writer = new System.Xml.XmlTextWriter(File.Create(files.AretomoSettingsXml), Encoding.Unicode);
            Writer.Formatting = System.Xml.Formatting.Indented;
            Writer.IndentChar = '\t';
            Writer.Indentation = 1;
            Writer.WriteStartDocument();
            Writer.WriteStartElement("Settings");

            WriteToXML(Writer);

            Writer.WriteStartElement("AretomoSettings");
            LocalAretomoSettings.WriteToXML(Writer);
            Writer.WriteEndElement();

            Tools.XMLHelper.WriteParamNode(Writer, "AretomoIncludedImages", String.Join(",", AretomoIncludedImages));
            Tools.XMLHelper.WriteParamNode(Writer, "DarkImagesRemoved", String.Join(",", DarkImagesRemoved));
            Tools.XMLHelper.WriteParamNode(Writer, "FinalImages", String.Join(",", FinalImages));

        Writer.WriteEndElement();
            Writer.WriteEndDocument();
            Writer.Flush();
            Writer.Close();
        }

        public void ReadAretomoSettingsFromXml()
        {
            try
            {
                var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, "");

                using (Stream SettingsStream = File.OpenRead(files.AretomoSettingsXml))
                {
                    var Doc = new System.Xml.XPath.XPathDocument(SettingsStream);
                    var Reader = Doc.CreateNavigator();
                    Reader.MoveToRoot();

                    Reader.MoveToRoot();
                    Reader.MoveToChild("Settings", "");

                    ReadFromXML(Reader);
                    LocalAretomoSettings.ReadFromXML(Reader.SelectSingleNode("AretomoSettings"));

                    var AretomoIncludedImagesStr = Tools.XMLHelper.LoadParamNode(Reader, "AretomoIncludedImages", AretomoIncludedImages == null ? "" : String.Join(",",AretomoIncludedImages));
                    if (AretomoIncludedImagesStr != "") AretomoIncludedImages = new ObservableCollection<int> (AretomoIncludedImagesStr.Split(',').ToList().Select(x => int.Parse(x)).ToList());

                    var DarkImagesRemovedStr = Tools.XMLHelper.LoadParamNode(Reader, "DarkImagesRemoved", DarkImagesRemoved == null ? "" : String.Join(",", DarkImagesRemoved));
                    if (DarkImagesRemovedStr != "") DarkImagesRemoved = new ObservableCollection<float> (DarkImagesRemovedStr.Split(',').ToList().Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToList());

                    var FinalImagesStr = Tools.XMLHelper.LoadParamNode(Reader, "FinalImages", FinalImages == null ? "" : String.Join(",", FinalImages));
                    if (FinalImagesStr != "") FinalImages = new ObservableCollection<int>(FinalImagesStr.Split(',').ToList().Select(x => int.Parse(x)));

                    LogToFile($"Read settings for {DisplayName}");
                }
            }
            catch (Exception ex)
            {
                LogToFile("Load aretomo settings:" + ex.Message);
            }
            finally
            {
                SuccessfullyReadXmlProperties = true;
            }
        }

        public void CheckFilesExist()
        {
            var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, Settings.LinuxPath);

            if (File.Exists(files.InStack))
            {
                NewstackStatus = jobStatus.Finished;
            }
            else
            {
                NewstackStatus = jobStatus.Waiting;
            }

            if (File.Exists(files.OutTomo))
            {
                AretomoStatus = jobStatus.Finished;
                OutTomoLinux = files.OutTomoLinux;
                OutTomo = files.OutTomo;
            }
            else
            {
                AretomoStatus = jobStatus.Waiting;
            }

            if (File.Exists(files.OutTomoDenoised))
            {
                DenoiseStatus = jobStatus.Finished;
                OutTomoDenoised = files.OutTomoDenoised;
            } else
            {
                DenoiseStatus = jobStatus.Waiting;
            }

            if (File.Exists(files.OutPng))
            {
                Aretomo2PngStatus = jobStatus.Finished;
                TomoPngSource = files.OutPng;
                IsTomogramAvailable = true;
            }
            else
            {
                Aretomo2PngStatus = jobStatus.Waiting;
                IsTomogramAvailable = false;
            }

            CheckTiltSeriesIsComplete();
        }

        public void ParseMdocFile()
        {
            while (IsFileLocked(new FileInfo(MdocFile))) Thread.Sleep(1000);

            var ti = new TiltImage();
            using (StreamReader sr = new StreamReader(MdocFile))
            {
                bool isHeader = true;
                while (!sr.EndOfStream)
                {
                    var Line = sr.ReadLine();
                    if (isHeader)
                    {
                        if (Line.StartsWith("["))
                        {
                            isHeader = false; continue;
                        }
                        var data = Line.Split('=').Select(x => x.Trim()).ToList();
                        if (data.Count != 2) continue;
                        switch (data[0])
                        {
                            case "DataMode": DataMode = int.Parse(data[1]); break;
                            case "ImageSize": ImageSize = new ImageSize(data[1]); break;
                            case "ImageFile": ImageFile = data[1]; break;
                            case "Voltage": Voltage = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                        }
                    }
                    else
                    {
                        var data = Line.Split('=').Select(x => x.Trim()).ToList();
                        if (Line.StartsWith("[ZValue"))
                        {
                            ti = new TiltImage();
                            ti.PixelSpacing = PixelSpacing;
                            var zvalue = int.Parse(data[1].Trim(']'));
                            TiltImages.Add(zvalue, ti);
                            ti.Zvalue = zvalue;
                        }

                        if (Line.StartsWith("[")) continue;
                        switch (data[0])
                        {
                            case "TiltAngle": ti.TiltAngle = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "StagePosition": ti.StagePosition = new FloatPair(data[1]); break;
                            case "StageZ": ti.StageZ = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "Magnification": ti.Magnification = int.Parse(data[1]); break;
                            case "Intensity": ti.Intensity = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "ExposureDose": ti.ExposureDose = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            //case "PixelSpacing": ti.PixelSpacing = decimal.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "SpotSize": ti.SpotSize = int.Parse(data[1]); break;
                            case "Defocus": ti.Defocus = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "ImageShift": ti.ImageShift = new FloatPair(data[1]); break;
                            case "RotationAngle": ti.RotationAngle = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "ExposureTime": ti.ExposureTime = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "Binning": ti.Binning = int.Parse(data[1]); break;
                            case "MagIndex": ti.MagIndex = int.Parse(data[1]); break;
                            case "CountsPerElectron": ti.CountsPerElection = data[1]; break;
                            case "MinMaxMean": ti.MinMaxMean = new MinMaxMean(data[1]); break;
                            case "TargetDefocus": ti.TargetDefocus = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "PriorRecordDose": ti.PriorRecordDose = float.Parse(data[1], CultureInfo.InvariantCulture); break;
                            case "SubFramePath": ti.SubFramePath = data[1]; break;
                            case "NumSubFrames": ti.NumSubFrames = int.Parse(data[1]); break;
                            case "FrameDosesAndNumber": ti.FrameDosesAndNumber = new FloatPair(data[1]); break;
                            case "FilterSlitAndLoss": ti.FilterSlitAndLoss = new FloatPair(data[1]); break;
                            case "ChannelName": ti.ChannelName = data[1]; break;
                            case "ChannelLength": ti.ChannelLength = data[1]; break;
                            case "DateTime": ti.DateTime = data[1]; break;
                        }
                    }

                }
            };
        }

        public bool CheckTiltSeriesIsComplete()
        {
            jobStatus status;
            status = jobStatus.Waiting;
            foreach (var ti in TiltImages)
            {
                if (ti.Value.Status != Controls.ProcessingStatus.Unprocessed && ti.Value.Status != Controls.ProcessingStatus.Outdated)
                {
                    status = jobStatus.Started;
                    continue;
                }
                WarpStatus = status;
                return false;
            }
            WarpStatus = jobStatus.Finished;
            return true;
        }

        #region Update tilt image status
        /* This code makes sure that the status of tilt images (processed, outdated, unprocessed) 
         is always up to date*/
        public bool NeedsCheckTiltSeriesIsComplete { get; set; }

        public Task UpdateTiltStatusTask { get; set; }

        public bool UpdateTiltStatus(Movie Movie, Controls.ProcessingStatus Status)
        {
            var match = TiltImages.Where(ti => Path.GetFileName(ti.Value.SubFramePath) == Movie.Name).ToList();
            if (match.Count == 0) return false;

            match.ForEach(ti => ti.Value.Status = Status);
            if (UpdateTiltStatusTask == null) { NeedsCheckTiltSeriesIsComplete = true; } 
            else if (UpdateTiltStatusTask.Status != TaskStatus.Running) { NeedsCheckTiltSeriesIsComplete = true; }
            
            if (NeedsCheckTiltSeriesIsComplete)
            {
                UpdateTiltStatusTask = Task.Run(() =>
                {
                    while (NeedsCheckTiltSeriesIsComplete)
                    {
                        NeedsCheckTiltSeriesIsComplete = false;
                        CheckTiltSeriesIsComplete();
                    }
                });
            }
            return true;
        }
        #endregion

        public async Task TiltSeriesProcess(int gpu)
        {
            CancellationTokenSource = new CancellationTokenSource();
            if (WarpStatus != jobStatus.Finished) return;
            if (!IsNotProcessing) return;
            IsNotProcessing = false;

            if (!OverrideGlobalSettings) { CopyGlobalSettings(); }
            var files = TiltSeriesProcessor.ProcessingFiles(this.MdocFile, Settings.LinuxPath);

            if (File.Exists(files.LogFile))
            {
                File.Delete(files.LogFile);
            }

            await Task.Run(() =>
           {
               TiltSeriesProcessor.ApplySamplePreTilt(this, LocalAretomoSettings);
           });

            await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(files.InStack))
                    {
                        bool IncludeAll = LocalAretomoSettings.IncludeAll;
                        NewstackStatus = jobStatus.Started;
                        NewstackStatus = TiltSeriesProcessor.RunNewStack(this, Settings, IncludeAll);
                    }
                    else
                    {
                        NewstackStatus = jobStatus.Finished;
                    }
                }
                catch (Exception e)
                {
                    NewstackStatus = jobStatus.Failed;
                }
            });

            await Task.Run(() =>
            {
                if (NewstackStatus == jobStatus.Finished)
                {
                    try
                    {
                        if (!File.Exists(files.OutTomo))
                        {
                            AretomoHasProcessed = 0;
                            AretomoStatus = jobStatus.Started;
                            AretomoStatus = TiltSeriesProcessor.RunAretomo(this, Settings, LocalAretomoSettings, gpu, CancellationTokenSource.Token);
                        }
                        else
                        {
                            AretomoStatus = jobStatus.Finished;
                        }
                    }
                    catch (Exception e)
                    {
                        AretomoStatus = jobStatus.Failed;
                    }
                }
            });

            await Task.Run(() =>
            {
                if (AretomoStatus == jobStatus.Finished)
                {
                    try
                    {
                        OutTomo = files.OutTomo;
                        TiltSeriesProcessor.ApplyAretomoTiltCorrection(this, LocalAretomoSettings);
                        DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(files.CorrectedMdoc));
                        TiltSeriesProcessor.WriteTomostar(this, files.AretomoOutXf, di.Parent.FullName, PixelSpacing);
                        if (!File.Exists(files.OutPng))
                        {
                            Aretomo2PngStatus = jobStatus.Started;
                            Aretomo2PngStatus = TiltSeriesProcessor.RunAretomo2Png(this, Settings, LocalAretomoSettings);
                        }
                        else
                        {
                            Aretomo2PngStatus = jobStatus.Finished;
                        }
                    }
                    catch (Exception e)
                    {
                        Aretomo2PngStatus = jobStatus.Failed;
                    }
                }
            });

            if (Aretomo2PngStatus == jobStatus.Finished) TomoPngSource = files.OutPng;
            LogToFile($"Finished processing {DisplayName}");
            IsNotProcessing = true;
        }

        #region Icommands
        public void CopyTomoToClipboard(object param)
        {
            var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, Settings.LinuxPath);
            if (File.Exists(files.OutTomoDenoised))
            {
                Clipboard.SetText(files.OutTomoDenoised);
            } else
            {
                Clipboard.SetText(files.OutTomo);
            }
        }

        public void CopyGlobalSettings()
        {
            LocalAretomoSettings.GlobalBinning = GlobalAretomoSettings.GlobalBinning;
            LocalAretomoSettings.GlobalDosePerTilt = GlobalAretomoSettings.GlobalDosePerTilt;
            LocalAretomoSettings.GlobalFlipVol = GlobalAretomoSettings.GlobalFlipVol;
            LocalAretomoSettings.GlobalAlignZ = GlobalAretomoSettings.GlobalAlignZ;
            LocalAretomoSettings.GlobalDarkTol = GlobalAretomoSettings.GlobalDarkTol;
            LocalAretomoSettings.GlobalTiltAxis = GlobalAretomoSettings.GlobalTiltAxis;
            LocalAretomoSettings.GlobalTiltCorAng = GlobalAretomoSettings.GlobalTiltCorAng;
            LocalAretomoSettings.GlobalTiltCorInt = GlobalAretomoSettings.GlobalTiltCorInt;
            LocalAretomoSettings.GlobalFlipVol = GlobalAretomoSettings.GlobalFlipVol;
            LocalAretomoSettings.GlobalVolZ = GlobalAretomoSettings.GlobalVolZ;
            LocalAretomoSettings.GlobalCs = GlobalAretomoSettings.GlobalCs;
            LocalAretomoSettings.GlobalOutImod = GlobalAretomoSettings.GlobalOutImod;
            LocalAretomoSettings.GlobalSkipReconstruction = GlobalAretomoSettings.GlobalSkipReconstruction;
            LocalAretomoSettings.GlobalUseWbp = GlobalAretomoSettings.GlobalUseWbp;
            LocalAretomoSettings.GlobalTiltCorInt = GlobalAretomoSettings.GlobalTiltCorInt;
            LocalAretomoSettings.GlobalTiltCorAng = GlobalAretomoSettings.GlobalTiltCorAng;
            LocalAretomoSettings.GlobalSamplePreTilt = GlobalAretomoSettings.GlobalSamplePreTilt;
            LocalAretomoSettings.NPatchesX = GlobalAretomoSettings.NPatchesX;
            LocalAretomoSettings.NPatchesY = GlobalAretomoSettings.NPatchesY;
            LocalAretomoSettings.IncludeAll = GlobalAretomoSettings.IncludeAll;
        }

        public void OpenLog(object param)
        {
            var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, Settings.LinuxPath);
            if (File.Exists(files.LogFile))
            {
                System.Diagnostics.Process.Start(files.LogFile);
            }
        }

        public void OpenStMovie(object param)
        {
            var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, Settings.LinuxPath);
            if (File.Exists(files.AretomoOutStMov))
            {
                System.Diagnostics.Process.Start(files.AretomoOutStMov);
            }
        }

        public void OpenTomoMovie(object param)
        {
            var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, Settings.LinuxPath);
            if (File.Exists(files.AretomoOutTomogramMov))
            {
                System.Diagnostics.Process.Start(files.AretomoOutTomogramMov);
            }
        }

        public List<string> ClearSeries()
        {
            var files = TiltSeriesProcessor.ProcessingFiles(MdocFile, "");
            if (!IsNotProcessing)
            {
                CancellationTokenSource.Cancel();
            }
            List<string> failedToDelete = new List<string>();
            LogToFile(files.AngFile);
            IsTomogramAvailable = false;

            try
            {
                LogToFile($"Deleting {files.LogFile}");
                File.Delete(Path.GetFullPath(files.LogFile));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.LogFile);
            }

            try
            {
                LogToFile($"Deleting {files.InStack}");
                File.Delete(Path.GetFullPath(files.InStack));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.InStack);
            }

            try
            {
                LogToFile($"Deleting {files.OutPng}");
                File.Delete(Path.GetFullPath(files.OutPng));

            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.OutPng);
            }

            try
            {
                LogToFile($"Deleting {files.OutTomo}");
                File.Delete(Path.GetFullPath(files.OutTomo));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.OutTomo);
            }

            try
            {
                LogToFile($"Deleting {files.TiltFile}");
                File.Delete(Path.GetFullPath(files.TiltFile));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.TiltFile);
            }

            try
            {
                LogToFile($"Deleting {files.Projection}");
                File.Delete(Path.GetFullPath(files.Projection));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.Projection);
            }

            try
            {
                LogToFile($"Deleting {files.InputFiles}");
                File.Delete(Path.GetFullPath(files.InputFiles));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.InputFiles);
            }

            try
            {
                LogToFile($"Deleting {files.AngFile}");
                File.Delete(Path.GetFullPath(files.AngFile));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.AngFile);
            }

            try
            {
                LogToFile($"Deleting {files.AretomoOutXf}");
                File.Delete(Path.GetFullPath(files.AretomoOutXf));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.AretomoOutXf);
            }

            try
            {
                LogToFile($"Deleting {files.AretomoOutTlt}");
                File.Delete(Path.GetFullPath(files.AretomoOutTlt));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.AretomoOutTlt);
            }

            try
            {
                LogToFile($"Deleting {files.AretomoOutSt}");
                File.Delete(Path.GetFullPath(files.AretomoOutSt));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.AretomoOutSt);
            }

            try
            {
                LogToFile($"Deleting {files.CorrectedMdoc}");
                File.Delete(Path.GetFullPath(files.CorrectedMdoc));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.CorrectedMdoc);
            }

            try
            {
                LogToFile($"Deleting {files.OutTomoDenoised}");
                File.Delete(Path.GetFullPath(files.OutTomoDenoised));
            }
            catch (Exception e)
            {
                LogToFile(e.Message);
                failedToDelete.Add(files.OutTomoDenoised);
            }

            IsQueued = false;
            CheckFilesExist();
            return failedToDelete;
        }

        private void WriteAretomoSettingsToXml_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WriteAretomoSettingsToXml();
        }
        #endregion

        #region utility methods
        private static void LogToFile(string s)
        {
            Application.Current.Dispatcher.InvokeAsync(async () => {
                var LogFile = Path.Combine(AppContext.BaseDirectory, "log.txt");
                try
                {
                    using (StreamWriter outputFile = new StreamWriter(LogFile, true))
                    {
                        await outputFile.WriteLineAsync(s);
                    }
                }
                catch { Console.WriteLine(s); };
            });
        }

        public bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Cannot read file {file.Name}. {ex.Message}");
                return true;
            }
            return false;
        }
        #endregion

    }

    public struct ImageSize
    {
        public int x;
        public int y;

        public ImageSize(string input)
        {
            var ints = input.Split(' ').Select(x => int.Parse(x)).ToList();
            x = ints[0];
            y = ints[1];
        }
    }
}
