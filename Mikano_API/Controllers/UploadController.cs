using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Mikano_API.Models;
using Newtonsoft.Json;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    //[Authorize]
    [RoutePrefix("api/upload")]
    public class UploadController : SharedController<SocketHub>
    {
        private string RelativeStorageRoot
        {
            get
            {
                var httpServer = HttpContext.Current.Server;
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsRemoteUpload"]))
                {
                    return Path.GetFullPath(Path.Combine(httpServer.MapPath("~"), @"..\BackOffice\wwwroot\" + "/" + ConfigurationManager.AppSettings["MediaFolder"]));
                }
                else
                {
                    return Path.Combine(httpServer.MapPath("~/" + ConfigurationManager.AppSettings["MediaFolder"]));
                }
            } //Path should! always end with '/'

        }
        private string StorageRoot
        {
            get
            {
                var httpServer = HttpContext.Current.Server;
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsRemoteUpload"]))
                {
                    return Path.GetFullPath(Path.Combine(httpServer.MapPath("~"), @"..\BackOffice\wwwroot\" + "/" + ConfigurationManager.AppSettings["MediaFolder"]));
                }
                else
                {
                    return Path.Combine(httpServer.MapPath("~/" + ConfigurationManager.AppSettings["MediaFolder"]));
                }
            } //Path should! always end with '/'
        }


        private bool IsImage(string ext)
        {
            return ext == ".gif" || ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".svg" || ext == ".svg";
        }

        private string EncodeFile(string fileName)
        {
            return Convert.ToBase64String(System.IO.File.ReadAllBytes(fileName));
        }


        [HttpDelete]
        public HttpResponseMessage DeleteFile(string directory, string fileName)
        {
            var filePath = StorageRoot + directory + "/" + fileName;
            //if (File.Exists(filePath))
            //{
            //    File.Delete(filePath);
            //}

            return Request.CreateResponse(HttpStatusCode.OK, new { error = string.Empty });
        }

        [HttpGet]
        public void Download(string id)
        {
            var httpServer = HttpContext.Current.Server;
            var filename = id;
            var filePath = Path.Combine(httpServer.MapPath("~/Files"), filename);

            var context = HttpContext.Current;

            if (System.IO.File.Exists(filePath))
            {
                context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + filename + "\"");
                context.Response.ContentType = "application/octet-stream";
                context.Response.ClearContent();
                context.Response.WriteFile(filePath);
            }
            else
                context.Response.StatusCode = 404;
        }

        [HttpPost]
        public HttpResponseMessage UploadFiles(bool hasCaption, bool hasDescription, bool hasCheckbox, bool hasLink, string inputName, string directory, int maxNumberOfFiles)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

            #region Default Attributes
            string allowedExtensions = (ConfigurationManager.AppSettings["AllowedExtensions"]).ToString();
            long maxFileSize = Convert.ToInt64(ConfigurationManager.AppSettings["MaxFileSize"]);
            #endregion

            var httpRequest = HttpContext.Current.Request;

            var statuses = new List<ViewDataUploadFilesResult>();

            CreateDirectoryIfNotExists(StorageRoot + directory);

            for (int i = 0; i < httpRequest.Files.Count; i++)
            {
                var file = httpRequest.Files[i];
                if (file.ContentLength <= maxFileSize)
                {
                    string fileExt = Path.GetExtension(file.FileName).ToLower();

                    if (allowedExtensions.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Contains(fileExt.Replace(".", "")))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName.Replace(" ", "-").Replace("/", "-").Replace("&", "-").Replace("~", "-").Replace("*", "-").Replace("?", "-").Replace("!", "-").Replace("--", "-"));
                        string fullFileName = fileName + fileExt;
                        var orignialFullFileName = fullFileName;

                        var fullPath = Path.Combine(StorageRoot + directory + "/", fullFileName);

                        while (System.IO.File.Exists(fullPath))
                        {
                            DateTime now = DateTime.Now;
                            string counter = now.ToString("fff~");
                            fullFileName = counter + orignialFullFileName;
                            fullPath = Path.Combine(StorageRoot + directory + "/", fullFileName);
                            //counter++;
                        }


                        var headers = httpRequest.Headers;

                        if (string.IsNullOrEmpty(headers["X-File-Name"]))
                        {
                            file.SaveAs(fullPath);
                        }
                        else
                        {
                            if (httpRequest.Files.Count != 1) throw new HttpRequestValidationException("Attempt to upload chunked file containing more than one fragment per request");
                            var inputStream = file.InputStream;
                            using (var fs = new FileStream(fullFileName, FileMode.Append, FileAccess.Write))
                            {
                                var buffer = new byte[1024];

                                var l = inputStream.Read(buffer, 0, 1024);
                                while (l > 0)
                                {
                                    fs.Write(buffer, 0, l);
                                    l = inputStream.Read(buffer, 0, 1024);
                                }
                                fs.Flush();
                                fs.Close();
                            }
                        }

                        statuses.Add(new ViewDataUploadFilesResult()
                        {
                            name = fullFileName,
                            prettyName = orignialFullFileName,
                            type = file.ContentType,
                            size = file.ContentLength,
                            progress = "1.0",
                            url = projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + fullFileName,
                            delete_url = projectConfigKeys.apiUrl + "/api/upload/deletefile?fileName=" + fullFileName + "&directory=" + directory,
                            delete_type = "DELETE",
                            hasCaption = hasCaption,
                            hasLink = hasLink,
                            // thumbnail_url = IsImage(fileExt)? @"data:image/png;base64," + EncodeFile(fullPath) : "",
                            thumbnail_url = IsImage(fileExt) ? projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + fullFileName : "",
                            hasDescription = hasDescription,
                            hasCheckbox = hasCheckbox,
                            inputName = inputName,
                            maxNumberOfFiles = maxNumberOfFiles,
                            directory = directory
                        });

                        if (!string.IsNullOrEmpty(fullFileName) && IsImage(fileExt))
                        //if (!string.IsNullOrEmpty(fullFileName))
                        {
                            GenerateImageInAllSizes(directory, fullFileName, directory);
                        }
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new { error = "This file type is not allowed" });
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new { error = "This file is too big" });
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, statuses);
        }


        public dynamic GetUploadedFilesOld(string files, string[] filesArray, string directory)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            var imgSrcFiles = new List<ViewDataUploadFilesResult>();
            var arrayOfFiles = filesArray ?? (files + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (arrayOfFiles.Count() > 0)
            {
                foreach (var item in arrayOfFiles)
                {
                    var fileVar = new FileInfo(StorageRoot + directory + "/" + item);
                    bool fileExists = fileVar.Exists;
                    imgSrcFiles.Add(new ViewDataUploadFilesResult
                    {
                        name = item,
                        prettyName = item.Contains("~") ? item.Split('~')[1] : item,
                        type = fileExists ? fileVar.GetType().ToString() : "",
                        size = fileExists ? (int)fileVar.Length : 0,
                        progress = "1.0",
                        url = projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + item,
                        delete_url = projectConfigKeys.apiUrl + "/api/upload/deletefile?fileName=" + item + "&directory=" + directory,
                        delete_type = "DELETE",
                        // thumbnail_url = IsImage(fileExt)? @"data:image/png;base64," + EncodeFile(fullPath) : "",
                        thumbnail_url = fileExists && IsImage(fileVar.Extension) ? projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + item : ""
                    });
                }
                return imgSrcFiles;
            }
            else
            {
                return "";
            }
        }

        public dynamic GetUploadedFiles(string files, List<MediaModel> filesArray, string directory,
            bool? hasCaption = false, bool? hasSubCaption = false, bool? hasDescription = false, bool? hasCheckbox = false, bool? hasLink = false)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            var imgSrcFiles = new List<ViewDataUploadFilesResult>();
            var arrayOfFiles = filesArray ?? (files + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(d => new MediaModel
            {
                mediaSrc = d
            });
            if (arrayOfFiles.Count() > 0)
            {
                foreach (var item in arrayOfFiles.Where(d => d.mediaSrc != null && d.mediaSrc != ""))
                {
                    var fileVar = new FileInfo(StorageRoot + directory + "/" + item.mediaSrc);
                    bool fileExists = fileVar.Exists;
                    imgSrcFiles.Add(new ViewDataUploadFilesResult
                    {
                        name = item.mediaSrc,
                        prettyName = item.mediaSrc.Contains("~") ? item.mediaSrc.Split('~')[1] : item.mediaSrc,
                        type = fileExists ? fileVar.GetType().ToString() : "",
                        size = fileExists ? (int)fileVar.Length : 0,
                        progress = "1.0",
                        url = projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + item.mediaSrc,
                        delete_url = projectConfigKeys.apiUrl + "/api/upload/deletefile?fileName=" + item.mediaSrc + "&directory=" + directory,
                        delete_type = "DELETE",
                        // thumbnail_url = IsImage(fileExt)? @"data:image/png;base64," + EncodeFile(fullPath) : "",
                        thumbnail_url = fileExists && IsImage(fileVar.Extension) ? projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + item.mediaSrc : "",
                        hasCaption = hasCaption.Value,
                        caption = item.caption,
                        hasSubCaption = hasSubCaption.Value,
                        subCaption = item.subCaption,
                        hasDescription = hasDescription.Value,
                        description = item.description,
                        hasCheckbox = hasCheckbox.Value,
                        hasLink = hasLink.Value,
                        link = item.link,
                    });
                }
                return imgSrcFiles;
            }
            else
            {
                return "";
            }
        }


        public void GenerateImageInAllSizes(string sourceDirectory, string imageName, string resizeName, bool forceUpdate = false)
        {
            var httpServer = HttpContext.Current.Server;
            if (!string.IsNullOrEmpty(resizeName))
            {
                if (imageName.Contains(sourceDirectory))
                {
                    imageName = imageName.Substring(sourceDirectory.Length);
                }
                imageName = imageName.Replace("/", "");

                var resizeRpstry = new ImageResizeRepository();
                var dbSizes = resizeRpstry.GetAllBySectionName(resizeName);
                sourceDirectory = ConfigurationManager.AppSettings["MediaFolder"].ToString() + "/" + sourceDirectory + "/";

                if (dbSizes.Any())
                {
                    foreach (var item in dbSizes)
                    {
                        string fitMode_Dir = httpServer.MapPath("~/" + ConfigurationManager.AppSettings["ResizedImagesBaseFolder"] + (item.width + "x" + item.height + "x" + (item.isInside ? "i" : "o") + "/"));
                        CreateDirectoryIfNotExists(fitMode_Dir);
                        if (!File.Exists(fitMode_Dir + imageName) || forceUpdate)
                        {
                            GenerateImage(fitMode_Dir, sourceDirectory, imageName, item.width, item.height, (item.isInside));
                        }
                    }
                }
                else
                {
                    string[] resizeAppSettings = ProjectKeysConfiguration.AllKeys.Where(x => x.ToString().ToLower().StartsWith(resizeName.ToLower() + "resize_")).ToArray();

                    for (int i = 0; i < resizeAppSettings.Length; i++)
                    {
                        ////////// "/231/x/131/x/outside/"
                        string width = ProjectKeysConfiguration[resizeAppSettings[i]].ToString().Split('x')[0]; // "/231/"
                        string height = ProjectKeysConfiguration[resizeAppSettings[i]].ToString().Split('x')[1]; // "/131/"
                        string fitMode = ProjectKeysConfiguration[resizeAppSettings[i]].ToString().Split('x')[2]; // "/outside/"

                        string fitMode_Dir = httpServer.MapPath("~/" + ConfigurationManager.AppSettings["ResizedImagesBaseFolder"] + (width.Replace("/", "") + "x" + height.Replace("/", "") + "x" + fitMode.Replace("/outside", "o").Replace("/inside", "i")));
                        CreateDirectoryIfNotExists(fitMode_Dir);

                        if (!File.Exists(fitMode_Dir + imageName) || forceUpdate)
                        {
                            GenerateImage(fitMode_Dir, sourceDirectory, imageName, width.Replace("/", ""), height.Replace("/", ""), fitMode.Replace("/", "") == "inside");
                        }
                    }
                }
            }
        }
        public void GenerateImage(string target_Dir, string sourceDirectory, string imageName, string width, string height, bool inside)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            var httpServer = HttpContext.Current.Server;
            double targetWidth = 0, targetHeight = 0;
            string location = sourceDirectory + imageName;
            if (height != null)
            {
                targetHeight = Convert.ToDouble(height);
            }
            if (width != null)
            {
                targetWidth = Convert.ToDouble(width);
            }
            double resizedWidth = targetHeight, resizedHeight = targetWidth;
            try
            {
                System.Drawing.Image oImg = System.Drawing.Image.FromFile(httpServer.MapPath("~/" + location));
                ImageFormat oFormat = getImageFormat(imageName);
                if (inside)
                {
                    if ((double)(oImg.Width / targetWidth) > (double)(oImg.Height / targetHeight))
                    {
                        int l2 = oImg.Width;
                        resizedWidth = targetWidth;
                        resizedHeight = oImg.Height * (resizedWidth / l2);
                        if (resizedHeight > targetHeight)
                        {
                            resizedWidth = resizedWidth * (targetHeight / resizedHeight);
                            resizedHeight = targetHeight;
                        }
                    }
                    else
                    {
                        int l2 = oImg.Height;
                        resizedHeight = targetHeight;
                        resizedWidth = oImg.Width * (targetHeight / l2);
                        if (resizedWidth > targetWidth)
                        {
                            resizedHeight = resizedHeight * (targetWidth / resizedWidth);
                            resizedWidth = targetWidth;
                        }
                    }
                }
                else
                {
                    if ((double)(oImg.Width / targetWidth) < (double)(oImg.Height / targetHeight))
                    {
                        int l2 = oImg.Width;
                        resizedWidth = targetWidth;
                        resizedHeight = oImg.Height * (resizedWidth / l2);
                        //if (resizedHeight > targetHeight)
                        //{
                        // resizedWidth = resizedWidth * (targetHeight / resizedHeight);
                        // resizedHeight = targetHeight;
                        //}
                    }
                    else
                    {
                        int l2 = oImg.Height;
                        resizedHeight = targetHeight;
                        resizedWidth = oImg.Width * (targetHeight / l2);
                        //if (resizedWidth > targetWidth)
                        //{
                        // resizedHeight = resizedHeight * (targetWidth / resizedWidth);
                        // resizedWidth = targetWidth;
                        //}
                    }
                }
                Bitmap oRectangle = new Bitmap((int)Math.Round(resizedWidth), (int)Math.Round(resizedHeight));//, PixelFormat.Format64bppPArgb); removed for png resizing*/
                Graphics oGraphic = Graphics.FromImage(oRectangle);

                oGraphic.CompositingQuality = CompositingQuality.HighQuality;

                oGraphic.SmoothingMode = SmoothingMode.AntiAlias;
                oGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

                oGraphic.DrawImage(oImg, 0, 0, oRectangle.Width, oRectangle.Height);
                MemoryStream stream = new MemoryStream();
                if (oFormat == ImageFormat.Jpeg)
                {
                    ImageCodecInfo destCodec = GetEncoder(ImageFormat.Jpeg);
                    EncoderParameters destEncParams = new EncoderParameters(1);

                    //Use quality parameter
                    EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
                    destEncParams.Param[0] = qualityParam;
                    if (destCodec != null)
                    {
                        //oRectangle.(saveDirectory + sPhysicalPath, destCodec, destEncParams);
                        //oRectangle.Save(Response.OutputStream, destCodec, destEncParams);
                        oRectangle.Save(target_Dir + imageName, destCodec, destEncParams);
                    }
                }
                else
                {

                    /*new for png resizing*/
                    // stream.WriteTo(Response.OutputStream);
                    BackgroundTaskManager.Run(async () =>
                    {
                        System.Drawing.Image oImg2 = System.Drawing.Image.FromFile(httpServer.MapPath("~/" + location));
                        Bitmap oRectangle2 = new Bitmap((int)Math.Round(resizedWidth), (int)Math.Round(resizedHeight));//, PixelFormat.Format64bppPArgb); removed for png resizing*/
                        Graphics oGraphic2 = Graphics.FromImage(oRectangle2);

                        oGraphic2.CompositingQuality = CompositingQuality.HighQuality;

                        oGraphic2.SmoothingMode = SmoothingMode.AntiAlias;
                        oGraphic2.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        oGraphic2.DrawImage(oImg2, 0, 0, oRectangle2.Width, oRectangle2.Height);


                        var resizeDir = width + "x" + height + "x" + (inside ? "i" : "o") + "/";
                        var waitingCompressionLocation = httpServer.MapPath("~/" + ConfigurationManager.AppSettings["ResizedImagesBaseFolder"] + "waitingcompression/" + resizeDir);
                        CreateDirectoryIfNotExists(waitingCompressionLocation);
                        //oRectangle.Save(Response.OutputStream, oFormat);
                        oRectangle2.Save(waitingCompressionLocation + imageName, oFormat);


                        using (var webClient = new WebClient())
                        {
                            try
                            {
                                var waitingCompressionImageUrl = projectConfigKeys.apiUrl + "images/waitingcompression/" + resizeDir + imageName;
                                var results = webClient.DownloadString("http://api.resmush.it/ws.php?qlty=90&img=" + waitingCompressionImageUrl);
                                var optImage = JsonConvert.DeserializeObject<MinifyImageModel>(results);

                                if (!String.IsNullOrEmpty(optImage.dest))
                                {
                                    DownloadPicture(optImage.dest, target_Dir + imageName);
                                    if (File.Exists(waitingCompressionLocation + imageName))
                                    {
                                        File.Delete(waitingCompressionLocation + imageName);
                                    }
                                }
                                else
                                {
                                    oRectangle2.Save(target_Dir + imageName, oFormat);
                                    if (File.Exists(waitingCompressionLocation + imageName))
                                    {
                                        File.Delete(waitingCompressionLocation + imageName);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                oRectangle2.Save(target_Dir + imageName, oFormat);
                                if (File.Exists(waitingCompressionLocation + imageName))
                                {
                                    File.Delete(waitingCompressionLocation + imageName);
                                }
                            }
                        }

                    });

                }

                //Response.ContentType = "image/Jpeg";
                oRectangle.Dispose();
                oImg.Dispose();
            }
            catch (Exception es)
            {

            }
        }

        public void DownloadPicture(string imageUrl, string destinationPath)
        {
            byte[] imageBytes;
            HttpWebRequest imageRequest = (HttpWebRequest)WebRequest.Create(imageUrl);
            WebResponse imageResponse = imageRequest.GetResponse();

            Stream responseStream = imageResponse.GetResponseStream();

            using (BinaryReader br = new BinaryReader(responseStream))
            {
                imageBytes = br.ReadBytes(500000);
                br.Close();
            }
            responseStream.Close();
            imageResponse.Close();

            FileStream fs = new FileStream(destinationPath, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            try
            {
                bw.Write(imageBytes);
            }
            finally
            {
                fs.Close();
                bw.Close();
            }
        }
        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static ImageFormat getImageFormat(string imageName)
        {
            if (imageName.ToLower().IndexOf(".jpg") >= 0 || imageName.ToLower().IndexOf(".jpeg") >= 0)
            {
                return ImageFormat.Jpeg;
            }
            else if (imageName.ToLower().IndexOf(".gif") >= 0)
            {
                return ImageFormat.Gif;
            }
            else if (imageName.ToLower().IndexOf(".tiff") >= 0)
            {
                return ImageFormat.Tiff;
            }
            else if (imageName.ToLower().IndexOf(".png") >= 0)
            {
                return ImageFormat.Png;
            }
            else if (imageName.ToLower().IndexOf(".bmp") >= 0)
            {
                return ImageFormat.Bmp;
            }
            else
            {
                return null;
            }
        }
        private void CreateDirectoryIfNotExists(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (IOException e)
            {
            }
        }


        [HttpGet]
        [AllowAnonymous]
        public void ResizeExistingImages(string sectionName, bool forceUpdate = false)
        {
            var httpServer = HttpContext.Current.Server;
            DirectoryInfo d = new DirectoryInfo(httpServer.MapPath("~/Content/uploads/" + sectionName));//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.*").OrderByDescending(f => f.LastWriteTime).ToArray(); //Getting ALL files
            if (d.Exists)
            {
                foreach (FileInfo file in Files)
                {
                    GenerateImageInAllSizes(sectionName, file.Name, sectionName, forceUpdate);
                }
            }
        }
    }

    public class ViewDataUploadFilesResult
    {
        public string group { get; set; }
        public string name { get; set; }
        public string prettyName { get; set; }
        public string type { get; set; }
        public int size { get; set; }
        public string progress { get; set; }
        public string url { get; set; }
        public string thumbnail_url { get; set; }
        public string delete_url { get; set; }
        public string delete_type { get; set; }
        public string error { get; set; }


        //public string textCaption { get; set; }


        #region Caption
        public bool hasCaption { get; set; }
        public string caption { get; set; }
        #endregion

        #region SubCaption
        public bool hasSubCaption { get; set; }
        public string subCaption { get; set; }
        #endregion

        #region Description
        public bool hasDescription { get; set; }
        public string description { get; set; }
        #endregion

        #region Checkbox
        public bool hasCheckbox { get; set; }
        public bool isCover { get; set; }
        #endregion

        #region Link
        public bool hasLink { get; set; }
        public string link { get; set; }
        #endregion

        public string inputName { get; set; }
        public int maxNumberOfFiles { get; set; }
        public string directory { get; set; }
    }

    public class MinifyImageModel
    {
        public string dest { get; set; }
        public string error_long { get; set; }
    }

    public class MediaModel
    {
        public string mediaSrc { get; set; }
        public string caption { get; set; }
        public string subCaption { get; set; }
        public string description { get; set; }
        public string link { get; set; }
    }
}