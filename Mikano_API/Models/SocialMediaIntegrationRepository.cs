using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Mikano_API.Models
{
    #region Models

    #region Instagram
    public class InstagramModel
    {
        public IEnumerable<InstagramMedia> data;
    }

    public class InstagramMedia
    {
        public string type;
        public string link;
        public InstagramImages images;
    }

    public class InstagramImages
    {
        public InstagramImageEntry low_resolution;
        public InstagramImageEntry thumbnail;
        public InstagramImageEntry standard_resolution;
    }

    public class InstagramImageEntry
    {
        public string url;
        public string width;
        public string height;
    }
    #endregion

    #endregion


}