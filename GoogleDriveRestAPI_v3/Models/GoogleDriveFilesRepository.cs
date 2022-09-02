using Azure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace GoogleDriveRestAPI_v3.Models
{
    public class GoogleDriveFilesRepository
    {
        //defined scope.
        public static string[] Scopes = { DriveService.Scope.Drive };

        //create Drive API service.
        public static DriveService GetService()
        {
            //get Credentials from client_secret.json file 
            UserCredential credential;
            using (var stream = new FileStream(@"M:\client_secret2.json", FileMode.Open, FileAccess.Read))
            {
                String FolderPath = @"M:\";
                String FilePath = Path.Combine(FolderPath, "DriveServiceCredentials.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(FilePath, true)).Result;
            }

            //create Drive API service.
            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleDriveRestAPI-v3",
            });
            return service;
        }

        //get all files from Google Drive.
        public static List<GoogleDriveFiles> GetDriveFiles()
        {
            DriveService service = GetService();

            // define parameters of request.
            FilesResource.ListRequest FileListRequest = service.Files.List();

            //listRequest.PageSize = 10;
            //listRequest.PageToken = 10;
            FileListRequest.Fields = "nextPageToken, files(id, name, size, createdTime)";

            //get file list.
            IList<Google.Apis.Drive.v3.Data.File> files = FileListRequest.Execute().Files;
            List<GoogleDriveFiles> FileList = new List<GoogleDriveFiles>();

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    GoogleDriveFiles File = new GoogleDriveFiles
                    {
                        Id = file.Id,
                        Name = file.Name,
                        Size = file.Size,
                        CreatedTime = file.CreatedTime
                    };
                    FileList.Add(File);
                }
            }
            return FileList;
        }


        // //file encryption && Upload to the Google Drive file 

        public static void FileEncryption(HttpPostedFileBase file)
        {

            byte[] file1 = new byte[file.ContentLength];
            file.InputStream.Read(file1, 0, file.ContentLength);
            string fileName = file.FileName;

            // key for encryption
            byte[] Key = Encoding.UTF8.GetBytes("asdf!@#$1234ASDF");

            string outputFile = Path.Combine(HttpContext.Current.Server.MapPath("~/GoogleDriveFiles"), fileName);
            if (File.Exists(outputFile))
            {
                // Show Already exist Message 
            }
            else
            {
                FileStream fs = new FileStream(outputFile, FileMode.Create);
                RijndaelManaged rmCryp = new RijndaelManaged();
                CryptoStream cs = new CryptoStream(fs, rmCryp.CreateEncryptor(Key, Key), CryptoStreamMode.Write);
                foreach (var data in file1)
                {
                    cs.WriteByte((byte)data);
                }
                cs.Close();
                fs.Close();
            }
            DriveService service = GetService();
            var FileMetaData = new Google.Apis.Drive.v3.Data.File();
            FileMetaData.Name = Path.GetFileName(file.FileName);
            FileMetaData.MimeType = MimeMapping.GetMimeMapping(outputFile);

            FilesResource.CreateMediaUpload request;

            using (var stream = new System.IO.FileStream(outputFile, System.IO.FileMode.Open))
            {
                request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                request.Fields = "id";
                request.Upload();
            }
        }

        //file Upload to the Google Drive.
        /* public static void FileUpload(HttpPostedFileBase file)
         {

             if (file != null && file.ContentLength > 0)
             {
                 DriveService service = GetService();

                 string path = Path.Combine(HttpContext.Current.Server.MapPath("~/GoogleDriveFiles"),
                 Path.GetFileName(file.FileName));
                 file.SaveAs(path);

                 var FileMetaData = new Google.Apis.Drive.v3.Data.File();
                 FileMetaData.Name = Path.GetFileName(file.FileName);
                 FileMetaData.MimeType = MimeMapping.GetMimeMapping(path);

                 FilesResource.CreateMediaUpload request;

                 using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
                 {
                     request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                     request.Fields = "id";
                     request.Upload();
                 }
             }


         }*/

        /*  protected void btUpload_Click(object sender, EventArgs e)
          {

              byte[] file = new byte[FileUpload.PostedFile.ContentLength];
              FileUpload.PostedFile.InputStream.Read(file, 0, FileUpload.PostedFile.ContentLength);

              string fileName = FileUpload.PostedFile.FileName;

              // key for encryption
              byte[] Key = Encoding.UTF8.GetBytes("asdf!@#$1234ASDF");
              try
              {
                  string outputFile = Path.Combine(Server.MapPath("~/GoogleDriveFiles"), fileName);
                  if (File.Exists(outputFile))
                  {
                      // Show Already exist Message 
                  }
                  else
                  {
                      FileStream fs = new FileStream(outputFile, FileMode.Create);
                      RijndaelManaged rmCryp = new RijndaelManaged();
                      CryptoStream cs = new CryptoStream(fs, rmCryp.CreateEncryptor(Key, Key), CryptoStreamMode.Write);
                      foreach (var data in file)
                      {
                          cs.WriteByte((byte)data);
                      }
                      cs.Close();
                      fs.Close();
                  }

                  FileUpload();
              }
              catch
              {
                  Response.Write("Encryption Failed! Please try again.");
              }
          }*/

       
        //Download file from Google Drive by fileId.
        public static string DownloadGoogleFile(string fileId)
        {

            DriveService service = GetService();

            string FolderPath = HttpContext.Current.Server.MapPath("/GoogleDriveFiles/");
            FilesResource.GetRequest request = service.Files.Get(fileId);

            string FileName = request.Execute().Name;
            string FilePath =Path.Combine(FolderPath, FileName);

            MemoryStream stream1 = new MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            //Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                         //   Console.WriteLine("Download complete.");
                            SaveStream(stream1, FilePath);
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };

            request.Download(stream1);
            return FilePath;
        }


        // file save to server path
        private static void SaveStream(MemoryStream stream, string FilePath)
        {
            using (FileStream file = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.WriteTo(file);
            }
        }

        //Delete file from the Google drive
        public static void DeleteFile(GoogleDriveFiles files)
        {
            DriveService service = GetService();
            try
            {
                // Initial validation.
                if (service == null)
                    throw new ArgumentNullException("service");

                if (files == null)
                    throw new ArgumentNullException(files.Id);

                // Make the request.
                service.Files.Delete(files.Id).Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Delete failed.", ex);
            }
        }
    }

}