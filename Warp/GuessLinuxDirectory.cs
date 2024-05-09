using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Warp
{
    internal class GuessLinuxDirectory
    {
        public static bool IsMappedNetworkDrive(string driveLetter)
        {
            string hostname = null;
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DeviceID = '" + driveLetter + "'");
                foreach (ManagementObject drive in searcher.Get())
                {
                    if (drive["DriveType"].ToString() == "4")
                    {
                        return true;
                    }
                }
            }
            catch (ManagementException)
            {
            }
            return false;
        }

        public static string GetNetworkDriveHostname(string driveLetter)
        {
            string hostname = null;
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DeviceID = '" + driveLetter + "'");
                foreach (ManagementObject drive in searcher.Get())
                {
                    if (drive["DriveType"].ToString() == "4")
                    {
                        hostname = drive["ProviderName"].ToString();
                        break;
                    }
                }
            }
            catch (ManagementException e)
            {
                return (e.Message);
            }
            return hostname;
        }

        public static List<sharedresource> GetDriveShareName(string path)
        {
            //If it is a local drive
            string shareName = null;
            List<sharedresource> shares = new List<sharedresource>();
            try
            {
                //var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Share WHERE Path = '" + driveLetter + @"\\'");
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Share");
                foreach (ManagementObject drive in searcher.Get())
                {
                    if (path.StartsWith(drive["path"].ToString())) {
                        shares.Add(new sharedresource { name = drive["name"].ToString(), path = drive["path"].ToString() });
                    }
                    //shareName = drive["Name"].ToString();
                }
            }
            catch (ManagementException e)
            {
                return new List<sharedresource>();
            }
            return shares;
        }

        public static string GetHostname(string ipAddress)
        {
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
                var name = hostEntry.HostName;
                return name.Split('.')[0];
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return ipAddress;
            }
        }

        public static string GetHostnameOverSsh(string ipAddress, OptionsSshSettings sshsettings)
        {
            var cmd = $"echo '';echo START;host {ipAddress}; echo DONE";
            var host = default(string);

            bool parse = false;
            List<networkvolume> drives = new List<networkvolume>();
            try
            {
                using (var sshclient = new SshClient(sshsettings.IP, sshsettings.Port, sshsettings.Username, sshsettings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            var line = stream.ReadLine();
                            if (line == "START") { parse = true; continue; }
                            if (line == "DONE") break;
                            if (parse)
                            {
                                try
                                {
                                    host = line.Split(' ').Last().Split('.').First();
                                }
                                catch
                                {
                                    host = ipAddress;
                                }
                            }
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run ssh command: {cmd}");
                Console.WriteLine(ex.Message);
                return ipAddress;
            }
            return host;
        }

        public static string WindowsToLinux(string winpath, string linuxmountpoint)
        {
            return linuxmountpoint + winpath.Substring(2).Replace("\\", "/");
        }

        public static List<networkvolume> GetLinuxNetworkMounts(OptionsSshSettings sshsettings, CancellationToken token)
        {
            var cmd = "echo '';echo START;mount; echo DONE";

            bool parse = false;
            List<networkvolume> drives = new List<networkvolume>();
            try
            {
                using (var sshclient = new SshClient(sshsettings.IP, sshsettings.Port, sshsettings.Username, sshsettings.SshKeyObject))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
                            if (token.IsCancellationRequested)
                            {
                                Console.WriteLine("Cancellation requested");
                                break;
                            }
                            var line = stream.ReadLine();
                            if (line == "START") { parse = true; }
                            if (parse)
                            {
                                //Console.WriteLine(line);
                                //is nfs drive
                                if (line.Split(' ').Count() > 5)
                                {
                                    if (line.Split(' ')[4].StartsWith("nfs"))
                                    {
                                        try
                                        {
                                            var v = new networkvolume();
                                            var s = line.Split(' ');
                                            v.host = s[0].Split(':')[0];
                                            v.name = s[0].Split(':')[1].TrimStart('/');
                                            v.mountpoint = s[2];
                                            drives.Add(v);
                                        }
                                        catch { };
                                    }
                                    else if (line.Split(' ')[4].StartsWith("cifs"))
                                    {
                                        try
                                        {
                                            var v = new networkvolume();
                                            var s = line.Split(' ');
                                            v.host = s[0].Split('/')[2];
                                            v.name = s[0].Split('/')[3];
                                            v.mountpoint = s[2];
                                            drives.Add(v);
                                        }
                                        catch { };
                                    }
                                }
                            }
                            if (line == "DONE") break;
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run ssh command: {cmd}");
                Console.WriteLine(ex.Message);
                return new List<networkvolume> { };
            }
            foreach (var d in drives)
            {
                if (IsIPAddress(d.host))
                {
                    d.host = GetHostnameOverSsh(d.host, sshsettings);
                }
                else
                {
                    d.host = d.host.Split('.')[0];
                }
                Console.WriteLine($"host:{d.host} name:{d.name} mountpoint:{d.mountpoint}");
            }
            return drives;
        }

        public static bool IsIPAddress(string s)
        {
            string pattern = @"^([1-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(\.([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])){3}$";
            Regex regex = new Regex(pattern);
            bool isMatch = regex.IsMatch(s);

            if (isMatch)
            {
                return true;
            }
            return false;
        }

        public static string GuessLinuxDirectoryFromPath(string importFolder, OptionsSshSettings sshsettings, CancellationToken token)
        {
            if (importFolder.Length < 2) return null;
            string drive = importFolder.Substring(0,2);

            //var d = new networkvolume();
            List<sharedresource> shares = new List<sharedresource>();
            List<networkvolume> netvols = new List<networkvolume>();
            if (IsMappedNetworkDrive(drive))
            {
                var host = GetNetworkDriveHostname(drive);
                var netvol = new networkvolume
                {
                    host = host.Split('\\')[2],
                    name = host.Split('\\')[3],
                    winpath = drive
                };
                if (IsIPAddress(netvol.host))
                {
                    netvol.host = GetHostname(netvol.host);
                } else
                {
                    netvol.host = netvol.host.Split('.').First();
                }

                netvols.Add(netvol);
                //Console.WriteLine($"Network drive in: {d.host} {d.name}");
            }
            else
            {
                shares = GetDriveShareName(importFolder);
                foreach (var share in shares)
                {
                    netvols.Add(new networkvolume { host = Dns.GetHostName(), name = share.name, winpath=share.path.TrimEnd('\\')});
                }
                //d.host = Dns.GetHostName();
                //d.name = sharename;
            }

            var drives = GetLinuxNetworkMounts(sshsettings, token);
            if (token.IsCancellationRequested) return null;

            List<string> matches = new List<string>();
            foreach (var netvol in netvols)
            {
                foreach (var d in drives)
                {
                    if (d.host.ToLower() == netvol.host.ToLower() && d.name.ToLower() == netvol.name.ToLower()) {
                        var test = importFolder.Replace(netvol.winpath, d.mountpoint).Replace("\\", "/");
                        if (LinuxDirectoryIsCorrect(sshsettings, importFolder, test, token))
                        {
                            return test;
                        }
                    }
                }
            }

            return null;

        }

        public static bool LinuxDirectoryIsCorrect(OptionsSshSettings settings, string importFolder, string linuxImportFolder, CancellationToken token, string testfilename = "")
        {
            bool exists = false;
            if (importFolder == null || linuxImportFolder == null) { return false; }
            if (linuxImportFolder == "") { return false; }
            if (!Directory.Exists(importFolder)) { return false; }
            if (testfilename == "") {
                testfilename = DateTime.Now.ToString(".yyyyMMddHHmmss");
            }
            var fs = File.Create(Path.Combine(importFolder, testfilename));
            fs.Dispose();
            string LookupFile = linuxImportFolder.Trim().TrimEnd('/') + "/" + testfilename;
            string cmd = $"echo ''; if [ -f {LookupFile} ]; then echo YES; fi; echo DONE";
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
                            if (token.IsCancellationRequested) break;
                            var line = stream.ReadLine();
                            if (line == "YES") exists = true;
                            if (line == "DONE") break;
                        }
                    }
                    sshclient.Disconnect();
                }
            }
            catch (Exception ex) { }

            int counter = 0;
            while (true)
            {
                if (counter > 10) break;
                try
                {
                    File.Delete(Path.Combine(importFolder, testfilename));
                    break;
                }
                catch (IOException ex)
                {
                    Thread.Sleep(200);
                }
                counter++;
            }
            return exists;
        }

    }

    public class networkvolume
    {
        public string host;
        public string name;
        public string mountpoint;
        public string winpath;

        public bool Equals(networkvolume other)
        {
            if (this.host.ToLower() == other.host.ToLower() && this.name.ToLower() == other.name.ToLower()) return true;
            return false;
        }
    }

    public class sharedresource
    {
        public string name;
        public string path;
    }

}
