using System.Web.Http;
using Mikano_API.Models;
using System.Linq;
using System.Net.Http;
using System.Net;
using System;
using Microsoft.AspNet.Identity;
using System.Web.Http.ModelBinding;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using static Mikano_API.Models.KMSEnums;
using WebApi.OutputCache.V2;
using System.Web;
using System.Net.Http.Headers;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/settings")]
    public class SettingsController : SharedController<SocketHub>
    {
        private SettingsRepository rpstry = new SettingsRepository();
        private string kSectionName = "settings";
        private string kActionName = "";

        [HttpGet]
        public HttpResponseMessage GetById(int id)
        {
            UtilsHelper utilsHelper = new UtilsHelper();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToUpdate)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, "default", kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), false);
                    var entry = rpstry.GetFirstOrDefault();

                    var smsProviders = utilsHelper.GetEnumList<SmsProviders>().Select(d => new { id = d.Value + "", label = d.Key });

                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                            },
                            additionalData = new
                            {

                            }
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                shippingFee = entry.shippingFee,
                                voucherValidity = entry.voucherValidity,
                                giftCardExpiryDate = entry.giftCardExpiryDate,
                                giftCardMinAmount = entry.giftCardMinAmount,
                                loyaltyPointValue = entry.loyaltyPointValue,
                                defaultSendToEmail = entry.defaultSendToEmail,
                                receiptSendToEmail = entry.receiptSendToEmail,
                                tPPostFailureSendToEmail = entry.tPPostFailureSendToEmail,
                                defaultSendFromEmail = entry.defaultSendFromEmail
                            },
                            submissionOptions = new
                            {
                                saveAndStayHere = "saveAndStayHere"
                            },
                            additionalData = new
                            {
                                logs = new[] {
                                    new {
                                        label = "Last Modified on",
                                        value =entry.dateModified.ToString("dd MMM yyyy HH:mm tt")
                                    }
                                }
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, "default", kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpPost, HttpPut]
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, Setting entry)
        {

            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToUpdate)
            {
                if (entry.id == 0)
                {
                    ModelState.Remove("entry.id");
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            var oldEntry = rpstry.GetFirstOrDefault();
                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, entry.id);
                            }

                            oldEntry.shippingFee = entry.shippingFee;
                            oldEntry.voucherValidity = entry.voucherValidity;
                            oldEntry.defaultSendToEmail = entry.defaultSendToEmail;
                            oldEntry.receiptSendToEmail = entry.receiptSendToEmail;
                            oldEntry.tPPostFailureSendToEmail = entry.tPPostFailureSendToEmail;
                            oldEntry.defaultSendFromEmail = entry.defaultSendFromEmail;
                            oldEntry.giftCardExpiryDate = entry.giftCardExpiryDate;
                            oldEntry.giftCardMinAmount = entry.giftCardMinAmount;
                            oldEntry.loyaltyPointValue = entry.loyaltyPointValue;

                            oldEntry.dateModified = DateTime.Now;
                            entry = oldEntry;
                        }

                        rpstry.Save();
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), kSectionName, CollectRequestData(Request, entry), GetIpAddress(Request), false);
                        return Request.CreateResponse(HttpStatusCode.OK, new { id = entry.id });
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), kSectionName, CollectRequestData(Request, entry), GetIpAddress(Request), true);
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", ModelState.Values.SelectMany(d => d.Errors.Select(r => r.ErrorMessage))) });
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        #region frontend
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage RemoveCache()
        {
            RemoveStaticDataCache();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage SendEmail(string to)
        {
            var utilsHelper = new UtilsHelper();
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            string from = projectConfigKeys.projectName + "<" + settingsEntry.defaultSendFromEmail + ">";
            var results = utilsHelper.sendEmail(from, to, "", "", "hi", "helloooo", "");
            return Request.CreateResponse(HttpStatusCode.OK, results);
        }


        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetFile(string fileKey, string extension = "CSV")
        {
            try
            {
                UtilsHelper utilsHelper = new UtilsHelper();
                var dataToReturn = (byte[])utilsHelper.GetCacheData(fileKey);
                if (dataToReturn != null)
                {
                    var fileKeyParts = (utilsHelper.EDecryptString(fileKey) + "").Split(new string[] { "~$~" }, StringSplitOptions.RemoveEmptyEntries);

                    HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK);
                    result.Content = new ByteArrayContent(dataToReturn);
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                    result.Content.Headers.ContentDisposition.FileName = string.Format("{0}.{1}", fileKeyParts[0] + "-" + DateTime.Now, extension.ToLowerInvariant());
                    return result;
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
        #endregion
    }
}