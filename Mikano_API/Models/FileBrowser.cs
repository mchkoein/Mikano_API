namespace Mikano_API.Models
{

    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Web;
    using System;
    using System.Linq;


    public enum EntryType
    {
        File = 0,
        Directory
    }
    public class FileBrowserEntry
    {
        public string Name { get; set; }
        public EntryType Type { get; set; }
        public long Size { get; set; }
    }
    public class ImageSize
    {
        public int Height
        {
            get;
            set;
        }

        public int Width
        {
            get;
            set;
        }
    }


    public class ContentInitializer
    {
        private string rootFolder;
        private string[] foldersToCopy;
        private string prettyName;

        public ContentInitializer(string rootFolder, string[] foldersToCopy, string prettyName)
        {
            this.rootFolder = rootFolder;
            this.foldersToCopy = foldersToCopy;
            this.prettyName = prettyName;
        }

        private string UserID
        {
            get
            {
                var obj = HttpContext.Current.Session["UserID"];
                if (obj == null)
                {
                    HttpContext.Current.Session["UserID"] = obj = DateTime.Now.Ticks.ToString();
                }
                return (string)obj;
            }
        }


        public string CreateFolder(System.Web.HttpServerUtilityBase server)
        {
            var virtualPath = Path.Combine(rootFolder, "editor");

            var path = server.MapPath(virtualPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                //foreach (var sourceFolder in foldersToCopy)
                //{
                //    CopyFolder(server.MapPath(sourceFolder), path);
                //}
            }
            return virtualPath;
        }
        //public string CreateUserFolder(System.Web.HttpServerUtilityBase server)
        //{
        //    var virtualPath = Path.Combine(rootFolder, Path.Combine("UserFiles", UserID), prettyName);

        //    var path = server.MapPath(virtualPath);
        //    if (!Directory.Exists(path))
        //    {
        //        Directory.CreateDirectory(path);
        //        foreach (var sourceFolder in foldersToCopy)
        //        {
        //            CopyFolder(server.MapPath(sourceFolder), path);
        //        }
        //    }
        //    return virtualPath;
        //}

        private void CopyFolder(string source, string destination)
        {
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }

            foreach (var file in Directory.EnumerateFiles(source))
            {
                var dest = Path.Combine(destination, Path.GetFileName(file));
                System.IO.File.Copy(file, dest);
            }

            foreach (var folder in Directory.EnumerateDirectories(source))
            {
                var dest = Path.Combine(destination, Path.GetFileName(folder));
                CopyFolder(folder, dest);
            }
        }
    }

    public class DirectoryBrowser
    {
        public IEnumerable<FileBrowserEntry> GetContent(string path, string filter)
        {
            return GetFiles(path, filter).Concat(GetDirectories(path));
        }

        private IEnumerable<FileBrowserEntry> GetFiles(string path, string filter)
        {
            var directory = new DirectoryInfo(Server.MapPath(path));

            var extensions = (filter ?? "*").Split(",|;".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);

            return extensions.SelectMany(directory.GetFiles)
                .Select(file => new FileBrowserEntry
                {
                    Name = file.Name,
                    Size = file.Length,
                    Type = EntryType.File
                });
        }

        private IEnumerable<FileBrowserEntry> GetDirectories(string path)
        {
            var directory = new DirectoryInfo(Server.MapPath(path));

            return directory.GetDirectories()
                .Select(subDirectory => new FileBrowserEntry
                {
                    Name = subDirectory.Name,
                    Type = EntryType.Directory
                });
        }

        public System.Web.HttpServerUtilityBase Server { get; set; }
    }


    public class ImageResizer
    {
        public ImageSize Resize(ImageSize originalSize, ImageSize targetSize)
        {
            var aspectRatio = (float)originalSize.Width / (float)originalSize.Height;
            var width = targetSize.Width;
            var height = targetSize.Height;

            if (originalSize.Width > targetSize.Width || originalSize.Height > targetSize.Height)
            {
                if (aspectRatio > 1)
                {
                    height = (int)(targetSize.Height / aspectRatio);
                }
                else
                {
                    width = (int)(targetSize.Width * aspectRatio);
                }
            }
            else
            {
                width = originalSize.Width;
                height = originalSize.Height;
            }

            return new ImageSize
            {
                Width = Math.Max(width, 1),
                Height = Math.Max(height, 1)
            };
        }
    }


    public class ThumbnailCreator
    {
        private static readonly IDictionary<string, ImageFormat> ImageFormats = new Dictionary<string, ImageFormat>{
            {"image/png", ImageFormat.Png},
            {"image/gif", ImageFormat.Gif},
            {"image/jpeg", ImageFormat.Jpeg}
        };

        private readonly ImageResizer resizer;

        public ThumbnailCreator()
        {
            this.resizer = new ImageResizer();
        }

        public byte[] Create(Stream source, ImageSize desiredSize, string contentType)
        {
            using (var image = Image.FromStream(source))
            {
                var originalSize = new ImageSize
                {
                    Height = image.Height,
                    Width = image.Width
                };

                var size = resizer.Resize(originalSize, desiredSize);

                using (var thumbnail = new Bitmap(size.Width, size.Height))
                {
                    ScaleImage(image, thumbnail);

                    using (var memoryStream = new MemoryStream())
                    {
                        thumbnail.Save(memoryStream, ImageFormats[contentType]);

                        return memoryStream.ToArray();
                    }
                }
            }
        }

        public byte[] CreateFill(Stream source, ImageSize desiredSize, string contentType)
        {
            using (var image = Image.FromStream(source))
            {
                using (var memoryStream = new MemoryStream())
                {
                    FixedSize(image, desiredSize.Width, desiredSize.Height, true).Save(memoryStream, ImageFormats[contentType]);
                    return memoryStream.ToArray();
                }
            }
        }

        private void ScaleImage(Image source, Image destination)
        {
            using (var graphics = Graphics.FromImage(destination))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                graphics.DrawImage(source, 0, 0, destination.Width, destination.Height);
            }
        }


        private Image FixedSize(Image imgPhoto, int Width, int Height, bool needToFill)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)Width / (float)sourceWidth);
            nPercentH = ((float)Height / (float)sourceHeight);
            if (!needToFill)
            {
                if (nPercentH < nPercentW)
                {
                    nPercent = nPercentH;
                }
                else
                {
                    nPercent = nPercentW;
                }
            }
            else
            {
                if (nPercentH > nPercentW)
                {
                    nPercent = nPercentH;
                    destX = (int)Math.Round((Width -
                        (sourceWidth * nPercent)) / 2);
                }
                else
                {
                    nPercent = nPercentW;
                    destY = (int)Math.Round((Height -
                        (sourceHeight * nPercent)) / 2);
                }
            }

            if (nPercent > 1)
                nPercent = 1;

            int destWidth = (int)Math.Round(sourceWidth * nPercent);
            int destHeight = (int)Math.Round(sourceHeight * nPercent);

            System.Drawing.Bitmap bmPhoto = new System.Drawing.Bitmap(
                destWidth <= Width ? destWidth : Width,
                destHeight < Height ? destHeight : Height,
                              PixelFormat.Format32bppRgb);

            System.Drawing.Graphics grPhoto = System.Drawing.Graphics.FromImage(bmPhoto);
            grPhoto.Clear(System.Drawing.Color.White);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
            grPhoto.CompositingQuality = CompositingQuality.HighQuality;
            grPhoto.SmoothingMode = SmoothingMode.AntiAlias;

            grPhoto.DrawImage(imgPhoto,
                new System.Drawing.Rectangle(destX, destY, destWidth, destHeight),
                new System.Drawing.Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                System.Drawing.GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }
    }
}