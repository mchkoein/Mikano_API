using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web;
using Mikano_API.Helpers;

namespace Mikano_API.Models
{
    public class SharedRepository
    {
        public string GetUrlTitle(string title)
        {
            return title.Replace(",", "-").Replace("<br/>", "-").Replace("<br>", "-").Replace("!", "").Replace(":", "-").Replace("'", "").Replace(" ", "-").Replace("&", "").Replace("?", "").Replace("/", "-").Replace("*", "-").Replace(".", "").Replace("--", "-").ToLower();
        }

    }
}