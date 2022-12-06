using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Mikano_API.Models
{
    public class SharedModel
    {
        internal NameValueCollection ProjectKeysConfiguration = (NameValueCollection)ConfigurationManager.GetSection("ProjectKeysConfig");
    }

    public class SharedControllerModel
    {
        public string id { get; set; }
        public string label { get; set; }
    }

    public class NavigationModel
    {
        public bool canGoToFirst { get; set; }
        public bool canGoToNext { get; set; }
        public bool canGoToPrev { get; set; }
        public bool canGoToLast { get; set; }
    }
}