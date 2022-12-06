using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Xml;
using System.Text;
using System.IO;
using System.Configuration;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Mail;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.Routing;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Runtime.Caching;
using Mikano_API.Models;
using static Mikano_API.Models.KMSEnums;
using System.Net.Security;
using Mikano_API.EtisalatMEssagingReference;
using Facebook;
using Newtonsoft.Json;

namespace Mikano_API.Helpers
{
    public class MembershipHelper
    {
        public ExternalCustomLoginModel FacebookExternalLogin(string accessToken)
        {
            ExternalCustomLoginModel externalUserObj = null;
            try
            {
                var client = new FacebookClient(accessToken);
                externalUserObj = JsonConvert.DeserializeObject<ExternalCustomLoginModel>(client.Get("me", new { fields = "id,email,first_name,last_name,picture{url},birthday" }).ToString());
            }
            catch (Exception e)
            { }

            return externalUserObj;
        }

        public GoogleAccountModel GoogleExternalLogin(string accessToken)
        {
            GoogleAccountModel externalUserObj = null;
            try
            {
                using (var client = new WebClient())
                {
                    string googleApiTokenInfoUrl = ConfigurationManager.AppSettings["GoogleApiTokenInfoUrl"];
                    var values = new NameValueCollection();
                    values["id_token"] = accessToken;
                    var response = client.UploadValues(googleApiTokenInfoUrl, values);
                    var stringData = Encoding.Default.GetString(response);
                    externalUserObj = JsonConvert.DeserializeObject<GoogleAccountModel>(stringData);
                }
            }
            catch (Exception e)
            { }
            return externalUserObj;
        }

    }
}