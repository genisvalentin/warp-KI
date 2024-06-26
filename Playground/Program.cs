﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Renci.SshNet;
using ScottPlot;

namespace Playground
{
    public class DictTest
    {

        Dictionary<string, List<string>> testdict;
        int[] groups;

        public DictTest()
        {
            testdict = new Dictionary<string, List<string>>();
            groups = new int[10000];
            for (int i = 0; i < 10000; i++)
            {
                testdict.Add($"movie{i}.tiff", new List<string> { "0", "0", "0", i.ToString() });
                groups[i] = i;
            }
        }

        [Benchmark]
        public void ForLoop()
        {
            int counter = 0;
            foreach (var mic in testdict)
            {
                mic.Value[3] = (groups[counter] + 1).ToString();
                counter++;
            }
        }

        [Benchmark]
        public void Linq()
        {
            testdict = testdict
            .Zip(groups, (kvp, value) => {
                kvp.Value[3] = value.ToString();
                return new KeyValuePair<string, List<string>>(kvp.Key, kvp.Value);
            }
            )
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public class Program
    {
        public static async Task<string[]> GetJobVolumesTest(string projectUid, string jobUid, string host, string license_id)
        {
            var pwd = cryosparcClient.Client.GetMongoDbPassword(license_id);
            var client = new MongoClient("mongodb://cryosparc_user:" + pwd + "@" + host + ":39001");
            var jobsdb = client.GetDatabase("meteor").GetCollection<BsonDocument>("jobs");
            var filter = new BsonDocument { { "project_uid", projectUid }, { "uid", jobUid } };
            var job = await jobsdb.Find(filter).FirstAsync();

            List<string> available_volumes = new List<string>();
            string output_result_groups_string = job.GetValue("output_result_groups").ToString().ToLower();
            try
            {
                cryosparcClient.CryosparcOutputResultGroup[] output_result_groups =
                    JsonConvert.DeserializeObject<cryosparcClient.CryosparcOutputResultGroup[]>(output_result_groups_string);
                foreach (var group in output_result_groups)
                {
                    if (group.type == "volume")
                    {
                        if (group.contains.First()["type"] == "volume.blob")
                        available_volumes.Add(group.name);
                    }
                }
                return (available_volumes.ToArray());
            }
            catch (Exception e)
            {
                return (new string[0]);
            }


        }

        public static async Task<string> GetLatestHeteroRefinementJobTest(string projectUid, string host, string license_id, int nvolumes = 0)
        {
            var pwd = cryosparcClient.Client.GetMongoDbPassword(license_id);

            var client = new MongoClient("mongodb://cryosparc_user:" + pwd + "@" + host + ":39001");
            var jobsdb = client.GetDatabase("meteor").GetCollection<BsonDocument>("jobs");
            var filter = new BsonDocument { { "project_uid", projectUid }, { "job_type", "hetero_refine" }, { "deleted", false } };
            var jobsCursor = await jobsdb.FindAsync(filter);
            var jobsList = await jobsCursor.ToListAsync();

            if (jobsList.Count < 1)
            {
                return ("");
            }

            List<string> uidList = new List<string>();
            foreach (var job in jobsList)
            {
                if (job.GetValue("status") != "completed" && job.GetValue("status") != "running" && job.GetValue("status") != "queued") continue;
                string id = job.GetValue("uid").ToString();
                var vols = await GetJobVolumesTest(projectUid, id, host, license_id);
                if (nvolumes > 0 && nvolumes != vols.Length) continue;
                uidList.Add(id);
            }
            if (uidList.Count < 1)
            {
                return ("");
            }

            uidList.Sort((a, b) => int.Parse(a.Substring(1)).CompareTo(int.Parse(b.Substring(1))));

            return (uidList.Last());
        }

        /*
        public static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<DictTest>();
            var t = GetJobVolumesTest("P6", "J61", "3dem-workstation", "56ff1e14-ae24-11ee-900e-5b1af855443b");
            var r = GetLatestHeteroRefinementJobTest("P6", "3dem-workstation", "56ff1e14-ae24-11ee-900e-5b1af855443b", 5);
            t.Wait();
            foreach (var s in t.Result)
            {
                Console.WriteLine(s);
            }
            r.Wait();
            Console.WriteLine(r.Result);
            Console.ReadLine();
        }*/

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

        public static string GetDriveShareName(string driveLetter)
        {
            //If it is a local drive
            string shareName = null;
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Share WHERE Path = '" + driveLetter + @"\\'");
                foreach (ManagementObject drive in searcher.Get())
                {
                    shareName = drive["Name"].ToString();
                }
            }
            catch (ManagementException e)
            {
                return (e.Message);
            }
            return shareName;
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

        public static string GetHostnameOverSsh(string ipAddress)
        {
            var cmd = $"echo '';echo START;host {ipAddress}; echo DONE";
            var host = default(string);

            bool parse = false;
            List<networkvolume> drives = new List<networkvolume>();
            try
            {
                using (var sshclient = new SshClient("localhost", 22, "genis", "hallbergem3"))
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
                                } catch
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
            return linuxmountpoint +  winpath.Substring(2).Replace("\\", "/");
        }

        public static List<networkvolume> GetLinuxNetworkMounts()
        {
            var cmd = "echo '';echo START;mount; echo DONE";

            bool parse = false;
            List<networkvolume> drives = new List<networkvolume>();
            try
            {
                using (var sshclient = new SshClient("localhost", 22, "genis", "hallbergem3"))
                {
                    sshclient.Connect();
                    using (var stream = sshclient.CreateShellStream("xterm", 80, 50, 1024, 1024, 1024))
                    {
                        stream.WriteLine(cmd);
                        while (stream.CanRead)
                        {
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
                                        } catch { };
                                    } else if (line.Split(' ')[4].StartsWith("cifs"))
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
                    d.host = GetHostnameOverSsh(d.host);
                } else
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

        public static void Main(string[] args)
        {
            string importfolder = "T:\\valfur_20240422_142548_96_grid4\\WARP";
            string drive = importfolder.Substring(0, 2);
            var d = new networkvolume();
            if (IsMappedNetworkDrive(drive))
            {
                var host = GetNetworkDriveHostname(drive);
                d.host = host.Split('\\')[2];
                d.name = host.Split('\\')[3];
                if (IsIPAddress(d.host))
                {
                    d.host = GetHostname(d.host);
                }
                Console.WriteLine($"Network drive in: {d.host}");
            }
            else
            {
                var sharename = GetDriveShareName(drive);
                Console.WriteLine($"Local drive shared as: {sharename}");
                d.host = Dns.GetHostName();
                d.name = sharename;
            }
            var drives = GetLinuxNetworkMounts();

            string linuxmountpoint = default(string);
            foreach (var i in drives)
            {
                if (d.Equals(i))
                {
                    linuxmountpoint = i.mountpoint;
                    break;
                }
            }

            var linuxpath = WindowsToLinux(importfolder, linuxmountpoint);
            Console.WriteLine($"Linux path: {linuxpath}");
            Console.ReadLine();
        }

    }

    public class networkvolume
    {
        public string host;
        public string name;
        public string mountpoint;

        public bool Equals(networkvolume other)
        {
            if (this.host.ToLower() == other.host.ToLower() && this.name.ToLower() == other.name.ToLower()) return true;
            return false;
        }
    }
}