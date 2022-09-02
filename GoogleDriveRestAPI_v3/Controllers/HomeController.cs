using GoogleDriveRestAPI_v3.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace GoogleDriveRestAPI_v3.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult GetGoogleDriveFiles()
        {
            return View(GoogleDriveFilesRepository.GetDriveFiles());
        }

        [HttpPost]
        public ActionResult DeleteFile(GoogleDriveFiles file)
        {
            GoogleDriveFilesRepository.DeleteFile(file);
            return RedirectToAction("GetGoogleDriveFiles");
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {

           
            GoogleDriveFilesRepository.FileEncryption(file);
            return RedirectToAction("GetGoogleDriveFiles");
        }

        public void DownloadFile(string id)
        {

            // Decrypt & Download Here
            string FilePath = GoogleDriveFilesRepository.DownloadGoogleFile(id);
            Response.ContentType = "application/zip";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(FilePath));

            byte[] Key = Encoding.UTF8.GetBytes("asdf!@#$1234ASDF");
            FileStream fs = new FileStream(FilePath, FileMode.Open);
            RijndaelManaged rmCryp = new RijndaelManaged();
            CryptoStream cs = new CryptoStream(fs, rmCryp.CreateDecryptor(Key, Key), CryptoStreamMode.Read);
            try
            {
                int data;
                while ((data = cs.ReadByte()) != -1)
                {
                    Response.OutputStream.WriteByte((byte)data);
                    Response.Flush();

                }
                cs.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message);
            }
            finally
            {
                cs.Close();
                fs.Close();
            }

         
            Response.WriteFile(System.Web.HttpContext.Current.Server.MapPath("~/GoogleDriveFiles/" + Path.GetFileName(FilePath)));
            Response.End();
            Response.Flush();
        }
    }
}