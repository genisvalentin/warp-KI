using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using cryosparcClient;
using Newtonsoft.Json;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Threading;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Windows;

namespace cryosparcClient
{

    public class Lane
    {
        public string Desc { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
    }

    public class User
    {
        public string id { get; set; }
        public string email { get; set; }
    }

    public class CryosparcResponse<template>
    {
        public string id { get; set; }
        public string jsonrpc { get; set; }
        public List<template> Result { get; set; }
    }

    public class CryosparcResponse
    {
        public string id { get; set; }
        public string jsonrpc { get; set; }
        public string Result { get; set; }
        public CryosparcResponseError Error { get; set; }
    }

    public class CryosparcOutputResultGroup
    {
        public string uid;
        public string type;
        public string name;
        public string title;
        public string description;
        public Dictionary<string, string>[] contains;
        public string passthrough;
        public int num_items;
    }

    public class CryosparcResponseError
    {
        public int code;
        public string data;
        public string message;
    }

    public class Params { }

    public class MakeJobParams : Params
    {
        public string ProjectUid = "";
        public string WorkspaceUid = "";
        public string CryosparcUser = "";
        public string CreatedByJobUid = "None";
        public string Title = "None";
        public Dictionary<string, string> job_options = new Dictionary<string, string>();
        public Dictionary<string, string> input_group_connects = new Dictionary<string, string>();
        public string EnableBench = "False";
        public string Gpuid = "0";
    }

    public class EnqueueJobParams : Params
    {
        public string ProjectUid;
        public string JobUid;
        public string Lane;
    }

    public class JobParams : Params
    {
        public string ProjectUid;
        public string JobUid;
    }

    public class CryosparcProtocol
    {
        public string ProjectName;
        public string WorkSpaceName;
        public string Lane;
        public string ProjectDir;
        public string EmptyProject;
        public string EmptyWorkSpace;
        public string ProjectDirName;
        public string CurrentJob;
        public string ImportMicrographs;
        public string Particles;
        public string particle_meta_path;
        public string particle_blob_path;
        public string particle_meta_dir;
        public string SamplingRate;
        public string RunClass2D;
        public string RunAbInit;
        public string Run3DClassification;
        public string CheckedParticles;
        public string RunCheckForCorruptParticles;
        public string ProjectUid;
        public string WorkspaceUid;
        public string Status = "Not ready";
        public bool Success = true;
        public string CryoSparcUser;
        public string SchedulerLane;
    }

    public class SessionInfo
    {
        public string sessionPath;
        public string previousSessionPath;
        public string sessionName;
        public string voltage;
        public string cs;
        public string contrast;
        public string moviePixelSize;
        public string pixelSize;
        public string boxSize;
        public string diameter;
        public string TwoD;
        public string ThreeD;
        public string NClasses;
        public string NParticles;
        public string warpSubDir;
        public string nFrames;
        public string dosePerFrame;
        public string WindowsDir;
        public string UserEmail = "";
        public string ClassificationUrl = "";
        public string CryosparcLane = "";
        public string CryosparcLicense = "";
        public string CryosparcProject;
        public string CryosparcProjectName;
        public string CryosparcProjectDir;
    }

    public class STATUS
    {
        public const string STATUS_ABORTED = "aborted";
        public const string STATUS_FAILED = "failed";
        public const string STATUS_COMPLETED = "completed";
        public const string STATUS_KILLED = "killed";
        public static string[] STOP_STATUSES = { STATUS_ABORTED, STATUS_FAILED, STATUS_KILLED, STATUS_COMPLETED };
    }

    public class Client
    {
        private CryosparcProtocol protocol = new CryosparcProtocol();

        private static readonly HttpClient client = new HttpClient();

        public static async Task<Lane[]> GetSchedulerLanes(string url, string license_id)
        {
            var data = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "get_scheduler_lanes" },
                { "params", new Object() },
                { "id", Guid.NewGuid().ToString() }
            };

            string stringData = await SendRequest(data, license_id, url);
            var result = JsonConvert.DeserializeObject<CryosparcResponse<Lane>>(stringData);
            if (result != null)
            {
                List<Lane> lanes = new List<Lane>();
                foreach (var lane in result.Result)
                {
                    LogToFile(lane.Name);
                    lanes.Add(lane);
                }

                return (lanes.ToArray());
            }

            return (new Lane[0]);
        }

        public static string GetCryosparcUserFromEmail(SessionInfo session)
        {
            string email = session.UserEmail;
            User[] users = GetCryosparcUsers(session.ClassificationUrl, session.CryosparcLicense);
            foreach (var u in users)
            {
                if (u.email == email)
                {
                    LogToFile($"Got id {u.id} for {u.email}");
                    return (u.id);
                }
            }
            return ("");
        }

