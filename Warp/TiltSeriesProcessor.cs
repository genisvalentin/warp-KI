using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Warp.Tools;

namespace Warp
{
    public struct ProcessingFiles
    {
        public string TiltFile { get; set; }
        public string InputFiles { get; set; }
        public string OutputFile { get; set; }
        public string LogFile { get; set; }
        public string InStack { get; set; }
        public string OutTomo { get; set; }
        public string AngFile { get; set; }
        public string Projection { get; set; }
        public string ProjectionXZ { get; set; }
        public string OutPng { get; set; }
        public string OutPngXZ { get; set; }
        public string TiltFileLinux { get; set; }
        public string InputFilesLinux { get; set; }
        public string OutputFileLinux { get; set; }
        public string LogFileLinux { get; set; }
        public string InStackLinux { get; set; }
        public string OutTomoLinux { get; set; }
        public string AngFileLinux { get; set; }
        public string ProjectionLinux { get; set; }
        public string ProjectionXZLinux { get; set; }
        public string OutPngLinux { get; set; }
        public string OutPngXZLinux { get; set; }
        public string ImodDir { get; set; }

        public string AretomoDir { get; set; }
        public string AretomoSettingsXml { get; set; }
        public string AretomoOutXf { get; set; }

        public string AretomoOutXfLinux { get; set; }


        public string AretomoOutTlt { get; set; }


        public string AretomoOutSt { get; set; }


        public string AretomoOutStLinux { get; set; }


        public string AretomoOutStMov { get; set; }
        public string AretomoOutStMovLinux { get; set; }
        public string AretomoOutTomogramMov { get; set; }
        public string AretomoOutTomogramMovLinux { get; set; }
        public string CorrectedMdoc { get; set; }
        public string OutTomoDenoisedLinux { get; internal set; }
        public string OutTomoDenoised { get; internal set; }
    }

    public class TiltSeriesProcessor
    {
        public static ProcessingFiles ProcessingFiles(string mdoc_file, string linuxPath, int version=1)
        {
            var files = new ProcessingFiles();
            var baseDir = Path.GetDirectoryName(mdoc_file);
            var baseName = Path.GetFileNameWithoutExtension(mdoc_file);
            if (!Directory.Exists(Path.Combine(baseDir, "aretomo")))
            {
                Directory.CreateDirectory(Path.Combine(baseDir, "aretomo"));
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(baseDir, "aretomo", baseName));
            } catch (System.IO.IOException e)
            {
                LogToFile(e.ToString());
            }

