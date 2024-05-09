using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.XPath;
using Warp.Tools;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Warp
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Options : WarpBase
    {
        public ObservableCollection<string> _InputDatTypes = new ObservableCollection<string>
        {
            "int8", "int16", "int32", "int64", "float32", "float64"
        };
        public ObservableCollection<string> InputDatTypes
        {
            get { return _InputDatTypes; }
        }

        #region Pixel size

        private decimal _PixelSizeX = 1.35M;
        [WarpSerializable]
        [JsonProperty]
        public decimal PixelSizeX
        {
            get { return _PixelSizeX; }
            set
            {
                if (value != _PixelSizeX)
                {
                    _PixelSizeX = value;
                    OnPropertyChanged();
                    RecalcBinnedPixelSize();
                }
            }
        }

        private decimal _PixelSizeY = 1.35M;
        [WarpSerializable]
        [JsonProperty]
        public decimal PixelSizeY
        {
            get { return _PixelSizeY; }
            set
            {
                if (value != _PixelSizeY)
                {
                    _PixelSizeY = value;
                    OnPropertyChanged();
                    RecalcBinnedPixelSize();
                }
            }
        }

        private decimal _PixelSizeAngle = 0M;
        [WarpSerializable]
        [JsonProperty]
        public decimal PixelSizeAngle
        {
            get { return _PixelSizeAngle; }
            set
            {
                if (value != _PixelSizeAngle)
                {
                    _PixelSizeAngle = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Things to process

        private bool _ProcessCTF = true;
        [WarpSerializable]
        [JsonProperty]
        public bool ProcessCTF
        {
            get { return _ProcessCTF; }
            set { if (value != _ProcessCTF) { _ProcessCTF = value; OnPropertyChanged(); } }
        }

        private bool _ProcessMovement = true;
        [WarpSerializable]
        [JsonProperty]
        public bool ProcessMovement
        {
            get { return _ProcessMovement; }
            set { if (value != _ProcessMovement) { _ProcessMovement = value; OnPropertyChanged(); } }
        }

        private bool _ProcessPicking = false;
        [WarpSerializable]
        [JsonProperty]
        public bool ProcessPicking
        {
            get { return _ProcessPicking; }
            set { if (value != _ProcessPicking) { _ProcessPicking = value; OnPropertyChanged(); } }
        }

        private bool _ProcessClassification = false;
        [WarpSerializable]
        [JsonProperty]
        public bool ProcessClassification
        {
            get { return _ProcessClassification; }
            set { if (value != _ProcessClassification) { _ProcessClassification = value; OnPropertyChanged(); } }
        }

        private bool _IsNotProcessing = true;
        public bool IsNotProcessing { 
            get { return _IsNotProcessing; }
            set {
                if (value != _IsNotProcessing) { _IsNotProcessing = value; OnPropertyChanged(); }
            } 
        }

        private bool _IsProcessing = false;
        public bool IsProcessing
        {
            get { return _IsProcessing; }
            set { if (value != _IsProcessing) { 
                    _IsProcessing = value; 
                    IsNotProcessing = !value;
                    OnPropertyChanged(); } }
        }

        private bool _ProcessStacker = false;
        [WarpSerializable]
        [JsonProperty]
        public bool ProcessStacker
        {
            get { return _ProcessStacker; }
            set { if (value != _ProcessStacker) { _ProcessStacker = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Sub-categories

        private OptionsTiltSeries _TiltSeries;
        public OptionsTiltSeries TiltSeries
        {
            get { return _TiltSeries; }
            set { if (value != _TiltSeries) { _TiltSeries = value; OnPropertyChanged(); } }
        }

        private OptionsImport _Import = new OptionsImport();
        public OptionsImport Import
        {
            get { return _Import; }
            set { if (value != _Import) { _Import = value; OnPropertyChanged(); } }
        }

        private OptionsCTF _CTF = new OptionsCTF();
        public OptionsCTF CTF
        {
            get { return _CTF; }
            set { if (value != _CTF) { _CTF = value; OnPropertyChanged(); } }
        }

        private OptionsMovement _Movement = new OptionsMovement();
        public OptionsMovement Movement
        {
            get { return _Movement; }
            set { if (value != _Movement) { _Movement = value; OnPropertyChanged(); } }
        }

        private OptionsGrids _Grids = new OptionsGrids();
        public OptionsGrids Grids
        {
            get { return _Grids; }
            set { if (value != _Grids) { _Grids = value; OnPropertyChanged(); } }
        }

        private OptionsPicking _Picking = new OptionsPicking();
        public OptionsPicking Picking
        {
            get { return _Picking; }
            set { if (value != _Picking) { _Picking = value; OnPropertyChanged(); } }
        }

        private OptionsClassification _Classification = new OptionsClassification();
        public OptionsClassification Classification
        {
            get { return _Classification; }
            set { if (value != _Classification) { _Classification = value; OnPropertyChanged(); } }
        }

        private OptionsStacker _Stacker = new OptionsStacker();
        public OptionsStacker Stacker
        {
            get { return _Stacker; }
            set { if (value != _Stacker) { _Stacker = value; OnPropertyChanged(); } }
        }

        private OptionsTomo _Tomo = new OptionsTomo();
        public OptionsTomo Tomo
        {
            get { return _Tomo; }
            set { if (value != _Tomo) { _Tomo = value; OnPropertyChanged(); } }
        }

        private OptionsExport _Export = new OptionsExport();
        public OptionsExport Export
        {
            get { return _Export; }
            set { if (value != _Export) { _Export = value; OnPropertyChanged(); } }
        }

        private OptionsTasks _Tasks = new OptionsTasks();
        public OptionsTasks Tasks
        {
            get { return _Tasks; }
            set { if (value != _Tasks) { _Tasks = value; OnPropertyChanged(); } }
        }

        private OptionsFilter _Filter = new OptionsFilter();
        public OptionsFilter Filter
        {
            get { return _Filter; }
            set { if (value != _Filter) { _Filter = value; OnPropertyChanged(); } }
        }

        private OptionsAdvanced _Advanced = new OptionsAdvanced();
        public OptionsAdvanced Advanced
        {
            get { return _Advanced; }
            set { if (value != _Advanced) { _Advanced = value; OnPropertyChanged(); } }
        }

        private OptionsRuntime _Runtime = new OptionsRuntime();
        public OptionsRuntime Runtime
        {
            get { return _Runtime; }
            set { if (value != _Runtime) { _Runtime = value; OnPropertyChanged(); } }
        }

        private OptionsSshSettings _SshSettings = new OptionsSshSettings();
        public OptionsSshSettings SshSettings
        {
            get { return _SshSettings; }
            set { if (value != _SshSettings) { _SshSettings = value; OnPropertyChanged(); } }
        }

        private OptionsAretomoSettings _AretomoSettings = new OptionsAretomoSettings();
        public OptionsAretomoSettings AretomoSettings
        {
            get { return _AretomoSettings; }
            set { if (value != _AretomoSettings) { _AretomoSettings = value; OnPropertyChanged(); } }
        }


        #endregion

        #region Runtime

        private void RecalcBinnedPixelSize()
        {
            Runtime.BinnedPixelSizeMean = PixelSizeMean * (decimal)Math.Pow(2.0, (double)Import.BinTimes);

            CTF.OnPropertyChanged("RangeMin");
            CTF.OnPropertyChanged("RangeMax");
            Movement.OnPropertyChanged("RangeMin");
            Movement.OnPropertyChanged("RangeMax");

            Console.WriteLine($"Binned pixel size {Runtime.BinnedPixelSizeMean}");
            TiltSeries.RuntimePixelSize = Runtime.BinnedPixelSizeMean;
            TiltSeries.PixelSpacingUnbinned = PixelSizeMean;
            foreach (var ts in TiltSeries.TiltSeriesList)
            {
                ts.PixelSpacing = Runtime.BinnedPixelSizeMean;
                ts.PixelSpacingUnbinned = PixelSizeMean;
            }
        }

        public decimal PixelSizeMean => (PixelSizeX + PixelSizeY) * 0.5M;
        public decimal BinnedPixelSizeX => PixelSizeX * (decimal)Math.Pow(2.0, (double)Import.BinTimes);
        public decimal BinnedPixelSizeY => PixelSizeY * (decimal)Math.Pow(2.0, (double)Import.BinTimes);
        public decimal BinnedPixelSizeMean => (BinnedPixelSizeX + BinnedPixelSizeY) * 0.5M;

        public float2 AstigmatismMean = new float2();
        public float AstigmatismStd = 0.1f;

        public MainWindow MainWindow;

        public void UpdateGPUStats()
        {
            int NDevices = GPU.GetDeviceCount();
            string[] Stats = new string[NDevices];
            for (int i = 0; i < NDevices; i++)
                Stats[i] = "GPU" + i + ": " + GPU.GetFreeMemory(i) + " MB";
            Runtime.GPUStats = string.Join(", ", Stats);
        }

        #endregion

        public Options()
        {
            Import.PropertyChanged += SubOptions_PropertyChanged;
            CTF.PropertyChanged += SubOptions_PropertyChanged;
            Movement.PropertyChanged += SubOptions_PropertyChanged;
            Grids.PropertyChanged += SubOptions_PropertyChanged;
            Picking.PropertyChanged += SubOptions_PropertyChanged;
            Classification.PropertyChanged += SubOptions_PropertyChanged;
            Stacker.PropertyChanged += SubOptions_PropertyChanged;
            Tomo.PropertyChanged += SubOptions_PropertyChanged;
            Export.PropertyChanged += SubOptions_PropertyChanged;
            Tasks.PropertyChanged += SubOptions_PropertyChanged;
            Filter.PropertyChanged += SubOptions_PropertyChanged;
            Advanced.PropertyChanged += SubOptions_PropertyChanged;
            SshSettings.PropertyChanged += SubOptions_PropertyChanged;
            AretomoSettings.PropertyChanged += SubOptions_PropertyChanged;
        }

        private void SubOptions_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == Import)
            {
                OnPropertyChanged("Import." + e.PropertyName);
                if (e.PropertyName == "BinTimes")
                    RecalcBinnedPixelSize();
            }
            else if (sender == CTF)
                OnPropertyChanged("CTF." + e.PropertyName);
            else if (sender == Movement)
                OnPropertyChanged("Movement." + e.PropertyName);
            else if (sender == Grids)
                OnPropertyChanged("Grids." + e.PropertyName);
            else if (sender == Tomo)
                OnPropertyChanged("Tomo." + e.PropertyName);
            else if (sender == Picking)
                OnPropertyChanged("Picking." + e.PropertyName);
            else if (sender == Export)
                OnPropertyChanged("Export." + e.PropertyName);
            else if (sender == Tasks)
                OnPropertyChanged("Tasks." + e.PropertyName);
            else if (sender == Filter)
                OnPropertyChanged("Filter." + e.PropertyName);
            else if (sender == Advanced)
                OnPropertyChanged("Advanced." + e.PropertyName);
            else if (sender == Stacker)
                OnPropertyChanged("Stacker." + e.PropertyName);
            else if (sender == Classification)
                OnPropertyChanged("Classification." + e.PropertyName);
            else if (sender == SshSettings)
                OnPropertyChanged("SshSettings." + e.PropertyName);
            else if (sender == AretomoSettings)
                OnPropertyChanged("AretomoSettings." + e.PropertyName);
        }

        public void Save(string path)
        {
            XmlTextWriter Writer = new XmlTextWriter(File.Create(path), Encoding.Unicode);
            Writer.Formatting = System.Xml.Formatting.Indented;
            Writer.IndentChar = '\t';
            Writer.Indentation = 1;
            Writer.WriteStartDocument();
            Writer.WriteStartElement("Settings");

            WriteToXML(Writer);

            Writer.WriteStartElement("Stacker");
            Stacker.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Classification");
            Classification.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("SshSettings");
            SshSettings.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("AretomoSettings");
            AretomoSettings.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Import");
            Import.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("CTF");
            CTF.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Movement");
            Movement.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Grids");
            Grids.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Tomo");
            Tomo.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Picking");
            Picking.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Export");
            Export.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Tasks");
            Tasks.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Filter");
            Filter.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteStartElement("Advanced");
            Advanced.WriteToXML(Writer);
            Writer.WriteEndElement();

            Writer.WriteEndElement();
            Writer.WriteEndDocument();
            Writer.Flush();
            Writer.Close();
        }

        public void Load(string path)
        {
            try
            {
                using (Stream SettingsStream = File.OpenRead(path))
                {
                    XPathDocument Doc = new XPathDocument(SettingsStream);
                    XPathNavigator Reader = Doc.CreateNavigator();
                    Reader.MoveToRoot();

                    Reader.MoveToRoot();
                    Reader.MoveToChild("Settings", "");

                    ReadFromXML(Reader);

                    Stacker.ReadFromXML(Reader.SelectSingleNode("Stacker"));
                    Classification.ReadFromXML(Reader.SelectSingleNode("Classification"));
                    SshSettings.ReadFromXML(Reader.SelectSingleNode("SshSettings"));
                    AretomoSettings.ReadFromXML(Reader.SelectSingleNode("AretomoSettings"));
                    Import.ReadFromXML(Reader.SelectSingleNode("Import"));
                    CTF.ReadFromXML(Reader.SelectSingleNode("CTF"));
                    Movement.ReadFromXML(Reader.SelectSingleNode("Movement"));
                    Grids.ReadFromXML(Reader.SelectSingleNode("Grids"));
                    Tomo.ReadFromXML(Reader.SelectSingleNode("Tomo"));
                    Picking.ReadFromXML(Reader.SelectSingleNode("Picking"));
                    Export.ReadFromXML(Reader.SelectSingleNode("Export"));
                    Tasks.ReadFromXML(Reader.SelectSingleNode("Tasks"));
                    Filter.ReadFromXML(Reader.SelectSingleNode("Filter"));
                    Advanced.ReadFromXML(Reader.SelectSingleNode("Advanced"));

                    RecalcBinnedPixelSize();
                }
            }
            catch { }
        }

        #region 2D processing settings creation and adoption

        public ProcessingOptionsMovieCTF GetProcessingMovieCTF()
        {
            return new ProcessingOptionsMovieCTF
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,
                BinTimes = Import.BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,
                Window = CTF.Window,
                RangeMin = CTF.RangeMin,
                RangeMax = CTF.RangeMax,
                Voltage = CTF.Voltage,
                Cs = CTF.Cs,
                Cc = CTF.Cc,
                IllumAngle = CTF.IllAperture,
                EnergySpread = CTF.DeltaE,
                Thickness = CTF.Thickness,
                Amplitude = CTF.Amplitude,
                DoPhase = CTF.DoPhase,
                DoIce = false, //CTF.DoIce,
                UseMovieSum = CTF.UseMovieSum,
                ZMin = CTF.ZMin,
                ZMax = CTF.ZMax,
                GridDims = new int3(Grids.CTFX, Grids.CTFY, Grids.CTFZ)
            };
        }

        public void Adopt(ProcessingOptionsMovieCTF options)
        {
            PixelSizeX = options.PixelSizeX;
            PixelSizeY = options.PixelSizeY;
            PixelSizeAngle = options.PixelSizeAngle;

            Import.BinTimes = options.BinTimes;
            Import.GainPath = options.GainPath;
            Import.CorrectGain = !string.IsNullOrEmpty(options.GainPath);
            Import.GainFlipX = options.GainFlipX;
            Import.GainFlipY = options.GainFlipY;
            Import.GainTranspose = options.GainTranspose;
            Import.DefectsPath = options.DefectsPath;
            Import.CorrectDefects = !string.IsNullOrEmpty(options.DefectsPath);

            CTF.Window = options.Window;
            CTF.RangeMin = options.RangeMin;
            CTF.RangeMax = options.RangeMax;
            CTF.Voltage = options.Voltage;
            CTF.Cs = options.Cs;
            CTF.Cc = options.Cc;
            CTF.IllAperture = options.IllumAngle;
            CTF.DeltaE = options.EnergySpread;
            CTF.Thickness = options.Thickness;
            CTF.Amplitude = options.Amplitude;
            CTF.DoPhase = options.DoPhase;
            CTF.UseMovieSum = options.UseMovieSum;
            CTF.ZMin = options.ZMin;
            CTF.ZMax = options.ZMax;

            Grids.CTFX = options.GridDims.X;
            Grids.CTFY = options.GridDims.Y;
            Grids.CTFZ = options.GridDims.Z;
        }

        public ProcessingOptionsMovieMovement GetProcessingMovieMovement()
        {
            return new ProcessingOptionsMovieMovement
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,
                BinTimes = Import.BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,
                RangeMin = Movement.RangeMin,
                RangeMax = Movement.RangeMax,
                Bfactor = Movement.Bfactor,
                GridDims = new int3(Grids.MovementX, Grids.MovementY, Grids.MovementZ)
            };
        }

        public void Adopt(ProcessingOptionsMovieMovement options)
        {
            PixelSizeX = options.PixelSizeX;
            PixelSizeY = options.PixelSizeY;
            PixelSizeAngle = options.PixelSizeAngle;
            Import.BinTimes = options.BinTimes;
            Import.GainPath = options.GainPath;
            Import.CorrectGain = !string.IsNullOrEmpty(options.GainPath);
            Import.GainFlipX = options.GainFlipX;
            Import.GainFlipY = options.GainFlipY;
            Import.GainTranspose = options.GainTranspose;
            Import.DefectsPath = options.DefectsPath;
            Import.CorrectDefects = !string.IsNullOrEmpty(options.DefectsPath);
            Movement.RangeMin = options.RangeMin;
            Movement.RangeMax = options.RangeMax;
            Movement.Bfactor = options.Bfactor;
            Grids.MovementX = options.GridDims.X;
            Grids.MovementY = options.GridDims.Y;
            Grids.MovementZ = options.GridDims.Z;
        }

        public ProcessingOptionsMovieExport GetProcessingMovieExport()
        {
            return new ProcessingOptionsMovieExport
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,

                BinTimes = Import.BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,
                DosePerAngstromFrame = Import.DosePerAngstromFrame,

                DoAverage = Export.DoAverage,
                DoStack = Export.DoStack,
                DoDeconv = Export.DoDeconvolve,
                DeconvolutionStrength = Export.DeconvolutionStrength,
                DeconvolutionFalloff = Export.DeconvolutionFalloff,
                StackGroupSize = Export.StackGroupSize,
                SkipFirstN = Export.SkipFirstN,
                SkipLastN = Export.SkipLastN,

                Voltage = CTF.Voltage
            };
        }

        public void Adopt(ProcessingOptionsMovieExport options)
        {
            PixelSizeX = options.PixelSizeX;
            PixelSizeY = options.PixelSizeY;
            PixelSizeAngle = options.PixelSizeAngle;

            Import.BinTimes = options.BinTimes;
            Import.GainPath = options.GainPath;
            Import.CorrectGain = !string.IsNullOrEmpty(options.GainPath);
            Import.GainFlipX = options.GainFlipX;
            Import.GainFlipY = options.GainFlipY;
            Import.GainTranspose = options.GainTranspose;
            Import.DefectsPath = options.DefectsPath;
            Import.CorrectDefects = !string.IsNullOrEmpty(options.DefectsPath);

            Import.DosePerAngstromFrame = options.DosePerAngstromFrame;

            CTF.Voltage = options.Voltage;

            Export.DoAverage = options.DoAverage;
            Export.DoStack = options.DoStack;
            Export.DoDeconvolve = options.DoDeconv;
            Export.DeconvolutionStrength = options.DeconvolutionStrength;
            Export.DeconvolutionFalloff = options.DeconvolutionFalloff;
            Export.StackGroupSize = options.StackGroupSize;
            Export.SkipFirstN = options.SkipFirstN;
            Export.SkipLastN = options.SkipLastN;
        }

        public ProcessingOptionsParticlesExport GetProcessingParticleExport()
        {
            decimal BinTimes = (decimal)Math.Log((double)(Tasks.Export2DPixel / PixelSizeMean), 2.0);

            return new ProcessingOptionsParticlesExport
            {
                Suffix = Tasks.OutputSuffix,

                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,

                BinTimes = BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,
                DosePerAngstromFrame = Import.DosePerAngstromFrame,

                DoAverage = Tasks.Export2DDoAverages,
                DoStack = Tasks.Export2DDoMovies,
                DoDenoisingPairs = Tasks.Export2DDoDenoisingPairs,
                StackGroupSize = Export.StackGroupSize,
                SkipFirstN = Export.SkipFirstN,
                SkipLastN = Export.SkipLastN,

                Voltage = CTF.Voltage
            };
        }

        public ProcessingOptionsFullMatch GetProcessingFullMatch()
        {
            decimal BinTimes = (decimal)Math.Log((double)(Tasks.TomoFullReconstructPixel / PixelSizeMean), 2.0);

            return new ProcessingOptionsFullMatch
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,

                BinTimes = BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,
                DosePerAngstromFrame = Import.DosePerAngstromFrame,
                Voltage = CTF.Voltage,

                TemplatePixel = Tasks.TomoMatchTemplatePixel,
                TemplateDiameter = Tasks.TomoMatchTemplateDiameter,
                TemplateFraction = Tasks.TomoMatchTemplateFraction,

                SubPatchSize = 384,
                Symmetry = Tasks.TomoMatchSymmetry,
                HealpixOrder = (int)Tasks.TomoMatchHealpixOrder,

                Supersample = 5,

                NResults = (int)Tasks.TomoMatchNResults,

                Invert = Tasks.InputInvert,
                WhitenSpectrum = Tasks.TomoMatchWhitenSpectrum
            };
        }

        public ProcessingOptionsBoxNet GetProcessingBoxNet()
        {
            return new ProcessingOptionsBoxNet
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,

                BinTimes = Import.BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,
                

                OverwriteFiles = true,

                ModelName = Picking.ModelPath,

                PickingInvert = Picking.DataStyle != "cryo",
                ExpectedDiameter = Picking.Diameter,
                MinimumScore = Picking.MinimumScore,
                MinimumMaskDistance = Picking.MinimumMaskDistance,

                ExportParticles = Picking.DoExport,
                ExportBoxSize = Picking.BoxSize,
                ExportInvert = Picking.Invert,
                ExportNormalize = Picking.Normalize,
                FourierCropBox = Picking.FourierCropBox,
                DoFourierCrop = Picking.DoFourierCrop,
            };
        }

        #endregion

        #region Tomo processing settings creation

        public ProcessingOptionsTomoFullReconstruction GetProcessingTomoFullReconstruction()
        {
            decimal BinTimes = (decimal)Math.Log((double)(Tasks.TomoFullReconstructPixel / PixelSizeMean), 2.0);

            return new ProcessingOptionsTomoFullReconstruction
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,

                BinTimes = BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,

                Dimensions = new float3((float)Tomo.DimensionsX,
                                        (float)Tomo.DimensionsY,
                                        (float)Tomo.DimensionsZ),

                DoDeconv = Tasks.TomoFullReconstructDoDeconv,
                DeconvStrength = Tasks.TomoFullReconstructDeconvStrength,
                DeconvFalloff = Tasks.TomoFullReconstructDeconvFalloff,
                DeconvHighpass = Tasks.TomoFullReconstructDeconvHighpass,

                Invert = Tasks.InputInvert,
                Normalize = Tasks.InputNormalize,
                SubVolumeSize = 64,
                SubVolumePadding = 2,

                PrepareDenoising = Tasks.TomoFullReconstructPrepareDenoising,

                KeepOnlyFullVoxels = Tasks.TomoFullReconstructOnlyFullVoxels
            };
        }

        public ProcessingOptionsTomoFullMatch GetProcessingTomoFullMatch()
        {
            decimal BinTimes = (decimal)Math.Log((double)(Tasks.TomoFullReconstructPixel / PixelSizeMean), 2.0);

            return new ProcessingOptionsTomoFullMatch
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,

                BinTimes = BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,

                Dimensions = new float3((float)Tomo.DimensionsX,
                                        (float)Tomo.DimensionsY,
                                        (float)Tomo.DimensionsZ),

                TemplatePixel = Tasks.TomoMatchTemplatePixel,
                TemplateDiameter = Tasks.TomoMatchTemplateDiameter,
                TemplateFraction = Tasks.TomoMatchTemplateFraction,

                SubVolumeSize = 192,
                Symmetry = Tasks.TomoMatchSymmetry,
                HealpixOrder = (int)Tasks.TomoMatchHealpixOrder,

                Supersample = 1,

                KeepOnlyFullVoxels = true,
                NResults = (int)Tasks.TomoMatchNResults,

                ReuseCorrVolumes = Tasks.ReuseCorrVolumes,

                WhitenSpectrum = Tasks.TomoMatchWhitenSpectrum
            };
        }

        public ProcessingOptionsTomoSubReconstruction GetProcessingTomoSubReconstruction()
        {
            decimal BinTimes = (decimal)Math.Log((double)(Tasks.TomoSubReconstructPixel / PixelSizeMean), 2.0);

            return new ProcessingOptionsTomoSubReconstruction
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                PixelSizeAngle = PixelSizeAngle,

                BinTimes = BinTimes,
                EERGroupFrames = Import.ExtensionEER ? Import.EERGroupFrames : 0,
                GainPath = Import.CorrectGain ? Import.GainPath : "",
                GainHash = Import.CorrectGain ? Runtime.GainReferenceHash : "",
                DefectsPath = Import.CorrectDefects ? Import.DefectsPath : "",
                DefectsHash = Import.CorrectDefects ? Runtime.DefectMapHash : "",
                GainFlipX = Import.GainFlipX,
                GainFlipY = Import.GainFlipY,
                GainTranspose = Import.GainTranspose,

                Dimensions = new float3((float)Tomo.DimensionsX,
                                        (float)Tomo.DimensionsY,
                                        (float)Tomo.DimensionsZ),

                Suffix = "",

                BoxSize = (int)Tasks.TomoSubReconstructBox,
                ParticleDiameter = (int)Tasks.TomoSubReconstructDiameter,

                Invert = Tasks.InputInvert,
                NormalizeInput = Tasks.InputNormalize,
                NormalizeOutput = Tasks.OutputNormalize,

                PrerotateParticles = Tasks.TomoSubReconstructPrerotated,
                DoLimitDose = Tasks.TomoSubReconstructDoLimitDose,
                NTilts = Tasks.TomoSubReconstructNTilts,

                MakeSparse = Tasks.TomoSubReconstructMakeSparse
            };
        }

        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsImport : WarpBase
    {
        private string _Folder = "";
        [WarpSerializable]
        [JsonProperty]
        public string Folder
        {
            get { return _Folder; }
            set
            {
                if (value != _Folder)
                {
                    _Folder = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _UserFolder = "";
        [WarpSerializable]
        [JsonProperty]
        public string UserFolder
        {
            get { return _UserFolder; }
            set
            {
                if (value != _UserFolder)
                {
                    _UserFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _InvertAngles = false;
        [WarpSerializable]
        [JsonProperty]
        public bool InvertAngles
        {
            get { return _InvertAngles; }
            set
            {
                if (value != _InvertAngles)
                {
                    _InvertAngles = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _MicroscopePCFolder = "";
        [WarpSerializable]
        [JsonProperty]
        public string MicroscopePCFolder
        {
            get { return _MicroscopePCFolder; }
            set
            {
                if (value != _MicroscopePCFolder)
                {
                    _MicroscopePCFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _Extension = "*.mrc";
        [WarpSerializable]
        [JsonProperty]
        public string Extension
        {
            get { return _Extension; }
            set
            {
                if (value != _Extension)
                {
                    _Extension = value;
                    OnPropertyChanged();

                    OnPropertyChanged("ExtensionMRC");
                    OnPropertyChanged("ExtensionMRCS");
                    OnPropertyChanged("ExtensionEM");
                    OnPropertyChanged("ExtensionTIFF");
                    OnPropertyChanged("ExtensionTIFFF");
                    OnPropertyChanged("ExtensionEER");
                    OnPropertyChanged("ExtensionDAT");
                    OnPropertyChanged("ExtensionTomoSTAR");
                }
            }
        }
        
        public bool ExtensionMRC
        {
            get { return Extension == "*.mrc"; }
            set
            {
                if (value != (Extension == "*.mrc"))
                {
                    if (value)
                        Extension = "*.mrc";
                    OnPropertyChanged();
                }
            }
        }
        
        public bool ExtensionMRCS
        {
            get { return Extension == "*.mrcs"; }
            set
            {
                if (value != (Extension == "*.mrcs"))
                {
                    if (value)
                        Extension = "*.mrcs";
                    OnPropertyChanged();
                }
            }
        }
        
        public bool ExtensionEM
        {
            get { return Extension == "*.em"; }
            set
            {
                if (value != (Extension == "*.em"))
                {
                    if (value)
                        Extension = "*.em";
                    OnPropertyChanged();
                }
            }
        }
        
        public bool ExtensionTIFF
        {
            get { return Extension == "*.tif"; }
            set
            {
                if (value != (Extension == "*.tif"))
                {
                    if (value)
                        Extension = "*.tif";
                    OnPropertyChanged();
                }
            }
        }

        public bool ExtensionTIFFF
        {
            get { return Extension == "*.tiff"; }
            set
            {
                if (value != (Extension == "*.tiff"))
                {
                    if (value)
                        Extension = "*.tiff";
                    OnPropertyChanged();
                }
            }
        }

        public bool ExtensionEER
        {
            get { return Extension == "*.eer"; }
            set
            {
                if (value != (Extension == "*.eer"))
                {
                    if (value)
                        Extension = "*.eer";
                    OnPropertyChanged();
                }
            }
        }

        public bool ExtensionTomoSTAR
        {
            get { return Extension == "*.tomostar"; }
            set
            {
                if (value != (Extension == "*.tomostar"))
                {
                    if (value)
                        Extension = "*.tomostar";
                    OnPropertyChanged();
                }
            }
        }
        
        public bool ExtensionDAT
        {
            get { return Extension == "*.dat"; }
            set
            {
                if (value != (Extension == "*.dat"))
                {
                    if (value)
                        Extension = "*.dat";
                    OnPropertyChanged();
                }
            }
        }

        private int _HeaderlessWidth = 7676;
        [WarpSerializable]
        [JsonProperty]
        public int HeaderlessWidth
        {
            get { return _HeaderlessWidth; }
            set { if (value != _HeaderlessWidth) { _HeaderlessWidth = value; OnPropertyChanged(); } }
        }

        private int _HeaderlessHeight = 7420;
        [WarpSerializable]
        [JsonProperty]
        public int HeaderlessHeight
        {
            get { return _HeaderlessHeight; }
            set { if (value != _HeaderlessHeight) { _HeaderlessHeight = value; OnPropertyChanged(); } }
        }

        private string _HeaderlessType = "int8";
        [WarpSerializable]
        [JsonProperty]
        public string HeaderlessType
        {
            get { return _HeaderlessType; }
            set { if (value != _HeaderlessType) { _HeaderlessType = value; OnPropertyChanged(); } }
        }

        private long _HeaderlessOffset = 0;
        [WarpSerializable]
        [JsonProperty]
        public long HeaderlessOffset
        {
            get { return _HeaderlessOffset; }
            set { if (value != _HeaderlessOffset) { _HeaderlessOffset = value; OnPropertyChanged(); } }
        }

        private decimal _BinTimes = 0;
        [WarpSerializable]
        [JsonProperty]
        public decimal BinTimes
        {
            get { return _BinTimes; }
            set
            {
                if (value != _BinTimes)
                {
                    _BinTimes = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _GainPath = "";
        [WarpSerializable]
        [JsonProperty]
        public string GainPath
        {
            get { return _GainPath; }
            set
            {
                if (value != _GainPath)
                {
                    _GainPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _DefectsPath = "";
        [WarpSerializable]
        [JsonProperty]
        public string DefectsPath
        {
            get { return _DefectsPath; }
            set { if (value != _DefectsPath) { _DefectsPath = value; OnPropertyChanged(); } }
        }

        private bool _GainFlipX = false;
        [WarpSerializable]
        [JsonProperty]
        public bool GainFlipX
        {
            get { return _GainFlipX; }
            set { if (value != _GainFlipX) { _GainFlipX = value; OnPropertyChanged(); } }
        }

        private bool _GainFlipY = false;
        [WarpSerializable]
        [JsonProperty]
        public bool GainFlipY
        {
            get { return _GainFlipY; }
            set { if (value != _GainFlipY) { _GainFlipY = value; OnPropertyChanged(); } }
        }

        private bool _GainTranspose = false;
        [WarpSerializable]
        [JsonProperty]
        public bool GainTranspose
        {
            get { return _GainTranspose; }
            set { if (value != _GainTranspose) { _GainTranspose = value; OnPropertyChanged(); } }
        }

        private bool _CorrectGain = false;
        [WarpSerializable]
        [JsonProperty]
        public bool CorrectGain
        {
            get { return _CorrectGain; }
            set
            {
                if (value != _CorrectGain)
                {
                    _CorrectGain = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _CorrectDefects = false;
        [WarpSerializable]
        [JsonProperty]
        public bool CorrectDefects
        {
            get { return _CorrectDefects; }
            set { if (value != _CorrectDefects) { _CorrectDefects = value; OnPropertyChanged(); } }
        }

        private decimal _DosePerAngstromFrame = 0;
        [WarpSerializable]
        [JsonProperty]
        public decimal DosePerAngstromFrame
        {
            get { return _DosePerAngstromFrame; }
            set { if (value != _DosePerAngstromFrame) { _DosePerAngstromFrame = value; OnPropertyChanged(); } }
        }

        private int _EERGroupFrames = 10;
        [WarpSerializable]
        [JsonProperty]
        public int EERGroupFrames
        {
            get { return _EERGroupFrames; }
            set { if (value != _EERGroupFrames) { _EERGroupFrames = value; OnPropertyChanged(); } }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsCTF : WarpBase
    {
        private int _Window = 512;
        [WarpSerializable]
        [JsonProperty]
        public int Window
        {
            get { return _Window; }
            set
            {
                if (value != _Window)
                {
                    _Window = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _RangeMin = 0.10M;
        [WarpSerializable]
        [JsonProperty]
        public decimal RangeMin
        {
            get { return _RangeMin; }
            set
            {
                if (value != _RangeMin)
                {
                    _RangeMin = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _RangeMax = 0.6M;
        [WarpSerializable]
        [JsonProperty]
        public decimal RangeMax
        {
            get { return _RangeMax; }
            set
            {
                if (value != _RangeMax)
                {
                    _RangeMax = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _MinQuality = 0.8M;
        [WarpSerializable]
        [JsonProperty]
        public decimal MinQuality
        {
            get { return _MinQuality; }
            set
            {
                if (value != _MinQuality)
                {
                    _MinQuality = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _Voltage = 300;
        [WarpSerializable]
        [JsonProperty]
        public int Voltage
        {
            get { return _Voltage; }
            set
            {
                if (value != _Voltage)
                {
                    _Voltage = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _Cs = 2.7M;
        [WarpSerializable]
        [JsonProperty]
        public decimal Cs
        {
            get { return _Cs; }
            set
            {
                if (value != _Cs)
                {
                    _Cs = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _Cc = 2.7M;
        [WarpSerializable]
        public decimal Cc
        {
            get { return _Cc; }
            set { if (value != _Cc) { _Cc = value; OnPropertyChanged(); } }
        }

        private decimal _Amplitude = 0.07M;
        [WarpSerializable]
        [JsonProperty]
        public decimal Amplitude
        {
            get { return _Amplitude; }
            set
            {
                if (value != _Amplitude)
                {
                    _Amplitude = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _IllAperture = 30;
        [WarpSerializable]
        public decimal IllAperture
        {
            get { return _IllAperture; }
            set { if (value != _IllAperture) { _IllAperture = value; OnPropertyChanged(); } }
        }

        private decimal _DeltaE = 0.7M;
        [WarpSerializable]
        public decimal DeltaE
        {
            get { return _DeltaE; }
            set { if (value != _DeltaE) { _DeltaE = value; OnPropertyChanged(); } }
        }

        private decimal _Thickness = 0;
        [WarpSerializable]
        public decimal Thickness
        {
            get { return _Thickness; }
            set { if (value != _Thickness) { _Thickness = value; OnPropertyChanged(); } }
        }

        private bool _DoPhase = true;
        [WarpSerializable]
        [JsonProperty]
        public bool DoPhase
        {
            get { return _DoPhase; }
            set { if (value != _DoPhase) { _DoPhase = value; OnPropertyChanged(); } }
        }

        //private bool _DoIce = false;
        //[WarpSerializable]
        //public bool DoIce
        //{
        //    get { return _DoIce; }
        //    set { if (value != _DoIce) { _DoIce = value; OnPropertyChanged(); } }
        //}

        private bool _UseMovieSum = false;
        [WarpSerializable]
        [JsonProperty]
        public bool UseMovieSum
        {
            get { return _UseMovieSum; }
            set { if (value != _UseMovieSum) { _UseMovieSum = value; OnPropertyChanged(); } }
        }

        private decimal _ZMin = 0M;
        [WarpSerializable]
        [JsonProperty]
        public decimal ZMin
        {
            get { return _ZMin; }
            set
            {
                if (value != _ZMin)
                {
                    _ZMin = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _ZMax = 5M;
        [WarpSerializable]
        [JsonProperty]
        public decimal ZMax
        {
            get { return _ZMax; }
            set
            {
                if (value != _ZMax)
                {
                    _ZMax = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsMovement : WarpBase
    {
        private bool _ExportMotionTracks = false;
        [WarpSerializable]
        [JsonProperty]
        public bool ExportMotionTracks
        {
            get { return _ExportMotionTracks; }
            set
            {
                if (value != _ExportMotionTracks)
                {
                    _ExportMotionTracks = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _RangeMin = 0.05M;
        [WarpSerializable]
        [JsonProperty]
        public decimal RangeMin
        {
            get { return _RangeMin; }
            set
            {
                if (value != _RangeMin)
                {
                    _RangeMin = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _RangeMax = 0.25M;
        [WarpSerializable]
        [JsonProperty]
        public decimal RangeMax
        {
            get { return _RangeMax; }
            set
            {
                if (value != _RangeMax)
                {
                    _RangeMax = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _Bfactor = -500;
        [WarpSerializable]
        [JsonProperty]
        public decimal Bfactor
        {
            get { return _Bfactor; }
            set { if (value != _Bfactor) { _Bfactor = value; OnPropertyChanged(); } }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsGrids : WarpBase
    {
        private int _CTFX = 5;
        [WarpSerializable]
        [JsonProperty]
        public int CTFX
        {
            get { return _CTFX; }
            set { if (value != _CTFX) { _CTFX = value; OnPropertyChanged(); } }
        }

        private int _CTFY = 5;
        [WarpSerializable]
        [JsonProperty]
        public int CTFY
        {
            get { return _CTFY; }
            set { if (value != _CTFY) { _CTFY = value; OnPropertyChanged(); } }
        }

        private int _CTFZ = 1;
        [WarpSerializable]
        [JsonProperty]
        public int CTFZ
        {
            get { return _CTFZ; }
            set { if (value != _CTFZ) { _CTFZ = value; OnPropertyChanged(); } }
        }

        private int _MovementX = 5;
        [WarpSerializable]
        [JsonProperty]
        public int MovementX
        {
            get { return _MovementX; }
            set { if (value != _MovementX) { _MovementX = value; OnPropertyChanged(); } }
        }

        private int _MovementY = 5;
        [WarpSerializable]
        [JsonProperty]
        public int MovementY
        {
            get { return _MovementY; }
            set { if (value != _MovementY) { _MovementY = value; OnPropertyChanged(); } }
        }

        private int _MovementZ = 20;
        [WarpSerializable]
        [JsonProperty]
        public int MovementZ
        {
            get { return _MovementZ; }
            set { if (value != _MovementZ) { _MovementZ = value; OnPropertyChanged(); } }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsPicking : WarpBase
    {
        private string _ModelPath = "";
        [WarpSerializable]
        [JsonProperty]
        public string ModelPath
        {
            get { return _ModelPath; }
            set { if (value != _ModelPath) { _ModelPath = value; OnPropertyChanged(); } }
        }

        private string _DataStyle = "cryo";
        [WarpSerializable]
        [JsonProperty]
        public string DataStyle
        {
            get { return _DataStyle; }
            set { if (value != _DataStyle) { _DataStyle = value; OnPropertyChanged(); } }
        }

        private int _Diameter = 200;
        [WarpSerializable]
        [JsonProperty]
        public int Diameter
        {
            get { return _Diameter; }
            set { if (value != _Diameter) { _Diameter = value; OnPropertyChanged(); } }
        }

        private decimal _MinimumScore = 0.95M;
        [WarpSerializable]
        [JsonProperty]
        public decimal MinimumScore
        {
            get { return _MinimumScore; }
            set { if (value != _MinimumScore) { _MinimumScore = value; OnPropertyChanged(); } }
        }

        private decimal _MinimumMaskDistance = 0;
        [WarpSerializable]
        [JsonProperty]
        public decimal MinimumMaskDistance
        {
            get { return _MinimumMaskDistance; }
            set { if (value != _MinimumMaskDistance) { _MinimumMaskDistance = value; OnPropertyChanged(); } }
        }

        private bool _DoExport = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoExport
        {
            get { return _DoExport; }
            set { if (value != _DoExport) { _DoExport = value; OnPropertyChanged(); } }
        }

        private int _BoxSize = 128;
        [WarpSerializable]
        [JsonProperty]
        public int BoxSize
        {
            get { return _BoxSize; }
            set { if (value != _BoxSize) { _BoxSize = value; OnPropertyChanged(); } }
        }

        private int _FourierCropBox = 2;
        [WarpSerializable]
        [JsonProperty]
        public int FourierCropBox
        {
            get { return _FourierCropBox; }
            set { if (value != _FourierCropBox) { _FourierCropBox = value; OnPropertyChanged(); } }
        }

        private bool _DoFourierCrop = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoFourierCrop
        {
            get { return _DoFourierCrop; }
            set { if (value!= _DoFourierCrop) { _DoFourierCrop = value; OnPropertyChanged(); } }
        }

        private bool _Invert = true;
        [WarpSerializable]
        [JsonProperty]
        public bool Invert
        {
            get { return _Invert; }
            set { if (value != _Invert) { _Invert = value; OnPropertyChanged(); } }
        }

        private bool _Normalize = true;
        [WarpSerializable]
        [JsonProperty]
        public bool Normalize
        {
            get { return _Normalize; }
            set { if (value != _Normalize) { _Normalize = value; OnPropertyChanged(); } }
        }

        private bool _DoRunningWindow = true;
        [WarpSerializable]
        [JsonProperty]
        public bool DoRunningWindow
        {
            get { return _DoRunningWindow; }
            set { if (value != _DoRunningWindow) { _DoRunningWindow = value; OnPropertyChanged(); } }
        }

        private int _RunningWindowLength = 10000;
        [WarpSerializable]
        [JsonProperty]
        public int RunningWindowLength
        {
            get { return _RunningWindowLength; }
            set { if (value != _RunningWindowLength) { _RunningWindowLength = value; OnPropertyChanged(); } }
        }

		private bool _WriteOpticGroups = false;
		[WarpSerializable]
		[JsonProperty]
		public bool WriteOpticGroups
		{
			get { return _WriteOpticGroups; }
			set { if (value != _WriteOpticGroups) { _WriteOpticGroups = value; OnPropertyChanged(); } }
		}

        private int _WriteOpticGroupsN = 500;
        [WarpSerializable]
        [JsonProperty]
        public int WriteOpticGroupsN
        {
            get { return _WriteOpticGroupsN; }
            set { if (value != _WriteOpticGroupsN) { _WriteOpticGroupsN = value; OnPropertyChanged(); } }
        }

        private bool _NewOpticGroups = false;
		[WarpSerializable]
		[JsonProperty]
		public bool NewOpticGroups
		{
			get { return _NewOpticGroups; }
			set { if (value != _NewOpticGroups) { _NewOpticGroups = value; OnPropertyChanged(); } }
		}

		private int _NewOpticGroupsEvery = 1;
		[WarpSerializable]
		[JsonProperty]
		public int NewOpticGroupsEvery
		{
			get { return _NewOpticGroupsEvery; }
			set { if (value != _NewOpticGroupsEvery) { _NewOpticGroupsEvery = value; OnPropertyChanged(); } }
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class OptionsClassification : WarpBase
	{
        private string _SshKey;
        [WarpSerializable]
        [JsonProperty]
        public string SshKey
        {
            get { return _SshKey; }
            set
            {
                if (value != _SshKey && File.Exists(value))
                {
                    try
                    {
                        SshKeyObject = new PrivateKeyFile(value);
                        _SshKey = value;
                        OnPropertyChanged();
                    }
                    catch (Exception e)
                    {
                        //do nothing
                    }
                }
            }
        }

        public PrivateKeyFile SshKeyObject { get; set; }

        private string _UserName;
        [WarpSerializable]
        [JsonProperty]
        public string UserName
        {
            get { return _UserName; }
            set { if (value != _UserName) { _UserName = value; OnPropertyChanged(); } }
        }

        private string _Server;
        [WarpSerializable]
        [JsonProperty]
        public string Server
        {
            get { return _Server; }
            set {
                if (value != _Server)
                {
                    _Server = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _Ip;
        [WarpSerializable]
        [JsonProperty]
        public string Ip
        {
            get { return _Ip; }
            set
            {
                if (value != _Ip)
                {
                    var s = value.Split(':').ToList();
                    if (s.Count > 1)
                    {
                        int p;
                        Port = int.TryParse(s.Last(), out p) ? p : 22;
                        s.Last();
                        s.RemoveAt(s.Count - 1);
                        _Server = string.Join(":", s);
                    }
                    else
                    {
                        _Server = value;
                    }
                    _Ip = value;
                    OnPropertyChanged();
                }
            }
        }


        private int _Port = 22;
        [WarpSerializable]
        [JsonProperty]
        public int Port
        {
            get { return _Port; }
            set
            {
                if (value != _Port)
                {
                    OnPropertyChanged();
                }
            }
        }

        private bool _class2D = true;
		[WarpSerializable]
		[JsonProperty]
		public bool class2D
		{
			get { return _class2D; }
			set { if (value != _class2D) { _class2D = value; OnPropertyChanged(); } }
		}

		private bool _class3D = false;
		[WarpSerializable]
		[JsonProperty]
		public bool class3D
		{
			get { return _class3D; }
			set { if (value != _class3D) { _class3D = value; OnPropertyChanged(); } }
		}

		private int _NClasses = 2;
		[WarpSerializable]
		[JsonProperty]
		public int NClasses
		{
			get { return _NClasses; }
			set { if (value != _NClasses) { _NClasses = value; OnPropertyChanged(); } }
		}

        private int _NParticles = 1000;
        [WarpSerializable]
        [JsonProperty]
        public int NParticles
        {
            get { return _NParticles; }
            set { if (value != _NParticles) { _NParticles = value; OnPropertyChanged(); } }
        }

        private string _Results = "Results not available";
        [WarpSerializable]
        [JsonProperty]
        public string Results
        {
            get { return _Results; }
            set { if (value != _Results) { _Results = value; OnPropertyChanged(); } }
        }

        private string _Countdown = "Results not available";
        public string Countdown
        {
            get { return _Countdown; }
            set { if (value != _Countdown) { _Countdown = value; OnPropertyChanged(); } }
        }


        private string _EveryNParticles;
        [WarpSerializable]
        [JsonProperty]
        public string EveryNParticles
        {
            get { return _EveryNParticles; }
            set { if (value != _EveryNParticles) { _EveryNParticles = value; OnPropertyChanged(); } }
        }

        private bool _DoEveryNParticles = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoEveryNParticles
        {
            get { return _DoEveryNParticles; }
            set { if (value != _DoEveryNParticles) { _DoEveryNParticles = value; OnPropertyChanged(); } }
        }

        private string _EveryHours;
        [WarpSerializable]
        [JsonProperty]
        public string EveryHours
        {
            get { return _EveryHours; }
            set { if (value != _EveryHours) { _EveryHours = value; OnPropertyChanged(); } }
        }

        private bool _DoEveryHours = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoEveryHours
        {
            get { return _DoEveryHours; }
            set { if (value != _DoEveryHours) { _DoEveryHours = value; OnPropertyChanged(); } }
        }

        private bool _DoManualClassification = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoManualClassification
        {
            get { return _DoManualClassification; }
            set { if (value != _DoManualClassification) { _DoManualClassification = value; OnPropertyChanged(); } }
        }

        private bool _DoImmediateClassification = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoImmediateClassification
        {
            get { return _DoImmediateClassification; }
            set { if (value != _DoImmediateClassification) { _DoImmediateClassification = value; OnPropertyChanged(); } }
        }

        private bool _DoAtSessionEnd = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoAtSessionEnd
        {
            get { return _DoAtSessionEnd; }
            set { if (value != _DoAtSessionEnd) { _DoAtSessionEnd = value; OnPropertyChanged(); } }
        }

        private string _ClassificationMountPoint = "";
        [WarpSerializable]
        [JsonProperty]
        public string ClassificationMountPoint
        {
            get { return _ClassificationMountPoint; }
            set { if (value != _ClassificationMountPoint) { _ClassificationMountPoint = value; OnPropertyChanged(); } }
        }

        private string _CryosparcProject = "";
        [WarpSerializable]
        [JsonProperty]
        public string CryosparcProject
        {
            get { return _CryosparcProject; }
            set { if (value != _CryosparcProject) { _CryosparcProject = value; OnPropertyChanged();} }
        }

        private string _CryosparcProjectName = "";
        [WarpSerializable]
        [JsonProperty]
        public string CryosparcProjectName
        {
            get { return _CryosparcProjectName; }
            set { if (value != _CryosparcProjectName) { _CryosparcProjectName = value; OnPropertyChanged(); } }
        }

        private string _CryosparcProjectDir = "";
        [WarpSerializable]
        [JsonProperty]
        public string CryosparcProjectDir
        {
            get { return _CryosparcProjectDir; }
            set { if (value != _CryosparcProjectDir) { _CryosparcProjectDir = value; OnPropertyChanged(); } }
        }

        private string _CryosparcUserEmail = "";
        [WarpSerializable]
        [JsonProperty]
        public string CryosparcUserEmail
        {
            get { return _CryosparcUserEmail; }
            set { if (value != _CryosparcUserEmail) { _CryosparcUserEmail = value; OnPropertyChanged(); } }
        }

        private string _CryosparcLane = "default";
        [WarpSerializable]
        [JsonProperty]
        public string CryosparcLane
        {
            get { return _CryosparcLane; }
            set { if (value != _CryosparcLane) { _CryosparcLane = value; OnPropertyChanged(); } }
        }

        private string _CryosparcLicense = "837bd996-d55f-11eb-a911-a707e5386885";
        [WarpSerializable]
        [JsonProperty]
        public string CryosparcLicense
        {
            get { return _CryosparcLicense; }
            set { if (value != _CryosparcLicense) { _CryosparcLicense = value; OnPropertyChanged(); } }
        }

        private bool _LinuxServerOK = false;
        public bool LinuxServerOk {
            get { return _LinuxServerOK; }
            set { if (value != _LinuxServerOK) { _LinuxServerOK = value; OnPropertyChanged(); } }
        }

        private CancellationTokenSource TestLinuxServerConnectionToken = new CancellationTokenSource();

        private bool _IsTestingLinuxServer = false;
        public bool IsTestingLinuxServer
        {
            get { return _IsTestingLinuxServer; }
            set { if (value != _IsTestingLinuxServer) { _IsTestingLinuxServer = value; OnPropertyChanged(); } }
        }


        private SemaphoreSlim GuessLinuxServerMountpointSemaphore = new SemaphoreSlim(1);
        private CancellationTokenSource GuessLinuxServerMountpointToken = new CancellationTokenSource();

        public async Task GuessLinuxServerMountpoint(CancellationToken token, string importFolder = null)
        {
            GuessLinuxServerMountpointToken.Cancel();
            await GuessLinuxServerMountpointSemaphore.WaitAsync();
            IsTestingLinuxServer = true;
            GuessLinuxServerMountpointToken = new CancellationTokenSource();
            var wortkask = Task.Run(() => { GuessLinuxServerMountpointWork(GuessLinuxServerMountpointToken.Token, importFolder); });
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    GuessLinuxServerMountpointToken.Cancel();
                    break;
                }
                if (wortkask.IsCompleted)
                {
                    break;
                }
                await Task.Delay(200);
            }

            GuessLinuxServerMountpointSemaphore.Release();
            IsTestingLinuxServer = false;
        }
        private void GuessLinuxServerMountpointWork(CancellationToken token, string importFolder = null)
        {
            Console.WriteLine("Guess linux server");
            if (Server == "" || UserName == "" || SshKeyObject == null || importFolder == null)
            {
                return;
            }

            if (!Directory.Exists(importFolder))
            {
                return;
            }

            var sshsettings = new OptionsSshSettings { IP = Server, Username = UserName, Port = Port, SshKeyObject = SshKeyObject };

            string guessedpath = default(string);
            guessedpath = GuessLinuxDirectory.GuessLinuxDirectoryFromPath(importFolder, sshsettings, token);
            Console.WriteLine($"Guessed path: {guessedpath}");

            if (guessedpath != null)
            {
                LinuxServerOk = true;
                ClassificationMountPoint = guessedpath;
            }
        }
            

        private SemaphoreSlim TestLinuxServerSemaphore = new SemaphoreSlim(1);
        public async Task TestLinuxServerConnection(CancellationToken token, string importFolder = null)
        {
            TestLinuxServerConnectionToken.Cancel();
            await TestLinuxServerSemaphore.WaitAsync();
            IsTestingLinuxServer = true;
            TestLinuxServerConnectionToken = new CancellationTokenSource();
            var wortkask = Task.Run(() => { TestLinuxServerConnectionWork(TestLinuxServerConnectionToken.Token, importFolder); });
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    TestLinuxServerConnectionToken.Cancel();
                    break;
                }
                if (wortkask.IsCompleted)
                {
                    break;
                }
                await Task.Delay(200);
            }

            TestLinuxServerSemaphore.Release();
            IsTestingLinuxServer = false;
        }

        private void TestLinuxServerConnectionWork(CancellationToken token, string importFolder = null)
        {
            Console.WriteLine("Testing linux server");
            if (Server == "" || UserName == "" || SshKeyObject == null || importFolder == null)
            {
                LinuxServerOk = false;
                return;
            }

            if ( !Directory.Exists(importFolder) )
            {
                LinuxServerOk = false;
                return;
            }

            var sshsettings = new OptionsSshSettings { IP = Server, Username = UserName, Port = Port, SshKeyObject = SshKeyObject };

            if (GuessLinuxDirectory.LinuxDirectoryIsCorrect(sshsettings, importFolder, ClassificationMountPoint, token))
            {
                LinuxServerOk = true;
            }
            else
            {
                LinuxServerOk = false;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
	public class OptionsStacker : WarpBase
	{
		private string _Folder = "";
		[WarpSerializable]
		[JsonProperty]
		public string Folder
		{
			get { return _Folder; }
			set { if (value != _Folder) { _Folder = value; OnPropertyChanged(); } }
		}

		private bool _GridScreening = false;
		[WarpSerializable]
		[JsonProperty]
		public bool GridScreening
		{
			get { return _GridScreening; }
			set { if (value != _GridScreening) { _GridScreening = value; OnPropertyChanged(); } }
		}

	}

	[JsonObject(MemberSerialization.OptIn)]
    public class OptionsTomo : WarpBase
    {
        private decimal _DimensionsX = 3712;
        [WarpSerializable]
        [JsonProperty]
        public decimal DimensionsX
        {
            get { return _DimensionsX; }
            set { if (value != _DimensionsX) { _DimensionsX = value; OnPropertyChanged(); } }
        }

        private decimal _DimensionsY = 3712;
        [WarpSerializable]
        [JsonProperty]
        public decimal DimensionsY
        {
            get { return _DimensionsY; }
            set { if (value != _DimensionsY) { _DimensionsY = value; OnPropertyChanged(); } }
        }

        private decimal _DimensionsZ = 1400;
        [WarpSerializable]
        [JsonProperty]
        public decimal DimensionsZ
        {
            get { return _DimensionsZ; }
            set { if (value != _DimensionsZ) { _DimensionsZ = value; OnPropertyChanged(); } }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsExport : WarpBase
    {
        private bool _DoAverage = true;
        [WarpSerializable]
        [JsonProperty]
        public bool DoAverage
        {
            get { return _DoAverage; }
            set
            {
                if (value != _DoAverage)
                {
                    _DoAverage = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _DoStack = false;
        [WarpSerializable]
        [JsonProperty]
        public bool DoStack
        {
            get { return _DoStack; }
            set
            {
                if (value != _DoStack)
                {
                    _DoStack = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _DoDeconvolve = false;
        [WarpSerializable]
        public bool DoDeconvolve
        {
            get { return _DoDeconvolve; }
            set { if (value != _DoDeconvolve) { _DoDeconvolve = value; OnPropertyChanged(); } }
        }

        private decimal _DeconvolutionStrength = 1;
        [WarpSerializable]
        public decimal DeconvolutionStrength
        {
            get { return _DeconvolutionStrength; }
            set { if (value != _DeconvolutionStrength) { _DeconvolutionStrength = value; OnPropertyChanged(); } }
        }

        private decimal _DeconvolutionFalloff = 1;
        [WarpSerializable]
        public decimal DeconvolutionFalloff
        {
            get { return _DeconvolutionFalloff; }
            set { if (value != _DeconvolutionFalloff) { _DeconvolutionFalloff = value; OnPropertyChanged(); } }
        }

        private int _StackGroupSize = 1;
        [WarpSerializable]
        [JsonProperty]
        public int StackGroupSize
        {
            get { return _StackGroupSize; }
            set
            {
                if (value != _StackGroupSize)
                {
                    _StackGroupSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _SkipFirstN = 0;
        [WarpSerializable]
        [JsonProperty]
        public int SkipFirstN
        {
            get { return _SkipFirstN; }
            set { if (value != _SkipFirstN) { _SkipFirstN = value; OnPropertyChanged(); } }
        }

        private int _SkipLastN = 0;
        [WarpSerializable]
        [JsonProperty]
        public int SkipLastN
        {
            get { return _SkipLastN; }
            set { if (value != _SkipLastN) { _SkipLastN = value; OnPropertyChanged(); } }
        }

    }

    public class OptionsTasks : WarpBase
    {
        #region Common

        private bool _UseRelativePaths = true;
        [WarpSerializable]
        public bool UseRelativePaths
        {
            get { return _UseRelativePaths; }
            set { if (value != _UseRelativePaths) { _UseRelativePaths = value; OnPropertyChanged(); } }
        }

        private bool _IncludeFilteredOut = false;
        [WarpSerializable]
        public bool IncludeFilteredOut
        {
            get { return _IncludeFilteredOut; }
            set { if (value != _IncludeFilteredOut) { _IncludeFilteredOut = value; OnPropertyChanged(); } }
        }

        private bool _IncludeUnselected = false;
        [WarpSerializable]
        public bool IncludeUnselected
        {
            get { return _IncludeUnselected; }
            set { if (value != _IncludeUnselected) { _IncludeUnselected = value; OnPropertyChanged(); } }
        }

        private bool _InputOnePerItem = false;
        [WarpSerializable]
        public bool InputOnePerItem
        {
            get { return _InputOnePerItem; }
            set { if (value != _InputOnePerItem) { _InputOnePerItem = value; OnPropertyChanged(); } }
        }

        private decimal _InputPixelSize = 1;
        [WarpSerializable]
        public decimal InputPixelSize
        {
            get { return _InputPixelSize; }
            set { if (value != _InputPixelSize) { _InputPixelSize = value; OnPropertyChanged(); } }
        }

        private decimal _InputShiftPixelSize = 1;
        [WarpSerializable]
        public decimal InputShiftPixelSize
        {
            get { return _InputShiftPixelSize; }
            set { if (value != _InputShiftPixelSize) { _InputShiftPixelSize = value; OnPropertyChanged(); } }
        }

        private decimal _OutputPixelSize = 1;
        [WarpSerializable]
        public decimal OutputPixelSize
        {
            get { return _OutputPixelSize; }
            set { if (value != _OutputPixelSize) { _OutputPixelSize = value; OnPropertyChanged(); } }
        }

        private string _OutputSuffix = "";
        [WarpSerializable]
        public string OutputSuffix
        {
            get { return _OutputSuffix; }
            set { if (value != _OutputSuffix) { _OutputSuffix = value; OnPropertyChanged(); } }
        }

        private bool _InputInvert = true;
        [WarpSerializable]
        public bool InputInvert
        {
            get { return _InputInvert; }
            set { if (value != _InputInvert) { _InputInvert = value; OnPropertyChanged(); } }
        }

        private bool _InputNormalize = true;
        [WarpSerializable]
        public bool InputNormalize
        {
            get { return _InputNormalize; }
            set { if (value != _InputNormalize) { _InputNormalize = value; OnPropertyChanged(); } }
        }

        private bool _InputFlipX = false;
        [WarpSerializable]
        public bool InputFlipX
        {
            get { return _InputFlipX; }
            set { if (value != _InputFlipX) { _InputFlipX = value; OnPropertyChanged(); } }
        }

        private bool _InputFlipY = false;
        [WarpSerializable]
        public bool InputFlipY
        {
            get { return _InputFlipY; }
            set { if (value != _InputFlipY) { _InputFlipY = value; OnPropertyChanged(); } }
        }

        private bool _OutputNormalize = true;
        [WarpSerializable]
        public bool OutputNormalize
        {
            get { return _OutputNormalize; }
            set { if (value != _OutputNormalize) { _OutputNormalize = value; OnPropertyChanged(); } }
        }

        #endregion

        #region 2D

        private bool _MicListMakePolishing = false;
        [WarpSerializable]
        public bool MicListMakePolishing
        {
            get { return _MicListMakePolishing; }
            set { if (value != _MicListMakePolishing) { _MicListMakePolishing = value; OnPropertyChanged(); } }
        }

        private bool _AdjustDefocusSkipExcluded = true;
        [WarpSerializable]
        public bool AdjustDefocusSkipExcluded
        {
            get { return _AdjustDefocusSkipExcluded; }
            set { if (value != _AdjustDefocusSkipExcluded) { _AdjustDefocusSkipExcluded = value; OnPropertyChanged(); } }
        }

        private bool _AdjustDefocusDeleteExcluded = false;
        [WarpSerializable]
        public bool AdjustDefocusDeleteExcluded
        {
            get { return _AdjustDefocusDeleteExcluded; }
            set { if (value != _AdjustDefocusDeleteExcluded) { _AdjustDefocusDeleteExcluded = value; OnPropertyChanged(); } }
        }

        private decimal _Export2DPixel = 1M;
        [WarpSerializable]
        public decimal Export2DPixel
        {
            get { return _Export2DPixel; }
            set { if (value != _Export2DPixel) { _Export2DPixel = value; OnPropertyChanged(); } }
        }

        private decimal _Export2DBoxSize = 128;
        [WarpSerializable]
        public decimal Export2DBoxSize
        {
            get { return _Export2DBoxSize; }
            set { if (value != _Export2DBoxSize) { _Export2DBoxSize = value; OnPropertyChanged(); } }
        }

        private decimal _Export2DParticleDiameter = 100;
        [WarpSerializable]
        public decimal Export2DParticleDiameter
        {
            get { return _Export2DParticleDiameter; }
            set { if (value != _Export2DParticleDiameter) { _Export2DParticleDiameter = value; OnPropertyChanged(); } }
        }

        private bool _Export2DDoAverages = true;
        [WarpSerializable]
        public bool Export2DDoAverages
        {
            get { return _Export2DDoAverages; }
            set { if (value != _Export2DDoAverages) { _Export2DDoAverages = value; OnPropertyChanged(); } }
        }

        private bool _Export2DDoMovies = false;
        [WarpSerializable]
        public bool Export2DDoMovies
        {
            get { return _Export2DDoMovies; }
            set { if (value != _Export2DDoMovies) { _Export2DDoMovies = value; OnPropertyChanged(); } }
        }

        private bool _Export2DDoOnlyTable = false;
        [WarpSerializable]
        public bool Export2DDoOnlyTable
        {
            get { return _Export2DDoOnlyTable; }
            set { if (value != _Export2DDoOnlyTable) { _Export2DDoOnlyTable = value; OnPropertyChanged(); } }
        }

        private bool _Export2DDoDenoisingPairs = false;
        [WarpSerializable]
        public bool Export2DDoDenoisingPairs
        {
            get { return _Export2DDoDenoisingPairs; }
            set { if (value != _Export2DDoDenoisingPairs) { _Export2DDoDenoisingPairs = value; OnPropertyChanged(); } }
        }

        private bool _Export2DPreflip = false;
        [WarpSerializable]
        public bool Export2DPreflip
        {
            get { return _Export2DPreflip; }
            set { if (value != _Export2DPreflip) { _Export2DPreflip = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Tomo

        #region Full reconstruction

        private decimal _TomoFullReconstructPixel = 1M;
        [WarpSerializable]
        public decimal TomoFullReconstructPixel
        {
            get { return _TomoFullReconstructPixel; }
            set { if (value != _TomoFullReconstructPixel) { _TomoFullReconstructPixel = value; OnPropertyChanged(); } }
        }

        private bool _TomoFullReconstructDoDeconv = false;
        [WarpSerializable]
        public bool TomoFullReconstructDoDeconv
        {
            get { return _TomoFullReconstructDoDeconv; }
            set { if (value != _TomoFullReconstructDoDeconv) { _TomoFullReconstructDoDeconv = value; OnPropertyChanged(); } }
        }

        private decimal _TomoFullReconstructDeconvStrength = 1M;
        [WarpSerializable]
        public decimal TomoFullReconstructDeconvStrength
        {
            get { return _TomoFullReconstructDeconvStrength; }
            set { if (value != _TomoFullReconstructDeconvStrength) { _TomoFullReconstructDeconvStrength = value; OnPropertyChanged(); } }
        }

        private decimal _TomoFullReconstructDeconvFalloff = 1M;
        [WarpSerializable]
        public decimal TomoFullReconstructDeconvFalloff
        {
            get { return _TomoFullReconstructDeconvFalloff; }
            set { if (value != _TomoFullReconstructDeconvFalloff) { _TomoFullReconstructDeconvFalloff = value; OnPropertyChanged(); } }
        }

        private decimal _TomoFullReconstructDeconvHighpass = 300;
        [WarpSerializable]
        public decimal TomoFullReconstructDeconvHighpass
        {
            get { return _TomoFullReconstructDeconvHighpass; }
            set { if (value != _TomoFullReconstructDeconvHighpass) { _TomoFullReconstructDeconvHighpass = value; OnPropertyChanged(); } }
        }

        private bool _TomoFullReconstructInvert = true;
        [WarpSerializable]
        public bool TomoFullReconstructInvert
        {
            get { return _TomoFullReconstructInvert; }
            set { if (value != _TomoFullReconstructInvert) { _TomoFullReconstructInvert = value; OnPropertyChanged(); } }
        }

        private bool _TomoFullReconstructNormalize = true;
        [WarpSerializable]
        public bool TomoFullReconstructNormalize
        {
            get { return _TomoFullReconstructNormalize; }
            set { if (value != _TomoFullReconstructNormalize) { _TomoFullReconstructNormalize = value; OnPropertyChanged(); } }
        }

        private bool _TomoFullReconstructPrepareDenoising = false;
        [WarpSerializable]
        public bool TomoFullReconstructPrepareDenoising
        {
            get { return _TomoFullReconstructPrepareDenoising; }
            set { if (value != _TomoFullReconstructPrepareDenoising) { _TomoFullReconstructPrepareDenoising = value; OnPropertyChanged(); } }
        }

        private bool _TomoFullReconstructOnlyFullVoxels = false;
        [WarpSerializable]
        public bool TomoFullReconstructOnlyFullVoxels
        {
            get { return _TomoFullReconstructOnlyFullVoxels; }
            set { if (value != _TomoFullReconstructOnlyFullVoxels) { _TomoFullReconstructOnlyFullVoxels = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Sub reconstruction

        private bool _TomoSubReconstructNormalizedCoords = false;
        [WarpSerializable]
        public bool TomoSubReconstructNormalizedCoords
        {
            get { return _TomoSubReconstructNormalizedCoords; }
            set { if (value != _TomoSubReconstructNormalizedCoords) { _TomoSubReconstructNormalizedCoords = value; OnPropertyChanged(); } }
        }

        private decimal _TomoSubReconstructPixel = 1M;
        [WarpSerializable]
        public decimal TomoSubReconstructPixel
        {
            get { return _TomoSubReconstructPixel; }
            set { if (value != _TomoSubReconstructPixel) { _TomoSubReconstructPixel = value; OnPropertyChanged(); } }
        }

        private decimal _TomoSubReconstructBox = 128;
        [WarpSerializable]
        public decimal TomoSubReconstructBox
        {
            get { return _TomoSubReconstructBox; }
            set { if (value != _TomoSubReconstructBox) { _TomoSubReconstructBox = value; OnPropertyChanged(); } }
        }

        private decimal _TomoSubReconstructDiameter = 100;
        [WarpSerializable]
        public decimal TomoSubReconstructDiameter
        {
            get { return _TomoSubReconstructDiameter; }
            set { if (value != _TomoSubReconstructDiameter) { _TomoSubReconstructDiameter = value; OnPropertyChanged(); } }
        }

        private bool _TomoSubReconstructVolume = true;
        [WarpSerializable]
        public bool TomoSubReconstructVolume
        {
            get { return _TomoSubReconstructVolume; }
            set { if (value != _TomoSubReconstructVolume) { _TomoSubReconstructVolume = value; OnPropertyChanged(); } }
        }

        private bool _TomoSubReconstructSeries = false;
        [WarpSerializable]
        public bool TomoSubReconstructSeries
        {
            get { return _TomoSubReconstructSeries; }
            set { if (value != _TomoSubReconstructSeries) { _TomoSubReconstructSeries = value; OnPropertyChanged(); } }
        }

        private decimal _TomoSubReconstructShiftX = 0M;
        [WarpSerializable]
        public decimal TomoSubReconstructShiftX
        {
            get { return _TomoSubReconstructShiftX; }
            set { if (value != _TomoSubReconstructShiftX) { _TomoSubReconstructShiftX = value; OnPropertyChanged(); } }
        }

        private decimal _TomoSubReconstructShiftY = 0M;
        [WarpSerializable]
        public decimal TomoSubReconstructShiftY
        {
            get { return _TomoSubReconstructShiftY; }
            set { if (value != _TomoSubReconstructShiftY) { _TomoSubReconstructShiftY = value; OnPropertyChanged(); } }
        }

        private decimal _TomoSubReconstructShiftZ = 0M;
        [WarpSerializable]
        public decimal TomoSubReconstructShiftZ
        {
            get { return _TomoSubReconstructShiftZ; }
            set { if (value != _TomoSubReconstructShiftZ) { _TomoSubReconstructShiftZ = value; OnPropertyChanged(); } }
        }

        private bool _TomoSubReconstructPrerotated = false;
        [WarpSerializable]
        public bool TomoSubReconstructPrerotated
        {
            get { return _TomoSubReconstructPrerotated; }
            set { if (value != _TomoSubReconstructPrerotated) { _TomoSubReconstructPrerotated = value; OnPropertyChanged(); } }
        }

        private bool _TomoSubReconstructDoLimitDose = false;
        [WarpSerializable]
        public bool TomoSubReconstructDoLimitDose
        {
            get { return _TomoSubReconstructDoLimitDose; }
            set { if (value != _TomoSubReconstructDoLimitDose) { _TomoSubReconstructDoLimitDose = value; OnPropertyChanged(); } }
        }

        private int _TomoSubReconstructNTilts = 1;
        [WarpSerializable]
        public int TomoSubReconstructNTilts
        {
            get { return _TomoSubReconstructNTilts; }
            set { if (value != _TomoSubReconstructNTilts) { _TomoSubReconstructNTilts = value; OnPropertyChanged(); } }
        }

        private bool _TomoSubReconstructMakeSparse = true;
        [WarpSerializable]
        public bool TomoSubReconstructMakeSparse
        {
            get { return _TomoSubReconstructMakeSparse; }
            set { if (value != _TomoSubReconstructMakeSparse) { _TomoSubReconstructMakeSparse = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Template matching

        private decimal _TomoMatchTemplatePixel = 1M;
        [WarpSerializable]
        public decimal TomoMatchTemplatePixel
        {
            get { return _TomoMatchTemplatePixel; }
            set { if (value != _TomoMatchTemplatePixel) { _TomoMatchTemplatePixel = value; OnPropertyChanged(); } }
        }

        private decimal _TomoMatchTemplateDiameter = 100;
        [WarpSerializable]
        public decimal TomoMatchTemplateDiameter
        {
            get { return _TomoMatchTemplateDiameter; }
            set
            {
                if (value != _TomoMatchTemplateDiameter)
                {
                    _TomoMatchTemplateDiameter = value;
                    OnPropertyChanged();
                    TomoUpdateMatchRecommendation();
                }
            }
        }

        private decimal _TomoMatchTemplateFraction = 100M;
        [WarpSerializable]
        public decimal TomoMatchTemplateFraction
        {
            get { return _TomoMatchTemplateFraction; }
            set { if (value != _TomoMatchTemplateFraction) { _TomoMatchTemplateFraction = value; OnPropertyChanged(); } }
        }

        private bool _TomoMatchWhitenSpectrum = true;
        [WarpSerializable]
        public bool TomoMatchWhitenSpectrum
        {
            get { return _TomoMatchWhitenSpectrum; }
            set { if (value != _TomoMatchWhitenSpectrum) { _TomoMatchWhitenSpectrum = value; OnPropertyChanged(); } }
        }

        private decimal _TomoMatchHealpixOrder = 1;
        [WarpSerializable]
        public decimal TomoMatchHealpixOrder
        {
            get { return _TomoMatchHealpixOrder; }
            set
            {
                if (value != _TomoMatchHealpixOrder)
                {
                    _TomoMatchHealpixOrder = value;
                    OnPropertyChanged();

                    TomoMatchHealpixAngle = Math.Round(60M / (decimal)Math.Pow(2, (double)value), 3);
                }
            }
        }

        private decimal _TomoMatchHealpixAngle = 30;
        public decimal TomoMatchHealpixAngle
        {
            get { return _TomoMatchHealpixAngle; }
            set
            {
                if (value != _TomoMatchHealpixAngle)
                {
                    _TomoMatchHealpixAngle = value;
                    OnPropertyChanged();
                    TomoUpdateMatchRecommendation();
                }
            }
        }

        private string _TomoMatchSymmetry = "C1";
        [WarpSerializable]
        public string TomoMatchSymmetry
        {
            get { return _TomoMatchSymmetry; }
            set { if (value != _TomoMatchSymmetry) { _TomoMatchSymmetry = value; OnPropertyChanged(); } }
        }

        private decimal _TomoMatchRecommendedAngPix = 25.88M;
        public decimal TomoMatchRecommendedAngPix
        {
            get { return _TomoMatchRecommendedAngPix; }
            set { if (value != _TomoMatchRecommendedAngPix) { _TomoMatchRecommendedAngPix = value; OnPropertyChanged(); } }
        }

        private void TomoUpdateMatchRecommendation()
        {
            float2 AngularSampling = new float2((float)Math.Sin((float)TomoMatchHealpixAngle * Helper.ToRad),
                                                1f - (float)Math.Cos((float)TomoMatchHealpixAngle * Helper.ToRad));
            decimal AtLeast = TomoMatchTemplateDiameter / 2 * (decimal)AngularSampling.Length();
            AtLeast = Math.Round(AtLeast, 2);
            TomoMatchRecommendedAngPix = AtLeast;
        }

        private decimal _TomoMatchNResults = 1000;
        [WarpSerializable]
        public decimal TomoMatchNResults
        {
            get { return _TomoMatchNResults; }
            set { if (value != _TomoMatchNResults) { _TomoMatchNResults = value; OnPropertyChanged(); } }
        }

        private bool _ReuseCorrVolumes = false;
        [WarpSerializable]
        public bool ReuseCorrVolumes
        {
            get { return _ReuseCorrVolumes; }
            set { if (value != _ReuseCorrVolumes) { _ReuseCorrVolumes = value; OnPropertyChanged(); } }
        }

        #endregion

        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsFilter : WarpBase
    {
        private decimal _AstigmatismMax = 3;
        [WarpSerializable]
        [JsonProperty]
        public decimal AstigmatismMax
        {
            get { return _AstigmatismMax; }
            set { if (value != _AstigmatismMax) { _AstigmatismMax = value; OnPropertyChanged(); } }
        }

        private decimal _DefocusMin = 0;
        [WarpSerializable]
        [JsonProperty]
        public decimal DefocusMin
        {
            get { return _DefocusMin; }
            set { if (value != _DefocusMin) { _DefocusMin = value; OnPropertyChanged(); } }
        }

        private decimal _DefocusMax = 5;
        [WarpSerializable]
        [JsonProperty]
        public decimal DefocusMax
        {
            get { return _DefocusMax; }
            set { if (value != _DefocusMax) { _DefocusMax = value; OnPropertyChanged(); } }
        }

        private decimal _PhaseMin = 0;
        [WarpSerializable]
        [JsonProperty]
        public decimal PhaseMin
        {
            get { return _PhaseMin; }
            set { if (value != _PhaseMin) { _PhaseMin = value; OnPropertyChanged(); } }
        }

        private decimal _PhaseMax = 1;
        [WarpSerializable]
        [JsonProperty]
        public decimal PhaseMax
        {
            get { return _PhaseMax; }
            set { if (value != _PhaseMax) { _PhaseMax = value; OnPropertyChanged(); } }
        }

        private decimal _ResolutionMax = 5;
        [WarpSerializable]
        [JsonProperty]
        public decimal ResolutionMax
        {
            get { return _ResolutionMax; }
            set { if (value != _ResolutionMax) { _ResolutionMax = value; OnPropertyChanged(); } }
        }

        private decimal _MotionMax = 5;
        [WarpSerializable]
        [JsonProperty]
        public decimal MotionMax
        {
            get { return _MotionMax; }
            set { if (value != _MotionMax) { _MotionMax = value; OnPropertyChanged(); } }
        }

        private string _ParticlesSuffix = "";
        [WarpSerializable]
        [JsonProperty]
        public string ParticlesSuffix
        {
            get { return _ParticlesSuffix; }
            set { if (value != _ParticlesSuffix) { _ParticlesSuffix = value; OnPropertyChanged(); } }
        }

        private int _ParticlesMin = 1;
        [WarpSerializable]
        [JsonProperty]
        public int ParticlesMin
        {
            get { return _ParticlesMin; }
            set { if (value != _ParticlesMin) { _ParticlesMin = value; OnPropertyChanged(); } }
        }

        private decimal _MaskPercentage = 10;
        [WarpSerializable]
        [JsonProperty]
        public decimal MaskPercentage
        {
            get { return _MaskPercentage; }
            set { if (value != _MaskPercentage) { _MaskPercentage = value; OnPropertyChanged(); } }
        }
    }

    public class OptionsAdvanced : WarpBase
    {
        private int _ProjectionOversample = 2;
        [WarpSerializable]
        public int ProjectionOversample
        {
            get { return _ProjectionOversample; }
            set { if (value != _ProjectionOversample) { _ProjectionOversample = value; OnPropertyChanged(); } }
        }
    }

    public class OptionsRuntime : WarpBase
    {
        private int _DeviceCount = 0;
        public int DeviceCount
        {
            get { return _DeviceCount; }
            set { if (value != _DeviceCount) { _DeviceCount = value; OnPropertyChanged(); } }
        }

        private decimal _BinnedPixelSizeMean = 1M;
        public decimal BinnedPixelSizeMean
        {
            get { return _BinnedPixelSizeMean; }
            set { if (value != _BinnedPixelSizeMean) { _BinnedPixelSizeMean = value; OnPropertyChanged(); } }
        }

        private string _GainReferenceHash = "";
        public string GainReferenceHash
        {
            get { return _GainReferenceHash; }
            set { if (value != _GainReferenceHash) { _GainReferenceHash = value; OnPropertyChanged(); } }
        }

        private string _DefectMapHash = "";
        public string DefectMapHash
        {
            get { return _DefectMapHash; }
            set { if (value != _DefectMapHash) { _DefectMapHash = value; OnPropertyChanged(); } }
        }

        private string _GPUStats = "";
        public string GPUStats
        {
            get { return _GPUStats; }
            set { if (value != _GPUStats) { _GPUStats = value; OnPropertyChanged(); } }
        }

        private Movie _DisplayedMovie = null;
        public Movie DisplayedMovie
        {
            get { return _DisplayedMovie; }
            set
            {
                if (value != _DisplayedMovie)
                {
                    _DisplayedMovie = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _OverviewPlotHighlightID = -1;
        public int OverviewPlotHighlightID
        {
            get { return _OverviewPlotHighlightID; }
            set { if (value != _OverviewPlotHighlightID) { _OverviewPlotHighlightID = value; OnPropertyChanged(); } }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsSshSettings : WarpBase
    {
        private string _iP;
        [WarpSerializable]
        [JsonProperty]
        public string IP
        {
            get { return _iP; }
            set
            {
                if (value != _iP)
                {
                    var s = value.Split(':').ToList();
                    if (s.Count > 1)
                    {
                        int p;
                        Port = int.TryParse(s.Last(), out p) ? p : 22;
                        s.RemoveAt(s.Count - 1);
                        Server = string.Join(":", s);
                    } else { 
                        Server = value; 
                    }
                    _iP = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _Server;
        [WarpSerializable]
        [JsonProperty]
        public string Server
        {
            get { return _Server; }
            set { 
                if (value != _Server)
                {
                    _Server = value;
                    OnPropertyChanged();
                } 
            }
        }

        private int _Port = 22;
        [WarpSerializable]
        [JsonProperty]
        public int Port
        {
            get { return _Port; }
            set
            {
                if (value != _Port)
                {
                    _Port = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _Username;
        [WarpSerializable]
        [JsonProperty]
        public string Username
        {
            get { return _Username; }
            set
            {
                if (value != _Username)
                {
                    _Username = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _Password;
        [WarpSerializable]
        [JsonProperty]
        public string Password
        {
            get { return _Password; }
            set
            {
                if (value != _Password)
                {
                    _Password = value;
                    OnPropertyChanged();
                }
            }
        }

        private string linuxPath;
        [WarpSerializable]
        [JsonProperty]
        public string LinuxPath
        {
            get => linuxPath;
            set
            {
                if (value != linuxPath)
                {
                    if (value != null)
                    {
                        linuxPath = value.TrimEnd('/');
                    }
                    OnPropertyChanged();
                }
            }
        }

        public PrivateKeyFile SshKeyObject { get; set; }
        private string _SshKey;
        [WarpSerializable]
        [JsonProperty]
        public string SshKey
        {
            get { return _SshKey; }
            set
            {
                if (value != _SshKey && File.Exists(value))
                {
                    try
                    {
                        SshKeyObject = new PrivateKeyFile(value);
                        _SshKey = value;
                        OnPropertyChanged();
                    }
                    catch (Exception e)
                    {
                        //do nothing
                    }
                }
            }
        }

        private bool _linuxServerOk;
        [WarpSerializable]
        [JsonProperty]
        public bool LinuxServerOk
        {
            get => _linuxServerOk;
            set
            {
                if (value != _linuxServerOk)
                {
                    _linuxServerOk = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _aretomoVersion = 1;
        [WarpSerializable]
        [JsonProperty]
        public int AretomoVersion
        {
            get => _aretomoVersion;
            private set
            {
                if (value != _aretomoVersion) { _aretomoVersion = value; OnPropertyChanged(); }
            }
        }

        private string _topazEnv;
        [WarpSerializable]
        [JsonProperty]
        public string TopazEnv
        {
            get { return _topazEnv; }
            set { if (value != _topazEnv) { _topazEnv = value; OnPropertyChanged(); } }
        }

        private bool _IsTestingLinuxServer = false;
        [WarpSerializable]
        [JsonProperty]
        public bool IsTestingLinuxServer
        {
            get { return _IsTestingLinuxServer; }
            set { if (value != _IsTestingLinuxServer) { _IsTestingLinuxServer = value; OnPropertyChanged(); } }
        }

        private SemaphoreSlim TestLinuxServerSemaphore = new SemaphoreSlim(1);
        private CancellationTokenSource TestLinuxServerConnectionToken = new CancellationTokenSource();
        public async Task TestLinuxServerConnection(CancellationToken token, string importFolder = null)
        {
            TestLinuxServerConnectionToken.Cancel();
            await TestLinuxServerSemaphore.WaitAsync();
            Console.WriteLine("Testing linux server tomo");
            IsTestingLinuxServer = true;
            TestLinuxServerConnectionToken = new CancellationTokenSource();
            var wortkask = Task.Run(() => { TestLinuxServerConnectionWork(TestLinuxServerConnectionToken.Token, importFolder); });
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    TestLinuxServerConnectionToken.Cancel();
                    break;
                }
                if (wortkask.IsCompleted)
                {
                    break;
                }
                await Task.Delay(200);
            }
            if (TestLinuxServerConnectionToken.IsCancellationRequested)
            {
                Console.WriteLine("Testing linux server tomo cancelled");
            }
            TestLinuxServerSemaphore.Release();
            IsTestingLinuxServer = false;
        }

        private void TestLinuxServerConnectionWork(CancellationToken token, string importFolder = null)
        {
            
            if (Server == "" || Username == "" || SshKeyObject == null || importFolder == null)
            {
                LinuxServerOk = false;
                return;
            }

            if (!Directory.Exists(importFolder))
            {
                LinuxServerOk = false;
                return;
            }

            var sshsettings = new OptionsSshSettings { IP = Server, Username = Username, Port = Port, SshKeyObject = SshKeyObject };

            if (GuessLinuxDirectory.LinuxDirectoryIsCorrect(sshsettings, importFolder, LinuxPath, token, DateTime.Now.ToString(".yyyyMMddHHmmss")+"tomo"))
            {
                LinuxServerOk = true;
            }
            else
            {
                LinuxServerOk = false;
            }

            FindAretomoVersion(token);
        }

        public void FindAretomoVersion(CancellationToken token)
        {
            Console.WriteLine("Testing linux server tomo");
            if (IP == "" || Username == "" || SshKeyObject == null || LinuxPath == "")
            {
                LinuxServerOk = false;
                Auto = false;
                return;
            }

            try
            {
                using (var sshclient = new SshClient(Server, Port, Username, SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        if (LinuxServerOk)
                        {
                            stream.WriteLine("echo ''; aretomo --version;echo DONE");
                            while (stream.CanRead)
                            {
                                if (token.IsCancellationRequested) break;
                                var line = stream.ReadLine();
                                Console.WriteLine(line);
                                if (line.StartsWith("AreTomo "))
                                {
                                    AretomoVersion = 1;
                                    break;
                                }
                                if (line.StartsWith("AreTomo2"))
                                {
                                    AretomoVersion = 2;
                                    break;
                                }
                                if (line == "DONE")
                                {
                                    LinuxServerOk = false;
                                    break;
                                }
                            }
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                LinuxServerOk = false;
                Auto = false;
            }
        }


        private SemaphoreSlim GuessLinuxServerMountpointSemaphore = new SemaphoreSlim(1);
        private CancellationTokenSource GuessLinuxServerMountpointToken = new CancellationTokenSource();

        public async Task GuessLinuxServerMountpoint(CancellationToken token, string importFolder = null)
        {
            GuessLinuxServerMountpointToken.Cancel();
            await GuessLinuxServerMountpointSemaphore.WaitAsync();
            IsTestingLinuxServer = true;
            GuessLinuxServerMountpointToken = new CancellationTokenSource();
            var wortkask = Task.Run(() => { GuessLinuxServerMountpointWork(GuessLinuxServerMountpointToken.Token, importFolder); });
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    GuessLinuxServerMountpointToken.Cancel();
                    break;
                }
                if (wortkask.IsCompleted)
                {
                    break;
                }
                await Task.Delay(200);
            }

            GuessLinuxServerMountpointSemaphore.Release();
            IsTestingLinuxServer = false;
        }
        private void GuessLinuxServerMountpointWork(CancellationToken token, string importFolder = null)
        {
            Console.WriteLine("Guess linux server tomo");
            if (Server == "" || Username == "" || SshKeyObject == null || importFolder == null)
            {
                return;
            }

            if (!Directory.Exists(importFolder))
            {
                return;
            }

            var sshsettings = new OptionsSshSettings { IP = Server, Username = Username, Port = Port, SshKeyObject = SshKeyObject };

            string guessedpath = default(string);
            guessedpath = GuessLinuxDirectory.GuessLinuxDirectoryFromPath(importFolder, sshsettings, token);
            Console.WriteLine($"Guessed path TOMO: {guessedpath}");

            if (guessedpath != null)
            {
                LinuxServerOk = true;
                LinuxPath = guessedpath;
            }
        }

        public CheckBox[] CheckboxesSshGPUS = new CheckBox[0];

        public void ListGPUsSsh()
        {
            Console.WriteLine("Listing server GPUs");
            var GPUlist = new List<int>();
            if (IP == "" || Username == "" || SshKeyObject == null || LinuxPath == "")
            {
                GPUlist.Add(0);
                return;
            }
            try
            {
                using (var sshclient = new SshClient(Server, Port, Username, SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        var cmd = "echo ''; nvidia-smi -L; echo DONE";
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
                            Console.WriteLine(line);
                            if (line.StartsWith("GPU"))
                            {
                                try
                                {
                                    Console.WriteLine("Found GPU {0}",line.Split(' ')[1].Trim(':'));
                                    GPUlist.Add(int.Parse(line.Split(' ')[1].Trim(':')));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                            if (line == "DONE")
                            {
                                break;
                            }
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                LinuxServerOk = false;
                Auto = false;
            }
            if (GPUlist.Count < 1) { GPUlist.Add(0); }

            Console.WriteLine("Counted {0} GPUs in ssh server",GPUlist.Count);
            CheckboxesSshGPUS = Helper.ArrayOfFunction(i =>
            {
                CheckBox NewCheckBox = new CheckBox
                {
                    //Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(10, 0, 10, 0),
                    IsChecked = true,
                    Opacity = 1,
                    Focusable = false
                };

                return NewCheckBox;
            }, GPUlist.Count);

        }

        private bool _Auto;
        [WarpSerializable]
        [JsonProperty]
        public bool Auto
        {
            get { return _Auto; }
            set { if (value != _Auto) { _Auto = value; OnPropertyChanged(); } }
        }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OptionsAretomoSettings : WarpBase
        {

        private int _GlobalBinning;
        [WarpSerializable]
        [JsonProperty]
        public int GlobalBinning
        {
            get { return _GlobalBinning; }
            set { if (value != _GlobalBinning) { _GlobalBinning = value; OnPropertyChanged(); } }
        }

        private float _GlobalDosePerTilt;
        [WarpSerializable]
        [JsonProperty]
        public float GlobalDosePerTilt
        {
            get { return _GlobalDosePerTilt; }
            set { if (value != _GlobalDosePerTilt) { _GlobalDosePerTilt = value; OnPropertyChanged(); } }
        }

        private float _GlobalTiltAxis;
        [WarpSerializable]
        [JsonProperty]
        public float GlobalTiltAxis
        {
            get { return _GlobalTiltAxis; }
            set { if (value != _GlobalTiltAxis) { _GlobalTiltAxis = value; OnPropertyChanged(); } }
        }

        private float _GlobalCs;
        [WarpSerializable]
        [JsonProperty]
        public float GlobalCs
        {
            get { return _GlobalCs; }
            set { if (value != _GlobalCs) { _GlobalCs = value; OnPropertyChanged(); } }
        }

        private bool _GlobalFlipVol;
        [WarpSerializable]
        [JsonProperty]
        public bool GlobalFlipVol
        {
            get { return _GlobalFlipVol; }
            set { if (value != _GlobalFlipVol) { _GlobalFlipVol = value; OnPropertyChanged(); } }
        }

        private bool _GlobalSkipReconstruction;
        [WarpSerializable]
        [JsonProperty]
        public bool GlobalSkipReconstruction
        {
            get { return _GlobalSkipReconstruction; }
            set { if (value != _GlobalSkipReconstruction) { _GlobalSkipReconstruction = value; OnPropertyChanged(); } }
        }

        private bool _GlobalUseWbp;
        [WarpSerializable]
        [JsonProperty]
        public bool GlobalUseWbp
        {
            get { return _GlobalUseWbp; }
            set { if (value != _GlobalUseWbp) { _GlobalUseWbp = value; OnPropertyChanged(); } }
        }

        private float _GlobalDarkTol;
        [WarpSerializable]
        [JsonProperty]
        public float GlobalDarkTol
        {
            get { return _GlobalDarkTol; }
            set { if (value != _GlobalDarkTol) { _GlobalDarkTol = value; OnPropertyChanged(); } }
        }

        private int _GlobalOutImod;
        [WarpSerializable]
        [JsonProperty]
        public int GlobalOutImod
        {
            get { return _GlobalOutImod; }
            set { if (value != _GlobalOutImod) { _GlobalOutImod = value; OnPropertyChanged(); } }
        }

        private int _GlobalVolZ;
        [WarpSerializable]
        [JsonProperty]
        public int GlobalVolZ
        {
            get { return _GlobalVolZ; }
            set { if (value != _GlobalVolZ) { _GlobalVolZ = value; OnPropertyChanged(); } }
        }

        private int _GlobalTiltCorInt;
        [WarpSerializable]
        [JsonProperty]
        public int GlobalTiltCorInt
        {
            get { return _GlobalTiltCorInt; }
            set { if (value != _GlobalTiltCorInt) { _GlobalTiltCorInt = value; OnPropertyChanged(); } }
        }

        private int _GlobalTiltCorAng;
        [WarpSerializable]
        [JsonProperty]
        public int GlobalTiltCorAng
        {
            get { return _GlobalTiltCorAng; }
            set { if (value != _GlobalTiltCorAng) { _GlobalTiltCorAng = value; OnPropertyChanged(); } }
        }

        private int _GlobalAlignZ;
        [WarpSerializable]
        [JsonProperty]
        public int GlobalAlignZ
        {
            get { return _GlobalAlignZ; }
            set { if (value != _GlobalAlignZ) { _GlobalAlignZ = value; OnPropertyChanged(); } }
        }

        private int _PatchesToProcess;
        [WarpSerializable]
        [JsonProperty]
        public int PatchesToProcess
        {
            get { return _PatchesToProcess; }
            set { if (value != _PatchesToProcess) { _PatchesToProcess = value; OnPropertyChanged(); } }
        }

        private int _PatchesProcessed;
        public int PatchesProcessed
        {
            get { return _PatchesProcessed; }
            set { if (value != _PatchesProcessed) { _PatchesProcessed = value; OnPropertyChanged(); } }
        }


        private int _GlobalSamplePreTilt;
        [WarpSerializable]
        [JsonProperty]
        public int GlobalSamplePreTilt
        {
            get { return _GlobalSamplePreTilt; }
            set { if (value != _GlobalSamplePreTilt) { _GlobalSamplePreTilt = value; OnPropertyChanged(); } }
        }

        private int _NPatchesX = 5;
        [WarpSerializable]
        [JsonProperty]
        public int NPatchesX
        {
            get { return _NPatchesX; }
            set { if (value != _NPatchesX) { _NPatchesX = value; OnPropertyChanged(); } }
        }

        private int _NPatchesY = 4;
        [WarpSerializable]
        [JsonProperty]
        public int NPatchesY
        {
            get { return _NPatchesY; }
            set { if (value != _NPatchesY) { _NPatchesY = value; OnPropertyChanged(); } }
        }

        private bool _IncludeAll;
        [WarpSerializable]
        [JsonProperty]
        public bool IncludeAll
        {
            get { return _IncludeAll; }
            set { if (value != _IncludeAll) { _IncludeAll = value; OnPropertyChanged(); } }
        }

        public OptionsAretomoSettings ShallowCopy()
        {
            return (OptionsAretomoSettings)this.MemberwiseClone();
        }
    }

    public class OptionsTiltSeries : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangedEventHandler TiltSeriesInitialized;


        public List<TiltSeriesViewModel> TiltSeriesProcessQueue = new List<TiltSeriesViewModel>();

        private Dictionary<int,List<Task>> RunningProcesses = new Dictionary<int, List<Task>>();

        public ObservableCollection<TiltSeriesViewModel> TiltSeriesList { get; set; }

        public OptionsSshSettings ConnectionSettings { get; set; }

        private OptionsAretomoSettings aretomoSettings;
        public OptionsAretomoSettings AretomoSettings
        {
            get => aretomoSettings;
            set
            {
                if (value != aretomoSettings)
                {
                    aretomoSettings = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AretomoSettings)));
                }
            }
        }

        public OptionsAretomoSettings LocalAretomoSettings { get; set; }

        private string mdocFilesDirectory;
        public string MdocFilesDirectory
        {
            get => mdocFilesDirectory;
            set
            {
                mdocFilesDirectory = value;
                if (Directory.Exists(value))
                {
                    if (mdocFilesWatcher != null)
                    {
                        try
                        {
                            mdocFilesWatcher.Dispose();
                        } catch { }
                    }
                    mdocFilesWatcher = new FileSystemWatcher(value, "*.mdoc");
                    mdocFilesWatcher.EnableRaisingEvents = true;
                    mdocFilesWatcher.NotifyFilter = NotifyFilters.Attributes
                             | NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size;
                    mdocFilesWatcher.Created += OnMdocFileCreated;
                    mdocFilesWatcher.IncludeSubdirectories = false;
                    mdocFilesWatcher.EnableRaisingEvents = true;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MdocFilesDirectory)));
                }
            }
        }

        FileSystemWatcher mdocFilesWatcher;

        public decimal RuntimePixelSize { get; set; }

        public decimal PixelSpacingUnbinned { get; set; }

        public OptionsTiltSeries(OptionsSshSettings connectionSettings, OptionsAretomoSettings aretomoSettings)
        {
            ConnectionSettings = connectionSettings;
            AretomoSettings = aretomoSettings;
            TiltSeriesList = new ObservableCollection<TiltSeriesViewModel>();
            TiltSeriesList.CollectionChanged += OnTiltSeriesListChanged;
            MdocFilesDirectory = "";
            PropertyChanged += OnPropertyChanged;
        }

        public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MdocFilesDirectory")
            {
                ListAllMdocFiles();
            }
        }

        public void ListAllMdocFiles()
        {
            if (!Directory.Exists(MdocFilesDirectory)) return;
            foreach (var ts in TiltSeriesList)
            {
                ts.CancellationTokenSource.Cancel();
            }
            TiltSeriesList.Clear();
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new Action(() =>
           {
               foreach (var file in Directory.EnumerateFiles(MdocFilesDirectory, "*.mdoc").OrderBy(x => x))
               {
                   var ts = new TiltSeriesViewModel(file, ConnectionSettings, AretomoSettings, RuntimePixelSize, PixelSpacingUnbinned);
                   if (!ts.SuccessfullyReadXmlProperties) ts.CopyGlobalSettings();
                   TiltSeriesList.Add(ts);
               }
           }));
        }

        public void OnTiltSeriesListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= OnTiltSeriesPropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    try
                    {
                        var ts = (TiltSeriesViewModel)item;
                        item.PropertyChanged += OnTiltSeriesPropertyChanged;
                        Task.Run(() => ts.CheckFilesExist());
                    }
                    catch (InvalidCastException ex)
                    {
                        Console.WriteLine(ex.Message);
                        return;
                    }
                }
            }
        }

        public async void RunQueue()
        {
            if (TiltSeriesProcessQueue.Count < 1) return;
            if (ConnectionSettings.Auto)
            {
                List<string> enabledGPUsStr = new List<string>();
                Application.Current.Dispatcher.Invoke(() =>
               {
                   enabledGPUsStr = ConnectionSettings.CheckboxesSshGPUS.Where(x => x.IsChecked == true).Select(x => x.Content.ToString().Substring(4)).ToList();

               });
                var enabledGPUs = enabledGPUsStr.Select(x => int.Parse(x));
                int i = -1;
                Task t;
                TiltSeriesViewModel ts;
                lock (TiltSeriesProcessQueue)
                {
                    lock (RunningProcesses)
                    {
                        foreach (var id in enabledGPUs)
                        {
                            var tasks = new List<Task>();
                            if (RunningProcesses.TryGetValue(id, out tasks))
                            {
                                for (var j=0; j < tasks.Count; j++) //we remove any cancelled tasks
                                {
                                    if (tasks[j].IsCanceled) RunningProcesses[id].Remove(tasks[j]);
                                }

                                if (tasks.Count < 1) // 1 is the max number of processes that we allow per GPU
                                {
                                    i = id;
                                    break;
                                }
                            }
                            else
                            {
                                RunningProcesses.Add(id, new List<Task>());
                                i = id;
                                break;
                            }
                        }
                        if (i < 0) return; //no gpu is free, return



                        ts = TiltSeriesProcessQueue.First();
                        TiltSeriesProcessQueue.RemoveAt(0);
                        t = ts.TiltSeriesProcess(i);
                        Console.WriteLine("Queuing {0} on {1}", ts.DisplayName, i);
                        RunningProcesses[i].Add(t);
                    }
                }
                await t;
                lock (RunningProcesses)
                {
                    RunningProcesses[i].Remove(t);
                }
                ts.IsQueued = false;
            }
        }

        private readonly SemaphoreSlim ParsingMdocFileSync = new SemaphoreSlim(1);

        public async void OnMdocFileCreated(object sender, FileSystemEventArgs e)
        {
            await ParsingMdocFileSync.WaitAsync();
            var file = e.FullPath;
            var mdocfiles = new List<string>();
            foreach (var ts in TiltSeriesList)
            {
                mdocfiles.Add(ts.MdocFile);
            }

            if (!mdocfiles.Contains(file))
            {

                var ts = new TiltSeriesViewModel(file, ConnectionSettings, AretomoSettings, RuntimePixelSize, PixelSpacingUnbinned);
                ts.PixelSpacing = RuntimePixelSize;
                ts.PixelSpacingUnbinned = PixelSpacingUnbinned;
                while (!ts.IsInitialized)
                {
                    await Task.Delay(1000);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TiltSeriesList.Add(ts);
                });
            }
            ParsingMdocFileSync.Release();
        }

        public void OnTiltSeriesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var ts = (TiltSeriesViewModel)sender;
            if (e.PropertyName == "WarpStatus")
            {
                if (ts.Aretomo2PngStatus == jobStatus.Finished || ts.Aretomo2PngStatus == jobStatus.Failed) return;
                if (ts.AretomoStatus == jobStatus.Failed || ts.NewstackStatus == jobStatus.Failed) return;
                if (ts.WarpStatus == jobStatus.Finished)
                {
                    lock(TiltSeriesProcessQueue) {
                        if (!TiltSeriesProcessQueue.Contains(ts) && ts.IsNotProcessing)
                        {
                            TiltSeriesProcessQueue.Add(ts);
                            ts.IsQueued = true;
                        }
                    }
                }
            }
            else if (e.PropertyName == "IsInitialized")
            {
                TiltSeriesInitialized?.Invoke(this, new PropertyChangedEventArgs(ts.Name));
            }
        }
    }
}