        public static string GetCryosparcUserFromEmail(string email, string host, string license)
        {
            User[] users = GetCryosparcUsers(host, license);
            foreach (var u in users)
            {
                if (u.email == email)
                {
                    LogToFile($"Got id {u.id} for {u.email}");
                    return (u.id);
                }
            }
            return ("");
        }

        public static User[] GetCryosparcUsers(string host, string license_id)
        {
            // Replace the uri string with your MongoDB deployment's connection string.
            var pwd = GetMongoDbPassword(license_id);
            var client = new MongoClient("mongodb://cryosparc_user:" + pwd + "@" + host + ":39001");
            var cs_users = client.GetDatabase("meteor").GetCollection<BsonDocument>("users");
            var cs_users_list = cs_users.Find(new BsonDocument()).ToList();

            List<User> Users = new List<User>();
            foreach (var user in cs_users_list)
            {
                string id = user.GetValue("_id").ToString();
                BsonArray emails = user.GetValue("emails").AsBsonArray;
                LogToFile($"{id} {emails[0].AsBsonDocument.GetValue("address").ToString()}");
                Users.Add(new User() { id = id, email = emails[0].AsBsonDocument.GetValue("address").ToString() });
            }

            return (Users.ToArray());
        }

        public static async Task<string> GetLatestAbInitioJob(string projectUid, string host, string license_id, int nvolumes = 0)
        {
            var pwd = GetMongoDbPassword(license_id);
            var client = new MongoClient("mongodb://cryosparc_user:" + pwd + "@" + host + ":39001");
            var jobsdb = client.GetDatabase("meteor").GetCollection<BsonDocument>("jobs");
            var filter = new BsonDocument { { "project_uid", projectUid }, { "job_type", "homo_abinit" }, { "deleted", false } };
            var jobsCursor = await jobsdb.FindAsync(filter);
            var jobsList = await jobsCursor.ToListAsync();

            if (jobsList.Count < 1) {
                LogToFile($"Could not get ab-initio job");
                return (""); 
            }

            List<string> uidList = new List<string>();
            foreach (var job in jobsList)
            {
                if (job.GetValue("status") != "completed" && job.GetValue("status") != "running" && job.GetValue("status") != "queued") continue;
                string id = job.GetValue("uid").ToString();
                var vols = await GetJobVolumes(projectUid, id, host, license_id);
                if (nvolumes > 0 && nvolumes != vols.Length) continue;
                uidList.Add(id);
            }
            if (uidList.Count < 1)
            {
                LogToFile($"Could not get ab-initio job with {nvolumes} classes");
                return ("");
            }
            LogToFile($"Found ab-initio job with {nvolumes} classes: {uidList.Last()}");
            return (uidList.Last());
        }

