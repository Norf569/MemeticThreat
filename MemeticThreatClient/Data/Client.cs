using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using MemeticThreatClient.Models;

namespace MemeticThreatClient.Data
{
    internal class Client
    {
        private static Client clientInstance;
        private static Mutex mutex;
        private static IFileUpdateObserver fileUpdateObserver;

        private const string BASE_ADDR = "https://localhost:7092/";
        private const long MAX_BODY_SIZE = (long)30000000;
        private const string DOWNLOAD_PATH = "E:\\PSU\\5\\sp\\MemThreat\\MemeticThreatClient\\MemeticThreatClient\\files\\";

        public const long BASE_DIVIDER = 1000000000;
        public const double GB_DOUBLE = 1.024 * 1.024 * 1.024;

        public User User { get; set; }
        public NodeWrapper FileTree { get; set; }

        private HttpClient client;

        private Client()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(BASE_ADDR);

            User = new User();
            FileTree = null;

            mutex = new Mutex(false);
        }
        public static Client GetInstance()
        {
            if (clientInstance == null)
                clientInstance = new Client();
            return clientInstance;
        }
        public static IFileUpdateObserver SetFileUpdateObserver { set => fileUpdateObserver = value; }
        private void AuthorizeViaResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                string jsonUser = response.Content.ReadAsStringAsync().Result;
                User = JsonSerializer.Deserialize<User>(jsonUser);
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + User.Jwt);
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new AuthenticationException();
            else
                throw new Exception("Auth exception");
        }

        //
        public void Login(string userName, string pass)
        {
            var response = client.GetAsync("/login?name=" + userName + "&password=" + pass).Result;
            AuthorizeViaResponse(response);
        }

        //
        public void Register(string userName, string email, string pass)
        {
            var response = client.GetAsync("/register?name=" + userName + "&email=" + email + "&password=" + pass).Result;
            AuthorizeViaResponse(response);
        }
        public void DeleteUser()
        {
            var response = client.DeleteAsync("/deleteUser").Result;
            if (response.IsSuccessStatusCode)
                Logout();
            else
                throw new Exception(response.StatusCode.ToString());
        }
        public void Logout()
        {
            if (User != null)
            {
                //
                this.FileTree = new NodeWrapper();
                User = new User();
                client.DefaultRequestHeaders.Remove("Authorization");
            }
            else
                throw new Exception("User isn't authorized");
        }
        public void GetFiles()
        {
            var response = client.GetAsync("/view").Result;
            if (response.IsSuccessStatusCode)
            {
                string responseString = response.Content.ReadAsStringAsync().Result;
                List<FileModel> fileList = JsonSerializer.Deserialize<List<FileModel>>(responseString);

                FileTree = BuildFileTree(fileList);
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();
            else
                throw new Exception(response.StatusCode.ToString());

            response = client.GetAsync("/storage").Result;
            if (response.IsSuccessStatusCode)
            {
                User.TotalFileSize = Convert.ToInt64(response.Content.ReadAsStringAsync().Result);
            }
            else
                throw new Exception(response.StatusCode.ToString());
        }
        public void UploadFile(string filePath)
        {
            long partLenght = MAX_BODY_SIZE;

            if (!System.IO.File.Exists(filePath))
                throw new Exception($"{filePath} does not exist");

            string fileName = filePath.Split('\\').Last();

            if (new System.IO.FileInfo(filePath).Length > (long)2 * 1024 * 1024 * 1024)
                throw new System.IO.FileFormatException("File is too large");

            new Thread(f =>
            {
                mutex.WaitOne();
                try
                {
                    using (var reader = new System.IO.FileStream(path: filePath,
                    mode: System.IO.FileMode.Open,
                    share: System.IO.FileShare.Read,
                    access: System.IO.FileAccess.Read))
                    {

                        byte[] buffer = new byte[partLenght];
                        int filesCount = (int)Math.Ceiling(reader.Length / (partLenght * 1.0));

                        fileUpdateObserver.UploadedFilesUpdate(true);
                        fileUpdateObserver.UploadedFilesUpdate(0, filesCount);

                        for (int i = 0; i <= filesCount; i++)
                        {
                            long byteReaded = reader.Read(buffer, 0, buffer.Length);
                            byte[] bufferReaded = new byte[byteReaded];
                            Array.Copy(buffer, bufferReaded, byteReaded);

                            using (var byteContent = new ByteArrayContent(bufferReaded))
                            {
                                byteContent.Headers.Add("FileName", fileName + $".part_{i}.{filesCount}");
                                byteContent.Headers.Add("BaseFileName", fileName);
                                byteContent.Headers.Add("FileSize", reader.Length.ToString());
                                byteContent.Headers.ContentLength = byteReaded;

                                var response = client.PostAsync(client.BaseAddress, byteContent).Result;

                                if (response.StatusCode == HttpStatusCode.Unauthorized)
                                    throw new UnauthorizedAccessException();
                                else if (response.StatusCode == HttpStatusCode.BadRequest)
                                    throw new System.IO.FileLoadException("Not enougth storage space");
                                else if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    int filesUploaded = Convert.ToInt32(response.Headers.GetValues("UploadedFilesCount").First());
                                    fileUpdateObserver.UploadedFilesUpdate(filesUploaded, filesCount);
                                    if (filesUploaded == filesCount)
                                        break;
                                    //Trace.WriteLine($"Uploaded files: {filesUploaded}/{filesCount + 1}");
                                }
                            }
                        }
                    }
                }
                finally
                {
                    fileUpdateObserver.UploadedFilesUpdate(false);
                    mutex.ReleaseMutex();
                }
            }).Start();
        }

        //
        public void DownloadFile(string serverFilePath)
        {
            var response = client.GetAsync("?FilePath=" + serverFilePath).Result;

            if (response.IsSuccessStatusCode)
            {
                if (!System.IO.File.Exists(DOWNLOAD_PATH))
                    System.IO.Directory.CreateDirectory(DOWNLOAD_PATH);

                string fileName = response.Headers.GetValues("FileName").First();

                new Thread(f =>
                {
                    try
                    {
                        using (var stream = response.Content.ReadAsStreamAsync().Result)
                        using (var writer = new System.IO.FileStream(path: DOWNLOAD_PATH + fileName,
                                                                                mode: System.IO.FileMode.Create,
                                                                                share: System.IO.FileShare.None,
                                                                                access: System.IO.FileAccess.Write))
                        {
                            stream.CopyTo(writer);
                            stream.Flush();
                            stream.Dispose();
                            writer.Dispose();
                        }
                    } catch { throw new Exception("File download exception"); } 
                    finally { Thread.CurrentThread.Abort(); }
                }).Start();
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();
            else
                throw new Exception("File download exception");
        }
        public void DeleteFile (int fileId)
        {
            var response = client.DeleteAsync("https://localhost:7092?id=" + fileId.ToString()).Result;
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();
            else if (!response.IsSuccessStatusCode)
                throw new Exception("Delete file exception");

            fileUpdateObserver.UpdateFiles();
        }
        private NodeWrapper BuildFileTree(List<FileModel> fileList)
        {
            NodeWrapper TreeRoot = null;
            foreach (FileModel file in fileList)
            {
                string[] dirs = file.Path.Split('\\');
                var newFileNode = new NodeWrapper(file);

                if (TreeRoot == null)
                    TreeRoot = new NodeWrapper(new Directory(dirs[0]));
                var Node = TreeRoot;
                for (int i = 1; i < dirs.Length; i++)
                {
                    if (dirs[i] == "")
                        continue;
                    Directory newDirectory = new Directory(dirs[i]);
                    //
                    NodeWrapper tempNode = (NodeWrapper)Node.ChildWithValue(newDirectory);
                    if (tempNode != null)
                        Node = tempNode;
                    else
                    {
                        Node = new NodeWrapper(newDirectory);
                        Node.AddChild(Node);
                    }
                }
                Node.AddChild(newFileNode);
            }
            return TreeRoot;
        }
    }
}
