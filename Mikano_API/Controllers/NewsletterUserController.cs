using System.Web.Http;
using Mikano_API.Models;
using System.Linq;
using System.Net.Http;
using System.Net;
using System;
using System.Web.Http.ModelBinding;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/newsletteruser")]
    public class newsletteruserController : SharedController<SocketHub>
    {
        private NewsletterUserRepository rpstry = new NewsletterUserRepository();
        private string kSectionName = "newsletteruser";
        private string kActionName = "";

        #region backend
        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.read);
            if (hasPermissions)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    var results = rpstry.GetAll();

                    //li badon ybayno bel grid usually subtitle title image 

                    return Request.CreateResponse(HttpStatusCode.OK,
                results.Select(d => new
                {
                    id = d.id,
                    fullName = d.firstName + " " + d.lastName,
                    email = d.email,
                    mobile = d.mobile,
                    phone = d.phone,
                    gender = d.gender,
                    country = d.country.HasValue ? d.Country1.name : "",
                    dateCreated = d.dateCreated,
                }).ToDataSourceResult(request)
            );
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        public HttpResponseMessage Delete(string ids = "")
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.delete);
            if (hasPermissions)
            {
                kActionName = KActions.delete.ToString();
                try
                {
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        rpstry.Delete(entry);
                        rpstry.Save();
                        //UpdatePermissions();
                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, parId, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), false);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "");
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }
        #endregion

        #region front end
        [AllowAnonymous]
        [HttpPost]
        public HttpResponseMessage SubscribeToNewsletter(NewsletterUser entry)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var newsLetterEntry = rpstry.GetByEmail(entry.email);
                    if (newsLetterEntry != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "You are already subscribed to our newsletter" });
                    }

                    entry.dateCreated = DateTime.Now;
                    entry.dateModified = DateTime.Now;


                    rpstry.Add(entry);
                    rpstry.Save();

                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                catch (Exception e)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", ModelState.Values.SelectMany(d => d.Errors.Select(r => r.ErrorMessage))) });
            }
            #endregion

        }
    }
}