        public static bool CryosparcIsOnline(string host)
        {
            bool isOnline = false;
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(host, 39001);
                    isOnline = true;
                }
                catch (Exception)
                {
                    isOnline = false;
                }
            }
            return isOnline;
        }
        public static async Task<List<CryosparcProject>> GetProjectsList(string host, string license_id)
        {
            List<CryosparcProject> projects = new List<CryosparcProject>();
            var pwd = GetMongoDbPassword(license_id);

            if (!CryosparcIsOnline(host)) {
                Console.WriteLine("CS is not online");
                var p = new CryosparcProject();
                p.ID = "0";
                p.ProjectName = "CS is not online";
                projects.Add(p);
                return projects; 
            }

            var client = new MongoClient("mongodb://cryosparc_user:" + pwd + "@" + host + ":39001");
            var projectsdb = client.GetDatabase("meteor").GetCollection<BsonDocument>("projects");
            var filter = new BsonDocument { { "deleted", false } };
            var projectsCursor = await projectsdb.FindAsync(filter);
            var jobsList = await projectsCursor.ToListAsync();


            if (jobsList.Count < 1) { return projects; }
            foreach (var job in jobsList)
            {
                var p = new CryosparcProject();
                p.ID = job.GetValue("uid").ToString();
                p.ProjectName = job.GetValue("title").ToString();
                projects.Add(p);
            }
            return projects;
        }

        public static async Task<string> GetLatestHeteroRefinementJob(string projectUid, string host, string license_id, int nvolumes = 0)
        {
            var pwd = GetMongoDbPassword(license_id);

            var client = new MongoClient("mongodb://cryosparc_user:" + pwd + "@" + host + ":39001");
            var jobsdb = client.GetDatabase("meteor").GetCollection<BsonDocument>("jobs");
            var filter = new BsonDocument { { "project_uid", projectUid }, { "job_type", "hetero_refine" }, { "deleted", false } };
            var jobsCursor = await jobsdb.FindAsync(filter);
            var jobsList = await jobsCursor.ToListAsync();

            if (jobsList.Count < 1) {
                LogToFile($"Could not get hetero refinement job");
                return (""); 
            }

            List<string> uidList = new List<string>();
            foreach (var job in jobsList)
            {
                if (job.GetValue("status") != "completed" && job.GetValue("status") != "running" && job.GetValue("status") != "queued") continue;
                string id = job.GetValue("uid").ToString();
                var vols = await GetJobVolumes(projectUid, id, host, license_id);
                if (nvolumes > 0 && nvolumes != vols.Length) continue;
                uidList.Add(id);
            }
            if (uidList.Count < 1) { 
                LogToFile($"Could not get hetero refiement job with {nvolumes} classes");
                return ("");
            }
            LogToFile($"Found hetero refiement job with {nvolumes} classes: {uidList.Last()}");
            return (uidList.Last());
        }

        public static async Task<string[]> GetJobVolumes(string projectUid, string jobUid, string host, string license_id)
        {
            var pwd = GetMongoDbPassword(license_id);
            var client = new MongoClient("mongodb://cryosparc_user:" + pwd + "@" + host + ":39001");
            var jobsdb = client.GetDatabase("meteor").GetCollection<BsonDocument>("jobs");
            var filter = new BsonDocument { { "project_uid", projectUid }, { "uid", jobUid } };
            var job = await jobsdb.Find(filter).FirstAsync();

            List<string> available_volumes = new List<string>();
            string output_result_groups_string = job.GetValue("output_result_groups").ToString().ToLower();
            try
            {
                CryosparcOutputResultGroup[] output_result_groups =
                    JsonConvert.DeserializeObject<CryosparcOutputResultGroup[]>(output_result_groups_string);
                foreach (var group in output_result_groups)
                {
                    if (group.type == "volume")
                    {
                        available_volumes.Add(group.name);
                    }
                }
                return (available_volumes.ToArray());
            } catch (Exception e) {
                return (new string[0]);
            }
        }
 

        public static async Task<string> SendRequest(Dictionary<string, Object> data, string LicenseId, string url, bool printCmd = true)
        {

            var jsonData = JsonConvert.SerializeObject(data);

            if (printCmd)
            {
                LogToFile("Sending post request: ");
                LogToFile(jsonData);
            }
            var contentData = new StringContent(jsonData, Encoding.UTF8, "application/json");
            contentData.Headers.Add("Originator","client");
            contentData.Headers.Add("License-ID", LicenseId);

            int counter = 0;
            while (counter < 5)
            {
                try
                {
                    var response = await client.PostAsync(url, contentData);

                    if (response.IsSuccessStatusCode)
                    {
                        var stringData = await response.Content.ReadAsStringAsync();
                        LogToFile($"Got successful response: \n{stringData}");
                        return (stringData);
                    }
                }
                catch (Exception e)
                {
                    LogToFile(e.Message);
                }
                counter++;
            }

            return (string.Empty);
        }

        public static async Task<string[]> GetOrCreateProjectDir(string project_container_dir, string session_name, string cryosparc_user, string host, string license_id)
        {
            var pwd = GetMongoDbPassword(license_id);
            string apiUrl = "http://" + host + ":39002/api";
            string mongoUrl = "mongodb://cryosparc_user:" + pwd + "@" + host + ":39001";
            string final_project_dir = "";
            var jobParams = new { project_container_dir };
            var projectCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "check_or_create_project_container_dir" },
                { "params", jobParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string jsonResponse = await SendRequest(projectCmdData, license_id, apiUrl);
            LogToFile(jsonResponse);
            var cryosparcResponse = JsonConvert.DeserializeObject<CryosparcResponse>(jsonResponse);

            if (cryosparcResponse != null)
            {
                if (cryosparcResponse.Error == null)
                {
                    final_project_dir = cryosparcResponse.Result;
                }
            }

            if (final_project_dir == "")
            {
                LogToFile($"Failed to use project directory {project_container_dir}:\n{cryosparcResponse.Error.message}");
                jobParams = new { project_container_dir = "$HOME/CryosparcProjects/" + session_name };
                LogToFile($"Project will be created in {jobParams.project_container_dir}");
                projectCmdData["params"] = jobParams;
                jsonResponse = await SendRequest(projectCmdData, license_id, apiUrl);
                cryosparcResponse = JsonConvert.DeserializeObject<CryosparcResponse>(jsonResponse);

                if (cryosparcResponse != null)
                {
                    LogToFile($"Project was created in {cryosparcResponse.Result}");
                    final_project_dir = cryosparcResponse.Result;
                }
            }

            //try to get an exist pid
            string project_uid = await getProjectUidMongo(final_project_dir, mongoUrl);

            //if no project is found in this directory, create one
            if (project_uid == "")
            {
                project_uid = await createEmptyProject(cryosparc_user, final_project_dir, session_name, apiUrl, license_id);
            }

            LogToFile($"Cryosparc project {project_uid} in {final_project_dir}");
            return (new[] { final_project_dir, project_uid });
        }

        public static async Task<string> getProjectUidMongo(string dir, string mongoUrl)
        {
            var client = new MongoClient(mongoUrl);
            var projectsCol = client.GetDatabase("meteor").GetCollection<BsonDocument>("projects");
            LogToFile($"looking for project {String.Format("{ 0}/*", dir)}");
            BsonDocument filter = new BsonDocument { { "project_dir", new BsonRegularExpression(string.Format("{0}/*", dir)) } };
            var projectsCursor = await projectsCol.FindAsync(filter);
            var projects = await projectsCursor.ToListAsync();

            if (projects.Count < 1) { return (""); }

            List<string> uidList = new List<string>();
            foreach (var project in projects)
            {
                string id = project.GetValue("uid").ToString();
                uidList.Add(id);
            }
            return (uidList.Last());
        }

        public static async Task<string> getProjectUid(string dir)
        {
            if (dir != null)
            {
                var dirs = Directory.EnumerateDirectories(Path.GetFullPath(dir));
                if (dirs.Count() > 0)
                {
                    foreach (var d in dirs)
                    {
                        if (Path.GetFileName(d).StartsWith("P"))
                        {
                            return (Path.GetFileName(d));
                        }
                    }
                }
            }
            return ("");
        }

        public static async Task<string> createEmptyProject(string cryosparcUser, string projectDir, string projectTitle, string url, string license_id)
        {
            var jobParams = new[]
            {
                cryosparcUser,
                projectDir,
                projectTitle
            };

            var projectCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "create_empty_project" },
                { "params", jobParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string jsonResponse = await SendRequest(projectCmdData, license_id, url);
            var cryosparcResponse = JsonConvert.DeserializeObject<CryosparcResponse>(jsonResponse);
            if (cryosparcResponse != null) { return (cryosparcResponse.Result.ToUpper()); }
            return ("");
        }

        public static async Task<string> GetOrCreateEmptyWorkSpace(string projectUid, string cryosparcUser, string url, string license_id, string host, string title = "Auto WARP classification")
        {
            var pwd = GetMongoDbPassword(license_id);
            var mongoUrl = "mongodb://cryosparc_user:" + pwd + "@" + host + ":39001";
            var client = new MongoClient(mongoUrl);
            var workspaceCol = client.GetDatabase("meteor").GetCollection<BsonDocument>("workspaces");
            var filter = new BsonDocument { { "project_uid", projectUid }, { "title", title } };
            var workspaces = await workspaceCol.Find(filter).ToListAsync();
            if (workspaces.Count > 0) {
                return workspaces.Last().GetValue("uid").ToString();
            }

            var jobParams = new[]
            {
                projectUid,
                cryosparcUser,
                "None",
                title,
                ""
            };

            var projectCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "create_empty_workspace" },
                { "params", jobParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string jsonResponse = await SendRequest(projectCmdData, license_id, url);
            var cryosparcResponse = JsonConvert.DeserializeObject<CryosparcResponse>(jsonResponse);
            if (cryosparcResponse != null) { return (cryosparcResponse.Result.ToUpper()); }
            return ("");
        }

        public static async Task<string> doImportMicrographs(CryosparcProtocol protocol, SessionInfo session, CancellationToken token)
        {
            int nFrames = int.Parse(session.nFrames);
            float dosePerFrame = float.Parse(session.dosePerFrame);
            //float totalDose = dosePerFrame * nFrames;
            float totalDose = 0; //WARP mics are dose weighted so we give a dose of 0.
            string doseEperA = totalDose.ToString(CultureInfo.InvariantCulture);

            var job_options = new Dictionary<string, string>()
            {
                { "blob_paths",  protocol.particle_meta_dir.TrimEnd("/"[0]) + "/average/*.mrc"},
                { "skip_header_check", "True" },
                { "psize_A", session.pixelSize },
                { "accel_kv", session.voltage },
                { "cs_mm", session.cs },
                { "total_dose_e_per_A2", doseEperA }
            };

            var importParams = new List<Object>()
            {
                "import_micrographs",
                protocol.ProjectUid,
                protocol.WorkspaceUid,
                protocol.CryoSparcUser,
                "None",
                "None",
                "None",
                job_options, //job parameters
                new {}, //input group connects
            };

            var makeCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "make_job" },
                { "params", importParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string url = "http://" + session.ClassificationUrl + ":39002/api";
            string makeCmdOutput = await SendRequest(makeCmdData, session.CryosparcLicense, url);
            var response = JsonConvert.DeserializeObject<CryosparcResponse>(makeCmdOutput);
            if (response != null) { protocol.ImportMicrographs = response.Result.ToUpper(); protocol.CurrentJob = response.Result.ToUpper(); }

            var enqueueParams = new[]
            {
                protocol.ProjectUid,
                protocol.ImportMicrographs,
                (protocol.Lane != null) ? protocol.Lane : "default"
            };

            var enqueueCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "enqueue_job" },
                { "params", enqueueParams },
                { "id", Guid.NewGuid().ToString() }
            };

            await SendRequest(enqueueCmdData, session.CryosparcLicense, url);

            string finalStatus = await waitForCryosparc(protocol, session, token);
            return (finalStatus);
        }

        public static async Task<string> waitForCryosparc(CryosparcProtocol protocol, SessionInfo session, CancellationToken token)
        {

            string status = "";
            string url = "http://" + session.ClassificationUrl + ":39002/api";
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    LogToFile("Wait for cryosparc was cancelled.");
                    LogToFile(protocol.CurrentJob + " status is " + status + "\n");
                    return (status);
                }
                status = await getJobStatus(protocol.ProjectUid, protocol.CurrentJob, url, session.CryosparcLicense);
                LogToFile(protocol.CurrentJob + " status is " + status + "\n");
                if (STATUS.STOP_STATUSES.Contains(status))
                {
                    break;
                }
                await waitJob(protocol.ProjectUid, protocol.CurrentJob, url, session.CryosparcLicense);
            }

            if (status != STATUS.STATUS_COMPLETED)
            {
                LogToFile("Protocol failed");
            }

            return (status);
        }

        public static async Task<string> getJobStatus(string projectUid, string jobid, string url, string license_id)
        {

            var GetJobStatusParams = new[]
            {
                projectUid,
                jobid
            };

            var enqueueCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "get_job_status" },
                { "params", GetJobStatusParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string responseJson = await SendRequest(enqueueCmdData, license_id, url);

            CryosparcResponse response = JsonConvert.DeserializeObject<CryosparcResponse>(responseJson);
            if (response != null)
            {
                return (response.Result);
            }
            else
            {
                return ("");
            }
        }

        public static async Task waitJob(string projectUid, string jobid, string url, string license_id)
        {
            var WaitJobParams = new[]
            {
                projectUid,
                jobid
            };

            var WaitCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "wait_job_complete" },
                { "params", WaitJobParams },
                { "id", Guid.NewGuid().ToString() }
            };

            await SendRequest(WaitCmdData, license_id, url);
        }

        public static async Task<string> doImportParticles(CryosparcProtocol protocol, SessionInfo session, CancellationToken token)
        {
            var job_options = new Dictionary<string, string>()
            {
                { "particle_meta_path", protocol.particle_meta_path },
                { "remove_leading_uid", "True" }
            };

            var input_group_connects = new Dictionary<string, string>()
            {
                { "micrographs", String.Format("{0}.imported_micrographs", protocol.ImportMicrographs) }
            };

            var cmdParams = new List<Object>()
            {
                "import_particles",
                protocol.ProjectUid,
                protocol.WorkspaceUid,
                protocol.CryoSparcUser,
                "None",
                "None",
                "None",
                job_options,
                input_group_connects
            };

            var makeCmd = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "make_job" },
                { "params", cmdParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string url = "http://" + session.ClassificationUrl + ":39002/api";
            string makeCmdOutput = await SendRequest(makeCmd, session.CryosparcLicense, url);
            var makeResponse = JsonConvert.DeserializeObject<CryosparcResponse>(makeCmdOutput);
            if (makeResponse != null)
            {
                protocol.Particles = makeResponse.Result.ToUpper();
                protocol.CurrentJob = makeResponse.Result.ToUpper();
            }
            else
            {
                return ("");
            }

            await EnqueueJob(protocol.ProjectUid, protocol.Particles, url, session.CryosparcLicense, protocol.Lane);
            string finalStatus = await waitForCryosparc(protocol, session, token);

            //If job fails, try importing without linked micrographs

            if (finalStatus != STATUS.STATUS_COMPLETED && !token.IsCancellationRequested)
            {
                cmdParams = new List<Object>()
                {
                    "import_particles",
                    protocol.ProjectUid,
                    protocol.WorkspaceUid,
                    protocol.CryoSparcUser,
                    "None",
                    "None",
                    "None",
                    job_options,
                    new {}
                };

                makeCmd = new Dictionary<string, Object>()
                {
                    { "jsonrpc", "2.0" },
                    { "method", "make_job" },
                    { "params", cmdParams },
                    { "id", Guid.NewGuid().ToString() }
                };

                makeCmdOutput = await SendRequest(makeCmd, session.CryosparcLicense, url);
                makeResponse = JsonConvert.DeserializeObject<CryosparcResponse>(makeCmdOutput);
                if (makeResponse != null)
                {
                    protocol.Particles = makeResponse.Result.ToUpper();
                    protocol.CurrentJob = makeResponse.Result.ToUpper();
                }
                else { return (""); }

                await EnqueueJob(protocol.ProjectUid, protocol.Particles, url, session.CryosparcLicense, protocol.Lane);
                finalStatus = await waitForCryosparc(protocol, session, token);
            }
            return (finalStatus);
        }

        public static async Task EnqueueJob(string projectUid, string jobId, string url, string license_id, string lane = null)
        {
            var enqueueParams = new[]
            {
                projectUid,
                jobId,
                (lane != null) ? lane : "default"
            };

            var enqueueCmdData = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "enqueue_job" },
                { "params", enqueueParams },
                { "id", Guid.NewGuid().ToString() }
            };

            await SendRequest(enqueueCmdData, license_id, url);
        }

        public static async Task<string> doCheckForCorruptParticles(CryosparcProtocol protocol, SessionInfo session, CancellationToken token)
        {

            var job_options = new Dictionary<string, string>()
            {
                { "do_nancheck", "False" }
            };

            var input_group_connects = new Dictionary<string, string>()
            {
                { "particles", String.Format("{0}.imported_particles", protocol.Particles) }
            };

            var cmdParams = new List<Object>()
            {
                "check_corrupt_particles",
                protocol.ProjectUid,
                protocol.WorkspaceUid,
                protocol.CryoSparcUser,
                "None",
                "None",
                "None",
                job_options,
                input_group_connects
            };

            var makeCmd = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "make_job" },
                { "params", cmdParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string url = "http://" + session.ClassificationUrl + ":39002/api";
            string makeCmdOutput = await SendRequest(makeCmd, session.CryosparcLicense, url);
            var makeResponse = JsonConvert.DeserializeObject<CryosparcResponse>(makeCmdOutput);
            if (makeResponse != null)
            {
                protocol.CheckedParticles = makeResponse.Result.ToUpper();
                protocol.CurrentJob = makeResponse.Result.ToUpper();
            }
            else
            {
                return ("");
            }

            await EnqueueJob(protocol.ProjectUid, protocol.CheckedParticles, url, session.CryosparcLicense, protocol.Lane);
            string finalStatus = await waitForCryosparc(protocol, session, token);
            return (finalStatus);
        }

        public static async Task<string> doRunClass2D(CryosparcProtocol protocol, SessionInfo session, CancellationToken token)
        {

            var job_options = new Dictionary<string, string>()
            {
                { "class2D_K", session.NClasses },
                { "class2D_window", "True" },
                { "class2D_window_inner_A", session.diameter },
                { "intermediate_plots", "True" },
                { "compute_use_ssd", "True" }
            };

            var input_group_connects = new Dictionary<string, string>()
            {
                { "particles", String.Format("{0}.particles", protocol.CheckedParticles) }
            };

            var cmdParams = new List<Object>()
            {
                "class_2D",
                protocol.ProjectUid,
                protocol.WorkspaceUid,
                protocol.CryoSparcUser,
                "None",
                "None",
                "None",
                job_options,
                input_group_connects
            };

            var makeCmd = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "make_job" },
                { "params", cmdParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string url = "http://" + session.ClassificationUrl + ":39002/api";
            string makeCmdOutput = await SendRequest(makeCmd, session.CryosparcLicense, url);
            var makeResponse = JsonConvert.DeserializeObject<CryosparcResponse>(makeCmdOutput);
            if (makeResponse != null)
            {
                protocol.RunClass2D = makeResponse.Result.ToUpper();
                protocol.CurrentJob = makeResponse.Result.ToUpper();
            }
            else
            {
                return ("");
            }

            await EnqueueJob(protocol.ProjectUid, protocol.RunClass2D, url, session.CryosparcLicense, protocol.Lane);
            string finalStatus = await waitForCryosparc(protocol, session, token);
            return (finalStatus);
        }

        public static async Task<string> doRunHetero(CryosparcProtocol protocol, SessionInfo session, CancellationToken token)
        {
            LogToFile("Do Run Hetero started");
            //Job options in ./cryosparc_compute/jobs/hetero_refine/build.py
            var job_options = new Dictionary<string, Object>() //Changed from Dictionary<string, string> for testing
            {
                { "intermediate_plots", "True" },
                { "compute_use_ssd", "True" },
                { "multirefine_assignment_conv_eps", 0.1 }, //Default is 0.05
                { "multirefine_num_final_full_iters", 1 } //Default is 2
            };

            var input_group_connects = new Dictionary<string, string>()
            {
                { "particles", String.Format("{0}.particles", protocol.CheckedParticles) }
            };

            var cmdParams = new List<Object>()
            {
                "hetero_refine",
                protocol.ProjectUid,
                protocol.WorkspaceUid,
                protocol.CryoSparcUser,
                "None",
                "None",
                "None",
                job_options,
                input_group_connects
            };

            var makeCmd = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "make_job" },
                { "params", cmdParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string url = "http://" + session.ClassificationUrl + ":39002/api";
            string makeCmdOutput = await SendRequest(makeCmd, session.CryosparcLicense, url);
            var makeResponse = JsonConvert.DeserializeObject<CryosparcResponse>(makeCmdOutput);
            if (makeResponse == null) return ("");

            string[] input_volumes = new string[0];
            string VolumesSource;
            if (protocol.Run3DClassification == "" || protocol.Run3DClassification == null)
            {
                input_volumes = await GetJobVolumes(protocol.ProjectUid, protocol.RunAbInit, session.ClassificationUrl, session.CryosparcLicense);
                VolumesSource = protocol.RunAbInit;
            } else
            {
                input_volumes = await GetJobVolumes(protocol.ProjectUid, protocol.Run3DClassification, session.ClassificationUrl, session.CryosparcLicense);
                VolumesSource = protocol.Run3DClassification;
            }

            protocol.Run3DClassification = makeResponse.Result.ToUpper();
            protocol.CurrentJob = makeResponse.Result.ToUpper();

            int nclasses = 0;
            int.TryParse(session.NClasses, out nclasses);

            Dictionary<string, Object> connectCmd;
            List<Object> connectParams;
            if (input_volumes.Length > 0 && input_volumes.Length == nclasses)
            {
                foreach (var volume in input_volumes)
                {
                    connectParams = new List<object>()
                    {
                        protocol.ProjectUid,
                        String.Format("{0}.{1}", VolumesSource, volume),
                        String.Format("{0}.volume", protocol.Run3DClassification)
                    };
                    connectCmd = new Dictionary<string, Object>()
                    {
                        { "jsonrpc", "2.0" },
                        { "method", "job_connect_group" },
                        { "params", connectParams },
                        { "id", Guid.NewGuid().ToString() }
                    };
                    await SendRequest(connectCmd, session.CryosparcLicense, url);
                }
            }

            await EnqueueJob(protocol.ProjectUid, protocol.CurrentJob, url, session.CryosparcLicense, protocol.Lane);
            protocol.Run3DClassification = makeResponse.Result.ToUpper();
            string finalStatus = await waitForCryosparc(protocol, session, token);
            return (finalStatus);
        }

        public static async Task<string> doRunAbInit(CryosparcProtocol protocol, SessionInfo session, CancellationToken token)
        {
            var job_options = new Dictionary<string, string>()
            {
                { "intermediate_plots", "True" },
                { "abinit_num_particles" , session.NParticles },
                { "abinit_K", session.NClasses },
                { "compute_use_ssd", "True" }
            };

            var input_group_connects = new Dictionary<string, string>()
            {
                { "particles", String.Format("{0}.particles", protocol.CheckedParticles) }
            };

            var cmdParams = new List<Object>()
            {
                "homo_abinit",
                protocol.ProjectUid,
                protocol.WorkspaceUid,
                protocol.CryoSparcUser,
                "None",
                "None",
                "None",
                job_options,
                input_group_connects
            };

            var makeCmd = new Dictionary<string, Object>()
            {
                { "jsonrpc", "2.0" },
                { "method", "make_job" },
                { "params", cmdParams },
                { "id", Guid.NewGuid().ToString() }
            };

            string url = "http://" + session.ClassificationUrl + ":39002/api";
            string makeCmdOutput = await SendRequest(makeCmd, session.CryosparcLicense, url);
            var makeResponse = JsonConvert.DeserializeObject<CryosparcResponse>(makeCmdOutput);
            if (makeResponse != null)
            {
                protocol.RunClass2D = makeResponse.Result.ToUpper();
                protocol.CurrentJob = makeResponse.Result.ToUpper();
            }
            else
            {
                return ("");
            }

            await EnqueueJob(protocol.ProjectUid, protocol.RunClass2D, url, session.CryosparcLicense, protocol.Lane);
            string finalStatus = await waitForCryosparc(protocol, session, token);
            return (finalStatus);
        }

        public async Task Run(SessionInfo session, CancellationToken token)
        {
            //protocol = new CryosparcProtocol();
            protocol.Success = false;
            protocol.Status = "Starting...";
            protocol.CryoSparcUser = GetCryosparcUserFromEmail(session);
            protocol.Lane = session.CryosparcLane;
            LogToFile("Starting...");

            protocol.particle_meta_dir = session.sessionPath;
            protocol.particle_meta_dir = protocol.particle_meta_dir.TrimEnd('/');
            protocol.particle_meta_path = protocol.particle_meta_dir + "/goodparticles_cryosparc_input.star";

            LogToFile("particle_meta_path");
            LogToFile(protocol.particle_meta_path);

            string url = "http://" + session.ClassificationUrl + ":39002/api";

            if (session.CryosparcProject == null)
            {
                var p = await GetOrCreateProjectDir(protocol.particle_meta_dir, session.sessionName, protocol.CryoSparcUser, session.ClassificationUrl, session.CryosparcLicense); //returns array with ProjectDir (linux) and ProjectUid
                session.CryosparcProjectDir = p[0];
                session.CryosparcProject = p[1];
            }
            protocol.ProjectDir = session.CryosparcProjectDir;
            protocol.ProjectUid = session.CryosparcProject;

            protocol.WorkspaceUid = await GetOrCreateEmptyWorkSpace(protocol.ProjectUid, protocol.CryoSparcUser, url, session.CryosparcLicense, session.ClassificationUrl, session.sessionName);

            protocol.Status = session.ClassificationUrl + ":39000/browse/" + protocol.ProjectUid + "-" + protocol.WorkspaceUid + "-J*";

            string lastJobStatus;
            lastJobStatus = await doImportMicrographs(protocol, session, token);
            if (lastJobStatus != STATUS.STATUS_COMPLETED)
            {
                protocol.Status = "Classification failed!";
                protocol.Success = true;
                return;
            }

            if (token.IsCancellationRequested)
            {
                protocol.Success = true;
                return;
            }

            lastJobStatus = await doImportParticles(protocol, session, token);
            if (lastJobStatus != STATUS.STATUS_COMPLETED)
            {
                protocol.Status = "Classification failed!";
                protocol.Success = true;
                return;
            }
            if (token.IsCancellationRequested)
            {
                protocol.Success = true;
                return;
            }

            lastJobStatus = await doCheckForCorruptParticles(protocol, session, token);
            if (lastJobStatus != STATUS.STATUS_COMPLETED)
            {
                protocol.Status = "Classification failed!";
                protocol.Success = true;
                return;
            }
            if (token.IsCancellationRequested)
            {
                protocol.Success = true;
                return;
            }

            if (session.TwoD.Trim() == "True" || session.TwoD.Trim() == "true")
            {
                lastJobStatus = await doRunClass2D(protocol, session, token);
            }
            else
            {
                int nclasses = 0;
                int.TryParse(session.NClasses, out nclasses);
                LogToFile($"Looking for classification job with {nclasses} classes");
                protocol.Run3DClassification = await GetLatestHeteroRefinementJob(protocol.ProjectUid, session.ClassificationUrl, session.CryosparcLicense, nclasses);
                protocol.RunAbInit = await GetLatestAbInitioJob(protocol.ProjectUid, session.ClassificationUrl, session.CryosparcLicense, nclasses);
                bool FoundHetero = false;
                bool FoundAbInit = false;
                if (protocol.Run3DClassification != null && protocol.Run3DClassification != "")
                {
                    FoundHetero = true;
                    LogToFile($"Using volumes from hetero refiement job {protocol.Run3DClassification}");
                }

                if (!FoundHetero && protocol.RunAbInit != null && protocol.RunAbInit != "")
                {
                    FoundAbInit = true;
                    LogToFile($"Using volumes from ab-initio job {protocol.RunAbInit}");
                }

                if (FoundHetero || FoundAbInit)
                {
                    lastJobStatus = await doRunHetero(protocol, session, token);
                }
                else
                {
                    lastJobStatus = await doRunAbInit(protocol, session, token);
                }
            }

            if (lastJobStatus != STATUS.STATUS_COMPLETED)
            {
                protocol.Status = "Classification failed!";
            }
            protocol.Success = true;
            return;
        }

        public Dictionary<string, string> getClassificationResults(string sessionName)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            results["status"] = protocol.Status;
            results["success"] = (protocol.Success) ? "True" : "False";
            return (results);
        }

        public void SetClassificationResults(string sessionName, string Status)
        {
            protocol.Status = Status;
        }

        public void SetClassificationResults(string sessionName, bool Success)
        {
            protocol.Success = Success;
        }

        public static string GetMongoDbPassword(string license_id)
        {
            var sb = new StringBuilder();
            using (SHA256 mySHA256 = SHA256.Create())
            {
                var hash = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(license_id + "-user"));
                
                for (int i = 0; i < 10; i++)
                {
                    sb.Append($"{hash[i]:X2}");
                }
            }
            return(sb.ToString().ToLower());
        }

        public static void LogToFile(string s)
        {
            string LogFile = Path.Combine(AppContext.BaseDirectory, "log.txt");
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    using (StreamWriter outputFile = new StreamWriter(LogFile, true))
                    {
                        await outputFile.WriteLineAsync(s);
                    }
                }
                catch
                {
                    LogToFile(s);
                };
            });
            Console.WriteLine($"Log: {s}");
        }
    }

    public class CryosparcProject
    {
        public string ID { get; set; }
        public string ProjectName { get; set; }
    }
}