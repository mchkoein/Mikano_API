using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Configuration;
using Mikano_API.Models;
using System.Net;
using System.Text;
//using System.Web.Script.Serialization;
using System.IO;

namespace Mikano_API.Models
{
    public class MailChimpAddOrUpdateMemberModel
    {
        public string id { get; set; }
        public string unique_email_id { get; set; }
        public string status { get; set; }
    }

    public class MailChimpErrorModel
    {
        public string field { get; set; }
        public string message { get; set; }
    }

    public class MailChimpFaildModel
    {
        public string type { get; set; }
        public string title { get; set; }
        public int status { get; set; }
        public string detail { get; set; }
        public string instance { get; set; }
        public IEnumerable<MailChimpErrorModel> errors { get; set; }

    }

    public class MailChimpUser
    {
        public string email { get; set; }
        public string euid { get; set; }
        public string leid { get; set; }
    }

    public partial class NewsletterUser
    {
        public string CategoryToRelateTo { get; set; }
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
    }

    public class NewsletterUserRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        //private string mailChimpListId = ConfigurationManager.AppSettings["MailChimpListId"];
        //private string mailChimpType = ConfigurationManager.AppSettings["MailChimpType"];
        //private string mailChimpKey = (ConfigurationManager.AppSettings["MailChimpApikey"]);
        private string komkastListId = (ConfigurationManager.AppSettings["komkastListId"]);

        public NewsletterUser GetByEmail(string email)
        {
            email = (email + "").Trim().ToLower();
            return db.NewsletterUsers.FirstOrDefault(d => d.email.ToLower() == email);
        }

        public IQueryable<NewsletterUser> GetAll()
        {
            var results = db.NewsletterUsers;
            return results.OrderByDescending(d => d.dateCreated);
        }


        public NewsletterUser GetById(int id)
        {
            return db.NewsletterUsers.FirstOrDefault(d => d.id == id);
        }

        public void Add(NewsletterUser entry)
        {
            db.NewsletterUsers.InsertOnSubmit(entry);
        }


        public void Delete(NewsletterUser entry)
        {
            db.NewsletterUsers.DeleteOnSubmit(entry);
        }

        public void Save()
        {
            db.SubmitChanges();
        }

        #region newsletter mailchimp
        //public string subscribe(string email, string name, string countryName, string businessField, bool double_optin, bool send_welcome)
        //{
        //    try
        //    {
        //        using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
        //        {
        //            string key = mailChimpKey.Split('-')[1];
        //            var link = "https://" + key + ".api.mailchimp.com/2.0/lists/subscribe?apikey=" + mailChimpKey +
        //                        "&email[email]=" + email +
        //                        "&merge_vars[FULLNAME]=" + name +
        //                        "&merge_vars[COUNTRY]=" + countryName +
        //                        "&merge_vars[BUSINFIELD]=" + businessField +
        //                        "&id=" + mailChimpListId + "&double_optin=" + double_optin;

        //            string content = webClient.DownloadString(link);
        //            try
        //            {
        //                MailChimpUser jsonObj = new JavaScriptSerializer().Deserialize<MailChimpUser>(content);
        //                return jsonObj.euid;

        //            }
        //            catch (Exception e)
        //            {
        //                return "failure";
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return "failure";
        //    }
        //}

        //public void unsubscribe(string email)
        //{
        //    try
        //    {
        //        string key = mailChimpKey.Split('-')[1];
        //        var link = "https://" + key + ".api.mailchimp.com/2.0/lists/unsubscribe?apikey=" + mailChimpKey + "&id=" + mailChimpListId + "&email[email]=" + email;
        //        link = link + "&send_goodbye=false&send_notify=false";

        //        using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
        //        {
        //            string content = webClient.DownloadString(link);
        //        }
        //    }
        //    catch (Exception e)
        //    {

        //    }
        //}
        #endregion

        #region newsletter KomKast
        public void SubscribeToKomKast(string email, string name)
        {
            try
            {
                string responseCode = "";
                var url = "http://send.komkast.com/subscribe";
                var data = "boolean=true"
                    + "&name=" + name
                    + "&email=" + email
                    + "&list=" + komkastListId
                    + "&method=PUT";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);

                // Create a request using a URL that can receive a post. 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "application/xml";
                WebHeaderCollection headerCollection = request.Headers;
                headerCollection.Add("Accept-Charset", "utf-8");
                request.Method = "POST";
                request.ContentLength = buffer.Length;

                System.IO.Stream putData = request.GetRequestStream();
                putData.Write(buffer, 0, buffer.Length);
                putData.Close();

                #region GET Example
                var response = (HttpWebResponse)request.GetResponse();

                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    responseCode = sr.ReadToEnd();
                }
                #endregion

            }
            catch (Exception e)
            {
            }
        }

        public void UnsubscribeFromKomKast(string email)
        {
            try
            {
                string responseCode = "";
                var url = "http://send.komkast.com/unsubscribe";
                var data = "boolean=true"
                    + "&email=" + email
                    + "&list=" + komkastListId
                    + "&method=PUT";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);

                // Create a request using a URL that can receive a post. 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "application/xml";
                WebHeaderCollection headerCollection = request.Headers;
                headerCollection.Add("Accept-Charset", "utf-8");
                request.Method = "POST";
                request.ContentLength = buffer.Length;

                Stream putData = request.GetRequestStream();
                putData.Write(buffer, 0, buffer.Length);
                putData.Close();

                #region GET Example
                var response = (HttpWebResponse)request.GetResponse();

                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    responseCode = sr.ReadToEnd();
                }
                #endregion

            }
            catch (Exception e)
            {
            }
        }
        #endregion

    }
}