using System.Web.Http;
using Mikano_API.Models;
using System.Linq;
using System.Net.Http;
using System.Net;
using System;
using Mikano_API;
using System.Web.Http.ModelBinding;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using static Mikano_API.Models.KMSEnums;
using System.Web;
using System.Net.Http.Formatting;
using System.Configuration;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    [RoutePrefix("api/contactus")]
    public class ContactUsController : SharedController<SocketHub>
    {

        [HttpGet]
        public HttpResponseMessage GetAll()
        {
            InquiryRepository inqRpstry = new InquiryRepository();
            dblinqDataContext db = new dblinqDataContext();
            //logRpstry.AddLog(new Guid(User.Identity.GetUserId()), "Get KMS Section", "GetAll", "read");
            var contactId = Convert.ToInt32(ConfigurationManager.AppSettings["contact-corporate-id"]);
            var contact = db.CorporatePages.Where(d => d.isPublished && !d.isDeleted && d.id == contactId);

            var model = new
            {
                contactImage = contact.Select(d => new
                {
                    imgSrc = d.imgSrc,

                }).SingleOrDefault(),
                contactInfo = db.ContactInfos.Where(d => d.isPublished && !d.isDeleted).OrderByDescending(d => d.priority).Select(d => new
                {
                    UrlTitle = inqRpstry.GetUrlTitle(d.title),
                    email = d.email,
                    fax = d.fax,
                    googleMapLatitude = d.googleMapLatitude,
                    googleMapLongitude = d.googleMapLongitude,
                    id = d.id,
                    mainTitle = d.mainTitle,
                    phone = d.phone,
                    title = d.title
                })
            };

            return Request.CreateResponse(HttpStatusCode.OK, model);
        }

        public HttpResponseMessage GetInquiryTypes()
        {
            dblinqDataContext db = new dblinqDataContext();
            var model = new
            {
                InquiryTypes = db.InquiryTypes.Where(d => !d.isDeleted && d.isPublished).OrderBy(d => d.title).Select(d => new
                {
                    title = d.title,
                    id = d.id
                })
            };

            return Request.CreateResponse(HttpStatusCode.OK, model);
        }

        public string Send(Inquiry item)
        {
            InquiryRepository inqRpstry = new InquiryRepository();
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            UtilsHelper utilsHelper = new UtilsHelper();
            try
            {
                item.isPublished = true;
                item.priority = inqRpstry.GetMaxPriority() + 1;
                item.dateCreated = DateTime.Now;
                item.dateModified = DateTime.Now;

                inqRpstry.Add(item);
                inqRpstry.Save();

                try
                {
                    using (var client = new WebClient())
                    {
                        client.Encoding = System.Text.Encoding.UTF8;
                        var emailTemplate = client.DownloadString(projectConfigKeys.apiUrl + "/emails/EmailContact?id=" + item.id);

                        string from = projectConfigKeys.projectName + "<" + settingsEntry.defaultSendFromEmail + ">";
                        string to = ConfigurationManager.AppSettings["ContactToEmail"];
                        string subject = projectConfigKeys.projectName + ConfigurationManager.AppSettings["ContactSubject"];

                        utilsHelper.sendEmail(from, to, "", "", subject, emailTemplate, "");
                        client.Dispose();
                    }
                }
                catch (Exception ee) { }

                return "success";
            }
            catch (Exception e)
            {
                return "fail";
            }
        }

    }
}