            files.TiltFile = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_NewstackTiltFile.txt", baseName));
            files.InputFiles = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_NewstackInputFile.txt", baseName));
            files.InStack = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_Newstack.mrc", baseName));
            files.LogFile = Path.Combine(baseDir, "aretomo", baseName, baseName + ".log");
            files.OutTomo = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}.mrc", baseName));
            files.OutTomoDenoised = Path.Combine(baseDir, "topaz", String.Format("{0}.mrc", baseName));
            files.AngFile = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_angfile.aln", baseName));
            files.Projection = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_projXY.mrc", baseName));
            files.ProjectionXZ = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_projXZ.mrc", baseName));
            files.OutPng = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_projXY.png", baseName));
            files.OutPngXZ = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_projXZ.png", baseName));
            files.ImodDir = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_Imod", baseName));
            files.AretomoDir = Path.Combine(baseDir, "aretomo", baseName);
            files.AretomoSettingsXml = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}.xml", baseName));
            files.AretomoSettingsXml = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}.xml", baseName));
            files.AretomoOutXf = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}.xf", baseName));
            files.AretomoOutTlt = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}.tlt", baseName));
            files.AretomoOutSt = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}.st", baseName));
            files.CorrectedMdoc = Path.Combine(baseDir, "aretomo", baseName + ".mdoc");
            files.AretomoOutStMov = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_st.mp4", baseName));
            files.AretomoOutTomogramMov = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}.mp4", baseName));

            files.TiltFileLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_NewstackTiltFile.txt", baseName));
            files.InputFilesLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_NewstackInputFile.txt", baseName));
            files.InStackLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_Newstack.mrc", baseName));
            files.LogFileLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}.log", baseName));
            files.OutTomoLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}.mrc", baseName));
            files.OutTomoDenoisedLinux = String.Join("/", linuxPath, "topaz", String.Format("{0}.mrc", baseName));
            files.AngFileLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_angfile.aln", baseName));
            files.ProjectionLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_projXY.mrc", baseName));
            files.ProjectionXZLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_projXZ.mrc", baseName));
            files.OutPngLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_projXY.png", baseName));
            files.OutPngXZLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_projXZ.png", baseName));
            files.AretomoOutStLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}.st", baseName));
            files.AretomoOutStMovLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_st.mp4", baseName));
            files.AretomoOutTomogramMovLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}.mp4", baseName));
            files.AretomoOutXfLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}.xf", baseName));

            if (version == 2)
            {
                files.AretomoOutXfLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_Newstack_st.xf", baseName));
                files.AretomoOutStLinux = String.Join("/", linuxPath, "aretomo", baseName, String.Format("{0}_Newstack_st.mrc", baseName));
                files.AretomoOutXf = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_Newstack_st.xf", baseName));
                files.AretomoOutTlt = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_Newstack_st.tlt", baseName));
                files.AretomoOutSt = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_Newstack_st.mrc", baseName));
                files.ImodDir = Path.Combine(baseDir, "aretomo", baseName, String.Format("{0}_Newstack_Imod", baseName));
            }
            return files;
        }

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

        public static jobStatus RunNewStack(TiltSeriesViewModel ts, OptionsSshSettings settings, bool IncludeAll)
        {
            if (!File.Exists(ts.MdocFile)) { return jobStatus.Failed; }

            var files = ProcessingFiles(ts.MdocFile, settings.LinuxPath, settings.AretomoVersion);

            string baseDir = Path.GetDirectoryName(ts.MdocFile);
            string baseName = Path.GetFileNameWithoutExtension(ts.MdocFile);
            if (baseDir == null || baseName == null) { return jobStatus.Failed; }

            if (!WriteNewstackInpFile(ts, files.InputFiles, settings, IncludeAll)) { return jobStatus.Failed; }
            if (!WriteNewstackTiltFile(ts, files.TiltFile, IncludeAll)) { return jobStatus.Failed; }

            //var cmd = "source /etc/profile.d/IMOD-linux.sh";
            var cmd = String.Format("newstack -tilt '{0}' -format mrc -output '{1}' -fileinlist '{2}' ; echo DONE",
                files.TiltFileLinux, files.InStackLinux, files.InputFilesLinux);
            LogToFile(cmd);

            try
            {
                using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
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
            catch (Exception ex)
            {
                LogToFile($"Failed to run ssh command: {cmd}");
                LogToFile(ex.Message);
                return jobStatus.Failed;
            }

            LogToFile("Run ssh command");
            LogToFile($"Looking for file {files.InStack}, or {files.InStackLinux}");
            if (LinuxFileExists(settings, files.InStackLinux))
            {
                int counter = 0;
                while (!File.Exists(files.InStack)) {
                    counter++;
                    Thread.Sleep(1000);
                    if (counter == 10) return jobStatus.Failed;
                }
            } else
            {
                return jobStatus.Failed;
            }

            return jobStatus.Finished;
        }

        public static bool LinuxFileExists(OptionsSshSettings settings, string LookupFile)
        {
            bool exists = false;
            LookupFile = LookupFile.Trim().TrimEnd('/');
            string cmd = $"echo ''; if [ -f {LookupFile} ]; then echo YES; fi; echo DONE";
            LogToFile("Checking file exists by ssh...");
            LogToFile(cmd);
            try
            {
                using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
                            if (line == "YES") exists=true;
                            if (line == "DONE") break;
                        }
                    }
                    sshclient.Disconnect();
                }
            } catch (Exception ex) { 
                LogToFile(ex.Message);
            }
            return exists;
        }

        public static jobStatus RunAretomo(TiltSeriesViewModel ts, OptionsSshSettings settings, OptionsAretomoSettings aretomoSettings, int gpu, CancellationToken Token)
        {
            if (!File.Exists(ts.MdocFile)) { return jobStatus.Failed; }

            string baseDir = Path.GetDirectoryName(ts.MdocFile);
            string baseName = Path.GetFileNameWithoutExtension(ts.MdocFile);
            //if (baseDir == null || baseName == null) { return jobStatus.Failed; }

            var files = ProcessingFiles(ts.MdocFile, settings.LinuxPath, settings.AretomoVersion);

            if (!WriteAretomoAlnFile(ts, files.AngFile, aretomoSettings)) { return jobStatus.Failed; }

            string defocus = ts.TiltImages.First().Value.TargetDefocus.ToString();
            string binning = aretomoSettings.GlobalBinning > 0 ? aretomoSettings.GlobalBinning.ToString() : "4";
            string cs = aretomoSettings.GlobalCs > 0 ? aretomoSettings.GlobalCs.ToString() : "0";
            float tiltAxis = aretomoSettings.GlobalTiltAxis > 0 ? aretomoSettings.GlobalTiltAxis : ts.TiltImages.First().Value.RotationAngle;
            string flipvol = aretomoSettings.GlobalFlipVol ? "1" : "0";
            string outImod = aretomoSettings.GlobalOutImod.ToString();
            string volz;
            string wbp = aretomoSettings.GlobalUseWbp ? "1" : "0";
            string alignz = aretomoSettings.GlobalAlignZ > 0 ? aretomoSettings.GlobalAlignZ.ToString() : "0";
            if (aretomoSettings.GlobalSkipReconstruction)
            {
                volz = "0";
            } else
            {
                volz = aretomoSettings.GlobalVolZ > 0 ? aretomoSettings.GlobalVolZ.ToString() : "1200";
            }
            string tiltcor = aretomoSettings.GlobalTiltCorInt*aretomoSettings.GlobalTiltCorAng < 1 ? aretomoSettings.GlobalTiltCorInt.ToString() : $"{aretomoSettings.GlobalTiltCorInt} {aretomoSettings.GlobalTiltCorAng}";
            string cmd = String.Format("aretomo -InMrc '{0}' -OutMrc '{1}' -Kv '{2}' -PixSize '{3}' -AngFile '{4}' -Cs '{5}' -Defoc '{6}' -FlipVol {7} -OutBin {8} -OutImod {9} -VolZ {10} -Wbp {11} -AlignZ {12} -TiltCor {13}",
                files.InStackLinux, files.OutTomoLinux, ts.Voltage.ToString(), ts.PixelSpacing.ToString(), files.AngFileLinux, cs, defocus, flipvol, binning, outImod, volz, wbp, alignz, tiltcor);

            if ((aretomoSettings.NPatchesY * aretomoSettings.NPatchesY) > 0) cmd += $" -Patch {aretomoSettings.NPatchesX} {aretomoSettings.NPatchesY}";

            if (tiltAxis > 0)
            {
                cmd = cmd + String.Format(" -TiltAxis '{0}'",tiltAxis.ToString());
            }
            if (aretomoSettings.GlobalDarkTol > 0)
            {
                cmd = cmd + String.Format(" -DarkTol '{0}'", aretomoSettings.GlobalDarkTol.ToString());
            }
            cmd += $" -Gpu {gpu}";
            cmd += ";echo DONE";

            LogToFile(cmd);
            ts.DarkImagesRemoved = new ObservableCollection<float>();
            try
            {
                using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
                            using (StreamWriter file = new StreamWriter(files.LogFile, true))
                            {
                                file.WriteLine(line);
                            }
                            if (line.StartsWith("# Number of patches:"))
                            {
                                try
                                {
                                    var p1 = line.Split(':')[1].Trim().Split(' ').ToList().First().Trim();
                                    var p2 = line.Split(':')[1].Trim().Split(' ').ToList().Last().Trim();
                                    LogToFile($"p1: {p1}, p2: {p2}");
                                    var totalPatches = int.Parse(p1) * int.Parse(p2);
                                    ts.AretomoHasToProcess = totalPatches;
                                } catch { }
                            } 
                            else if (line.StartsWith("Align patch at"))
                            {
                                LogToFile(line);
                                try
                                {
                                    int i1 = line.IndexOf("),") + 2;
                                    int i2 = line.IndexOf("patches left");
                                    var parsedInt = line.Substring(i1, i2-i1).Trim();
                                    LogToFile($"Patches left: {parsedInt}");
                                    ts.AretomoHasProcessed = ts.AretomoHasToProcess - int.Parse(parsedInt);
                                }
                                catch { }
                            } 
                            else if (line.StartsWith("Tilt offset"))
                            {
                                var offset = line.Split(',')[0].Split(':')[1].Trim();
                                try
                                {
                                    LogToFile($"Aretomo determined tilt offset = {offset}");
                                    ts.AretomoTiltOffset = float.Parse(offset, CultureInfo.InvariantCulture);
                                } catch (Exception ex){
                                    LogToFile($"Failed to read tilt offset. {offset}\n{ex.Message}");
                                }
                            } 
                            else if (line.StartsWith("New tilt axis:"))
                            {
                                ts.AretomoRefinedTiltAxis = line.Split(':')[1].Trim();
                                LogToFile($"Aretomo refined tilt axis: {ts.AretomoRefinedTiltAxis}");
                            }
                            else if (line.StartsWith("Remove image at"))
                            {
                                try
                                {
                                    int ideg = line.IndexOf("deg:")-16;
                                    string s = line.Substring(16, ideg).Trim();
                                    LogToFile($"Removing dark image {s}");
                                    float ang = float.Parse(s, CultureInfo.InvariantCulture);
                                    ts.DarkImagesRemoved.Add(ang);
                                } catch (Exception ex)
                                {
                                    LogToFile(ex.Message);
                                }
                            }
                            if (line == "DONE") break;
                            if (Token.IsCancellationRequested) return jobStatus.Failed;
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to run ssh command: {cmd}");
                LogToFile(ex.Message);
                return jobStatus.Failed;
            }


            if (!File.Exists(files.OutTomo)) return jobStatus.Failed;

            if (Directory.Exists(files.ImodDir))
            {
                foreach (var f in Directory.EnumerateFiles(files.ImodDir))
                {
                    try
                    {
                        var name = Path.GetFileName(f);
                        var newName = Path.Combine(files.AretomoDir, name);
                        if (File.Exists(newName))
                        {
                            File.Delete(newName);
                        }
                        File.Move(f, newName);
                    }
                    catch (Exception e)
                    {
                        LogToFile($"Moving imod files for WARP failed: {e.Message}");
                    }
                }
            }

            return jobStatus.Finished;
        }

        public static int StackNImages(TiltSeriesViewModel ts, OptionsSshSettings settings, string file)
        {
            var files = ProcessingFiles(ts.MdocFile, settings.LinuxPath, settings.AretomoVersion);
            string NImages = "";
            int NImagesInt = 0;
            string cmd = $"source $HOME/.bashrc; source_eman2; e2iminfo.py {file}; echo DONE";
             
            try
            {
                using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
                            if (line.StartsWith(file)) {
                                NImages = line.Split(new string[] { " x " } , StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList().Last();
                                if (!int.TryParse(NImages, out NImagesInt)) { NImagesInt = 0; }
                                LogToFile($"NImages: {NImages}");
                            }
                            LogToFile(line);
                            if (line == "DONE") break;
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to run ssh command: {cmd}");
                LogToFile(ex.Message);
            }

            return NImagesInt;
        }

        public static jobStatus RunAretomo2Png(TiltSeriesViewModel ts, OptionsSshSettings settings, OptionsAretomoSettings AretomoSettings)
        {
            if (!File.Exists(ts.MdocFile)) { 
                LogToFile($"Aretomo2png failed: {ts.MdocFile} does not exist.");
                return jobStatus.Failed; 
            }

            string baseDir = Path.GetDirectoryName(ts.MdocFile);
            string baseName = Path.GetFileNameWithoutExtension(ts.MdocFile);
            if (baseDir == null || baseName == null) { 
                LogToFile($"Aretomo2png failed: {baseDir} or {baseName} is null.");
                return jobStatus.Failed; 
            }
            string LinuxPath = settings.LinuxPath.TrimEnd('/') + "/aretomo/" + baseName;

            var files = ProcessingFiles(ts.MdocFile, settings.LinuxPath, settings.AretomoVersion);

            var tomo = File.Exists(files.OutTomoDenoised) ? files.OutTomoDenoisedLinux : files.OutTomoLinux;
            if (!File.Exists(files.Projection)) { 
                LogToFile($"Aretomo2png failed: {files.Projection} does not exist.");
                return jobStatus.Failed; 
            }

            int centralSlice = AretomoSettings.GlobalVolZ / AretomoSettings.GlobalBinning / 2;
            string cmd = String.Format("mrc2tif -p -a 0,0 -z {0},{0} {1} {2}; ", centralSlice, tomo, files.OutPngLinux);
            cmd += String.Format("mrc2tif -p -a 0,0 {0} {1}; echo DONE", files.ProjectionXZLinux, files.OutPngXZLinux);

            //string cmd1 = String.Format("source $HOME/.bashrc;source_eman2;");
            //string cmd2 = String.Format("e2proc2d.py --process filter.lowpass.gauss:cutoff_freq=0.1 '{0}' '{1}';", files.ProjectionLinux, files.OutPngLinux);
            //string cmd3 = String.Format("e2proc2d.py --process filter.lowpass.gauss:cutoff_freq=0.1 '{0}' '{1}';", files.ProjectionXZLinux, files.OutPngXZLinux);
            //string cmd = cmd1 + cmd2 + cmd3 + " echo DONE";
            LogToFile(cmd);

            try
            {
                using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
                            LogToFile(line);
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
            catch (Exception ex)
            {
                LogToFile($"Failed to run ssh command: {cmd}");
                LogToFile(ex.Message);
                return jobStatus.Failed;
            }

            if (File.Exists(files.AretomoOutSt)) {
                    cmd = $"newstack -xform {files.AretomoOutXfLinux} -in {files.AretomoOutStLinux} -float 2 -fo mrc -mo 0 -ro {ts.AretomoRefinedTiltAxis} -out {LinuxPath}/{baseName}_aln_st.mrc; ";
                    cmd += $"mrc2tif -j -a 0,0 {LinuxPath}/{baseName}_aln_st.mrc {LinuxPath}/{baseName}_aln_st; ";
                    cmd += $"ffmpeg -y -i {LinuxPath}/{baseName}_aln_st.%3d.jpg -pix_fmt yuv420p -s 640x452 -filter_complex '[0]reverse[r]; [0][r] concat,loop = 0:40,setpts = N / 25 / TB' {files.AretomoOutStMovLinux}; rm {LinuxPath}/{baseName}_aln_st.*.jpg; "; // rm {LinuxPath}/{baseName}-*.jpeg; "; //Run ffmpeg
                    cmd += "echo DONE";
                    LogToFile(cmd);
                    try
                    {
                        using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                        {
                            sshclient.Connect();
                            using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                            {
                                stream.WriteLine(cmd);
                                while (stream.CanRead)
                                {
                                    var line = stream.ReadLine();
                                    LogToFile(line);
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
                    catch (Exception ex)
                    {
                        LogToFile($"Failed to run ssh command: {cmd}");
                        LogToFile(ex.Message);
                        return jobStatus.Failed;
                    }
            }

            if (File.Exists(files.OutTomoDenoised) || File.Exists(files.OutTomo)) {
                //var NImages = StackNImages(ts, settings, files.OutTomoLinux);
                //cmd = $"source $HOME/.bashrc;source_eman2;e2proc2d.py  --fixintscaling sane --process filter.lowpass.gauss:cutoff_freq=0.1 {files.OutTomoLinux} {LinuxPath}/e2_mrc_{baseName}.png --unstacking;"; //Generate png files
                int nslices = AretomoSettings.GlobalVolZ/AretomoSettings.GlobalBinning > 999 ? 4 : 3;
                cmd = $"mrc2tif -j -a 0,0 {tomo} {LinuxPath}/{baseName}; ";
                cmd += $"ffmpeg -y -i {LinuxPath}/{baseName}.%0{nslices}d.jpg -pix_fmt yuv420p -s 452x640 {files.AretomoOutTomogramMovLinux}; rm {LinuxPath}/{baseName}.*.jpg; "; //Run ffmpeg
                cmd += "echo DONE";
                LogToFile(cmd);
                try
                {
                    using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                    {
                        sshclient.Connect();
                        using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                        {
                            stream.WriteLine(cmd);
                            while (stream.CanRead)
                            {
                                var line = stream.ReadLine();
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
                catch (Exception ex)
                {
                    LogToFile($"Failed to run ssh command: {cmd}");
                    LogToFile(ex.Message);
                    return jobStatus.Failed;
                }
            }

            //Sometimes we need to wait some time until the outpng file is available
            //Maybe it's slower on network filesystems
            int c = 0;
            var OutPngSlice = Directory.EnumerateFiles(files.AretomoDir).Where(file => IsProjXY(file, ts.Name)).FirstOrDefault();
            while (!File.Exists(OutPngSlice))
            {
                Thread.Sleep(1000);
                OutPngSlice = Directory.EnumerateFiles(files.AretomoDir).Where(file => IsProjXY(file, ts.Name)).FirstOrDefault();
                if (c > 4)
                {
                    LogToFile($"Could not find aretomo2png outpng {OutPngSlice}");
                    return jobStatus.Failed;
                }
                c++;
            }

            File.Move(OutPngSlice, files.OutPng);
            return jobStatus.Finished;
        }

        static bool IsProjXY(string fileName, string tsName)
        {
            // Use a regular expression to check for a three-digit number in the file name
            Regex regex = new Regex(tsName+"_projXY.png."+@"\d+"+".png");
            return regex.IsMatch(fileName);
        }

        internal static jobStatus Denoise(string topazEnv, string linuxPath, string tomogram, string LogFile, int Device, OptionsSshSettings settings, CancellationToken Token, TiltSeriesViewModel ts)
        {
            var cmd = "source ~/.bashrc; ";
            cmd += $"conda activate {topazEnv}; ";
            cmd += $"cd {linuxPath}; ";
            cmd += $"topaz denoise3d -o topaz -d {Device} {tomogram}; ";
            cmd += "echo DONE";
            LogToFile(cmd);
            jobStatus result = jobStatus.Failed;
            try
            {
                using (var sshclient = new SshClient(settings.Server, settings.Port, settings.Username, settings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
                            LogToFile(line);
                            using (StreamWriter file = new StreamWriter(LogFile, true))
                            {
                                file.WriteLine(line);
                            }
                            if (line.Trim().EndsWith("DONE") && !line.Trim().StartsWith("hallberg") && !line.Trim().StartsWith("source"))
                            {
                                result = jobStatus.Finished;
                                break;
                            }
                            try
                            {
                                var fields = line.Trim().Split();
                                if (fields[2].EndsWith("%")) ts.TopazProgress = fields[2];
                            } catch { }
                            if (line.Trim().StartsWith("# loading"))
                            {
                                try
                                {
                                    var fields = line.Trim().Split();
                                    ts.TopazProgress = "Loading " + fields.Last();
                                }
                                catch { }
                            }
                            if (Token.IsCancellationRequested)
                            {
                                result = jobStatus.Failed;
                                break;
                            }
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to run ssh command: {cmd}");
                LogToFile(ex.Message);
                return jobStatus.Failed;
            }
            return result;
        }

        public static bool WriteNewstackTiltFile(TiltSeriesViewModel ts, string outfile, bool IncludeAll)
        {
            string[] tiltAngles;
            if (IncludeAll)
            {
                tiltAngles = ts.TiltImages.Select(image => image.Value.CorrectedTiltAngle.ToString()).ToArray();
            }
            else
            {
                tiltAngles = ts.TiltImages.Where(image => image.Value.Status == Controls.ProcessingStatus.Processed).Select(image => image.Value.CorrectedTiltAngle.ToString()).ToArray();
            }
            try
            {
                File.WriteAllLines(outfile, tiltAngles);
            }
            catch (Exception ex)
            {
                LogToFile("Could not write Newstack tilt file.");
                LogToFile(ex.Message);
                return false;
            }
            return true;
        }

        public static bool WriteNewstackInpFile(TiltSeriesViewModel ts, string outfile, OptionsSshSettings settings, bool IncludeAll)
        {
            string[] writeLines;
            int NImages;
            if (IncludeAll)
            {
                NImages = ts.TiltImages.Count;
                writeLines = ts.TiltImages.Select( image => {
                    var n = Path.GetFileNameWithoutExtension(image.Value.SubFramePath);
                    return String.Join("/", settings.LinuxPath, "average", n.Substring(0, n.Length - 9) + "fractions.mrc");
                }
                ).ToArray();
            }
            else
            {
                var InputImages = ts.TiltImages.Where(image => image.Value.Status == Controls.ProcessingStatus.Processed);
                NImages= InputImages.Count();
                writeLines = InputImages.Select( image => {
                    var n = Path.GetFileNameWithoutExtension(image.Value.SubFramePath);
                    return String.Join("/", settings.LinuxPath, "average", n.Substring(0, n.Length - 9) + "fractions.mrc");
                    }
                ).ToArray();
            }

            try
            {
                using (StreamWriter file = new StreamWriter(outfile))
                {
                    file.WriteLine(NImages.ToString());
                    foreach (string line in writeLines)
                    {
                        file.WriteLine(line);
                        file.WriteLine("0");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile("Could not write Newstack input file.");
                LogToFile(ex.Message);
                return false;
            }
            return true;
        }

        public static bool WriteAretomoAlnFile(TiltSeriesViewModel ts, string outfile, OptionsAretomoSettings settings)
        {
            float dosePerTilt = settings.GlobalDosePerTilt > 0 ? settings.GlobalDosePerTilt : ts.TiltImages.First().Value.ExposureDose;
            string[] writeLines;
            if (settings.IncludeAll)
            {
                ts.AretomoIncludedImages = new ObservableCollection<int> (ts.TiltImages.Select(image => image.Key));
                if (settings.GlobalDosePerTilt > 0 )
                {
                    writeLines = ts.TiltImages.Select(image => String.Format("{0} {1}", image.Value.CorrectedTiltAngle.ToString(), ((image.Value.Zvalue + 1) * dosePerTilt).ToString())).ToArray();
                }
                else if(settings.GlobalDosePerTilt == 0)
                {
                    writeLines = ts.TiltImages.Select(image => String.Format("{0} {1}", image.Value.CorrectedTiltAngle.ToString(), (image.Value.PriorRecordDose + image.Value.ExposureDose).ToString())).ToArray();
                }
                else
                {
                    writeLines = ts.TiltImages.Select(image => image.Value.CorrectedTiltAngle.ToString()).ToArray();
                }
            }
            else
            {
                var includedImages = ts.TiltImages.Where(image => image.Value.Status == Controls.ProcessingStatus.Processed);
                ts.AretomoIncludedImages = new ObservableCollection<int> (includedImages.Select(image => image.Key));
                if (settings.GlobalDosePerTilt > 0)
                {
                    writeLines = includedImages.Select(image => String.Format("{0} {1}", image.Value.CorrectedTiltAngle.ToString(), ((image.Value.Zvalue + 1) * dosePerTilt).ToString())).ToArray();
                } else if (settings.GlobalDosePerTilt == 0)
                {
                    writeLines = includedImages.Select(image => String.Format("{0} {1}", image.Value.CorrectedTiltAngle.ToString(), (image.Value.PriorRecordDose + image.Value.ExposureDose).ToString())).ToArray();
                } else
                {
                    writeLines = includedImages.Select(image => image.Value.CorrectedTiltAngle.ToString()).ToArray();
                }
            }
            try
            {
                File.WriteAllLines(outfile, writeLines);
            }
            catch (Exception ex)
            {
                LogToFile("Could not write Newstack aln file.");
                LogToFile(ex.Message);
                return false;
            }
            return true;
        }

        public static void ApplySamplePreTilt(TiltSeriesViewModel ts, OptionsAretomoSettings aretomoSettings)
        {
            foreach (var ti in ts.TiltImages)
            {
                ti.Value.CorrectedTiltAngle = ti.Value.TiltAngle + aretomoSettings.GlobalSamplePreTilt;
            }
        }

        public static void ApplyAretomoTiltCorrection(TiltSeriesViewModel ts, OptionsAretomoSettings aretomoSettings)
        {
            var newFile = Path.Combine(Path.GetDirectoryName(ts.MdocFile), "aretomo", ts.Name + ".mdoc");
            bool removed = false;
            List<string> buffer = new List<string>();
            List<string> newLines = new List<string>();
            ts.FinalImages.Clear();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(ts.MdocFile))
            {
                string SubFramePath = "";
                int zvalue = -1;
                while (!sr.EndOfStream)
                {
                    var Line = sr.ReadLine();
                    LogToFile(Line);
                    var data = Line.Split('=').Select(x => x.Trim()).ToList();

                    if (Line.StartsWith("[ZValue"))
                    {
                        if (!removed)
                        {
                            newLines.AddRange(buffer); 
                        }
                        if (!removed && zvalue > -1)
                        {
                            ts.FinalImages.Add(zvalue);
                        }
                        zvalue = int.Parse(data[1].Trim(']'));
                        buffer = new List<string>();
                        removed = false;
                    }

                    if (data[0].StartsWith("SubFramePath") && !aretomoSettings.IncludeAll) { 
                        SubFramePath = data[1];
                        var match = ts.TiltImages.Where(ti => ti.Value.SubFramePath == SubFramePath).ToList();
                        if (match.Count == 1)
                        {
                            TiltImage img = match.First().Value;
                            LogToFile($"Image {img.SubFramePath} status is {img.Status}");
                            if (aretomoSettings.GlobalDosePerTilt > 0)
                            {
                                float exposureDose = aretomoSettings.GlobalDosePerTilt > 0 ? aretomoSettings.GlobalDosePerTilt : img.ExposureDose;
                                float PriorRecordDose = aretomoSettings.GlobalDosePerTilt > 0 ? aretomoSettings.GlobalDosePerTilt * img.Zvalue : img.PriorRecordDose;
                                buffer.Add($"ExposureDose = {exposureDose}");
                                buffer.Add($"PriorRecordDose = {PriorRecordDose}");
                                img.UpdatedPriorRecordDose = PriorRecordDose;
                            } else
                            {
                                buffer.Add($"ExposureDose = {img.ExposureDose}");
                                buffer.Add($"PriorRecordDose = {img.PriorRecordDose}");
                                img.UpdatedPriorRecordDose = img.PriorRecordDose;
                            }

                            img.CorrectedTiltAngle = aretomoSettings.GlobalTiltCorInt == 1 ? img.CorrectedTiltAngle + ts.AretomoTiltOffset : img.CorrectedTiltAngle;
                            if (img.Status != Controls.ProcessingStatus.Processed)
                            {
                                removed = true;
                            }
                        }
                    }

                    if (data[0].StartsWith("TiltAngle")) {
                        try
                        {
                            float ang = float.Parse(data[1], CultureInfo.InvariantCulture);
                            ang += aretomoSettings.GlobalSamplePreTilt;
                            bool found = ts.DarkImagesRemoved.Any(num => Math.Abs(num - ang) < 0.1);
                            if (found)
                            {
                                LogToFile($"Dark image removed {ang.ToString()} from mdoc file");
                                removed = true;
                            };
                            if (aretomoSettings.GlobalTiltCorInt == 1 )
                            {
                                ang += ts.AretomoTiltOffset;
                            }
                            buffer.Add(String.Format("TiltAngle = {0}", ang.ToString("F2")));
                        }
                        catch
                        {
                            buffer.Add(Line);
                        }

                    } else if (data[0].StartsWith("ExposureDose") || data[0].StartsWith("PriorRecordDose"))
                    {
                        continue;
                    }
                    else if (data[0].StartsWith("PixelSpacing"))
                    {
                        buffer.Add($"PixelSpacing = {ts.PixelSpacingUnbinned}");
                    } else {
                        buffer.Add(Line);
                    }
                }
                if (!removed)
                {
                    newLines.AddRange(buffer);
                }
                if (!removed && zvalue > -1)
                {
                    ts.FinalImages.Add(zvalue);
                }
            }

            if (newLines.Count < 1) return;
            if (File.Exists(newFile))
            {
                while (ts.IsFileLocked(new FileInfo(newFile))) Thread.Sleep(500);
            }

            LogToFile($"Writing pre-tilt corrected mdoc file {newFile}");
            try
            {
                using (StreamWriter file = new StreamWriter(newFile))
                {
                    foreach (string line in newLines)
                    {
                        file.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile(ex.Message);
            }

        }

        public static void WriteTomostar(TiltSeriesViewModel ts, string XfPath, string _PathMovie, decimal _PixelSize)
        {
            
            bool _DontInvertTilts = false;
            string baseName = ts.DisplayName;
            List<MdocEntry> Entries = new List<MdocEntry>();
            var outpath = Path.Combine(_PathMovie, baseName + ".tomostar");
            LogToFile($"Writing {outpath}");
            ts.FinalImages.Select(i => ts.TiltImages[i]).ToList().ForEach(ti =>
           {
               MdocEntry NewEntry = new MdocEntry();
               NewEntry.ZValue = ti.Zvalue;
               NewEntry.TiltAngle = (float)Math.Round(ti.CorrectedTiltAngle,1);
               NewEntry.Name = ti.SubFramePath.Substring(ti.SubFramePath.LastIndexOf("\\") + 1);
               NewEntry.Time = DateTime.Parse(ti.DateTime);
               NewEntry.PriorRecordDose = ti.UpdatedPriorRecordDose;
               var dose = ts.OverrideGlobalSettings ? ts.LocalAretomoSettings.GlobalDosePerTilt : ts.GlobalAretomoSettings.GlobalDosePerTilt;
               NewEntry.Dose = dose > 0 ? dose : ti.ExposureDose;
               Entries.Add(NewEntry);
           });
            /*
            foreach (var ti in ts.TiltImages)
            {
                MdocEntry NewEntry = new MdocEntry();
                NewEntry.ZValue = ti.Value.Zvalue;
                NewEntry.TiltAngle = ti.Value.CorrectedTiltAngle;
                NewEntry.Dose = ti.Value.ExposureDose;
                NewEntry.Name = ti.Value.SubFramePath;
                NewEntry.Time = DateTime.Parse(ti.Value.DateTime);
                NewEntry.PriorRecordDose = ti.Value.UpdatedPriorRecordDose;
                Entries.Add(NewEntry);
            }*/
            LogToFile($"Writing {Entries.Count} images in tomostar.");

            List<MdocEntry> SortedTime = new List<MdocEntry>(Entries);
            SortedTime.Sort((a, b) => a.Time.CompareTo(b.Time));

            // Do running dose
            float Accumulated = 0;
            foreach (var entry in SortedTime)
            {
                if (entry.PriorRecordDose > 0)
                {
                    Accumulated = entry.Dose + entry.PriorRecordDose;
                }
                else
                {
                    Accumulated += entry.Dose;
                }
                entry.Dose = Accumulated;
            }


            // Sort entires by angle and time (accumulated dose)
            List<MdocEntry> SortedAngle = new List<MdocEntry>(Entries);
            SortedAngle.Sort((a, b) => a.TiltAngle.CompareTo(b.TiltAngle));
            // Sometimes, there will be 2 0-tilts at the beginning of plus and minus series. 
            // Sort them according to dose, considering in which order plus and minus were acquired
            float DoseMinus = SortedAngle.Take(SortedAngle.Count / 2).Select(v => v.Dose).Sum();
            float DosePlus = SortedAngle.Skip(SortedAngle.Count / 2).Take(SortedAngle.Count / 2).Select(v => v.Dose).Sum();
            int OrderCorrection = DoseMinus < DosePlus ? 1 : -1;
            SortedAngle.Sort((a, b) => a.TiltAngle.CompareTo(b.TiltAngle) != 0 ? a.TiltAngle.CompareTo(b.TiltAngle) : a.Dose.CompareTo(b.Dose) * OrderCorrection);


            LogToFile($"Parsing {XfPath}");
            using (TextReader Reader = new StreamReader(File.OpenRead(XfPath)))
            {
                string Line;
                for (int i = 0; i < SortedAngle.Count; i++)
                {
                    Line = Reader.ReadLine();
                    string[] Parts = Line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    float2 VecX = new float2(float.Parse(Parts[0], CultureInfo.InvariantCulture),
                                                float.Parse(Parts[2], CultureInfo.InvariantCulture));
                    float2 VecY = new float2(float.Parse(Parts[1], CultureInfo.InvariantCulture),
                                                float.Parse(Parts[3], CultureInfo.InvariantCulture));

                    Matrix3 Rotation = new Matrix3(VecX.X, VecX.Y, 0, VecY.X, VecY.Y, 0, 0, 0, 1);
                    float3 Euler = Matrix3.EulerFromMatrix(Rotation);

                    SortedAngle[i].AxisAngle = Euler.Z * Helper.ToDeg;

                    //SortedAngle[i].Shift += VecX * float.Parse(Parts[4], CultureInfo.InvariantCulture) + VecY * float.Parse(Parts[5], CultureInfo.InvariantCulture);
                    float3 Shift = new float3(-float.Parse(Parts[4], CultureInfo.InvariantCulture), -float.Parse(Parts[5], CultureInfo.InvariantCulture), 0);
                    Shift = Rotation.Transposed() * Shift;

                    SortedAngle[i].Shift += new float2(Shift);
                }
            }

            LogToFile($"Writing star table");
            Star Table = new Star(new[]
            {
                "wrpMovieName",
                "wrpAngleTilt",
                "wrpAxisAngle",
                "wrpAxisOffsetX",
                "wrpAxisOffsetY",
                "wrpDose"
            });

            for (int i = 0; i < SortedAngle.Count; i++)
            {
                Table.AddRow(new List<string>()
                {
                    SortedAngle[i].Name,
                    (SortedAngle[i].TiltAngle * (_DontInvertTilts ? 1 : -1)).ToString(CultureInfo.InvariantCulture),
                    SortedAngle[i].AxisAngle.ToString(CultureInfo.InvariantCulture),
                    (SortedAngle[i].Shift.X * (float)_PixelSize).ToString(CultureInfo.InvariantCulture),
                    (SortedAngle[i].Shift.Y * (float)_PixelSize).ToString(CultureInfo.InvariantCulture),
                    SortedAngle[i].Dose.ToString(CultureInfo.InvariantCulture)
                });
            }
            Table.Save(Path.Combine(_PathMovie, baseName + ".tomostar"));
        }
    }
}
