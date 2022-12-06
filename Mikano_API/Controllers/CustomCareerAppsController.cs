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
using System.Configuration;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/CustomCareerApps")]
    public class CustomCareerAppsController : SharedController<SocketHub>
    {
        private CustomCareerAppRepository rpstry = new CustomCareerAppRepository();
        private string kSectionName = "CustomCareerApps";
        private string kActionName = "";


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
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    return Request.CreateResponse(HttpStatusCode.OK,
                        rpstry.GetAll().Select(d => new
                        {
                            id = d.id,
                            careerTitle = d.Career.title,
                            careerId = d.careerId,
                            fullName = string.IsNullOrEmpty(d.fullName) ? d.FullName : d.fullName,
                            mobile = d.mobile,
                            email = d.email,
                            currentRole = d.currentRole,
                            yearsOfExperience = d.yearsOfExperience,
                            priority = d.priority,
                            dateCreated = d.dateCreated,
                            dateModified = d.dateModified
                        }).ToDataSourceResult(request)
                    );
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetById(int id)
        {
            CountryRepository countryRpstry = new CountryRepository();
            ContactTitleRepository contactTitlesRpstry = new ContactTitleRepository();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();

            loggedInUserId = User.Identity.GetUserId();
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);

            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(id);
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.FullName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    #region contactTitles
                    var contactTitles = contactTitlesRpstry.GetAllIsPublished().Select(d => new
                    {
                        id = d.id + "",
                        label = d.title
                    });
                    #endregion

                    #region countries
                    var countries = countryRpstry.GetAll().Select(d => new
                    {
                        id = d.id + "",
                        label = d.name
                    });
                    #endregion

                    #region Create
                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                            },
                            additionalData = new
                            {
                                contactTitles = contactTitles,
                                countries = countries,
                            }
                        });
                    }
                    #endregion

                    #region Update
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                id = entry.id,
                                careerTitle = entry.Career.title,
                                careerId = entry.careerId,
                                contactTitleId = entry.contactTitleId + "",
                                firstName = entry.firstName,
                                lastName = entry.lastName,
                                fullName = entry.fullName,
                                mobile = entry.mobile,
                                email = entry.email,
                                currentRole = entry.currentRole,
                                yearsOfExperience = entry.yearsOfExperience,
                                workedInFieldBefore = entry.workedInFieldBefore,
                                workedWithusBefore = entry.workedwithusBefore,
                                coverLetter = entry.coverLetter,
                                fileSrc = new UploadController().GetUploadedFiles(entry.fileSrc, null, CustomCareerAppsDirectory),
                                priority = entry.priority,
                            },
                            actions = new[] {
                                new {
                                    label = "Reply",
                                    link = "mailTo:" + entry.email,
                                    inEditModeOnly = true
                                }
                            },
                            additionalData = new
                            {
                                contactTitles = contactTitles,
                                countries = countries,
                                externalLinks = new[] {
                                    new
                                    {
                                        title = "Related Vacancy",
                                        fields = new[]
                                        {
                                            new {
                                                label = entry.Career.title,
                                                subLabel = "",
                                                link = projectConfigKeys.kmsUrl + "careers/FormPage/" +  entry.careerId
                                            },
                                        }
                                    }
                                },
                                infoBarBlocks = new[] {
                                    new
                                    {
                                        title = "Logs",
                                        fields = new[]
                                        {
                                            new {label = "Created on", value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")},
                                            new {label = "Modified on", value = entry.dateModified.ToString("dd MMM yyyy HH:mm tt")},
                                        }
                                    }
                                },
                            }
                        });
                    }
                    #endregion
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpPost, HttpPut]
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, CustomCareerApp entry, SubmissionOptions submissionType, bool inline = false)
        {

            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                if (entry.id == 0)
                {
                    ModelState.Remove("entry.id");
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        #region Create
                        if (entry.id == 0 && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            entry.dateCreated = DateTime.Now;
                            entry.dateModified = DateTime.Now;
                            entry.priority = rpstry.GetMaxPriority() + 1;

                            rpstry.Add(entry);
                        }
                        #endregion

                        #region Update
                        else if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            var oldEntry = rpstry.GetById(entry.id);

                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, entry.id);
                            }

                            oldEntry.careerId = entry.careerId;
                            oldEntry.contactTitleId = entry.contactTitleId;
                            oldEntry.firstName = entry.firstName;
                            oldEntry.lastName = entry.lastName;
                            oldEntry.fullName = entry.fullName;
                            oldEntry.mobile = entry.mobile;
                            oldEntry.email = entry.email;
                            oldEntry.currentRole = entry.currentRole;
                            oldEntry.yearsOfExperience = entry.yearsOfExperience;
                            oldEntry.workedInFieldBefore = entry.workedInFieldBefore;
                            oldEntry.workedwithusBefore = entry.workedwithusBefore;
                            oldEntry.coverLetter = entry.coverLetter;
                            oldEntry.fileSrc = entry.fileSrc;
                            oldEntry.dateModified = DateTime.Now;
                            entry = oldEntry;
                        }
                        #endregion

                        rpstry.Save();

                        if (submissionType == SubmissionOptions.saveAndGoToNext)
                        {
                            entry = rpstry.GetNextEntry(entry);
                        }

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), entry.FullName, CollectRequestData(Request, entry), GetIpAddress(Request), false);

                        if (inline)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new[] {
                                new {
                                    id = entry.id,
                                    firstName = entry.firstName,
                                    lastName = entry.lastName,
                                    contactTitleId = entry.contactTitleId,
                                    priority = entry.priority,
                                    dateCreated = entry.dateCreated,
                                    dateModified = entry.dateModified
                                } }.ToDataSourceResult(request));
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new { id = entry.id });
                        }
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), entry.FullName, CollectRequestData(Request, entry), GetIpAddress(Request), true);
                        return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
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

        [HttpPost]
        public string SortGrid(SortGridBindingModel model)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissions)
            {
                kActionName = KActions.sort.ToString();
                try
                {
                    var entry = rpstry.GetById(model.id);
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, model.id.ToString(), entry.FullName, CollectRequestData(Request, model), GetIpAddress(Request), false);
                    var result = rpstry.SortGrid(model.newIndex, model.oldIndex, model.id);

                    return result;
                }
                catch (Exception e)
                {

                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, model.id.ToString(), kSectionName, CollectRequestData(Request, model), GetIpAddress(Request), true);
                    return "failure";
                }
            }
            else
            {
                return "failure";
            }
        }

        [HttpDelete]
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
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.FullName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, "");
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        #region Front End

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetContactData()
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            var settingRpstry = new SettingsRepository();
            var settingsEntry = settingRpstry.GetFirstOrDefault();

            return Request.CreateResponse(HttpStatusCode.OK,
                new
                {
                    //supportPhone = settingsEntry.supportPhone,
                }
            );
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetInquiryData()
        {
            CountryRepository countryRpstry = new CountryRepository();
            ContactTitleRepository contactTitlesRpstry = new ContactTitleRepository();

            return Request.CreateResponse(HttpStatusCode.OK,
                new
                {
                    titles = contactTitlesRpstry.GetAllIsPublished().Select(d => new
                    {
                        id = d.id,
                        label = d.title
                    }),
                    countries = countryRpstry.GetAll().Select(d => new
                    {
                        id = d.id,
                        label = d.name
                    })
                }
        );
        }

        //[HttpPost]
        //[AllowAnonymous]
        //public HttpResponseMessage SendRequest(CustomCareerApp entry)
        //{
        //	if (ModelState.IsValid)
        //	{
        //		try
        //		{
        //			ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
        //			UtilsHelper utilsHelper = new UtilsHelper();
        //			EmailTemplateRepository emailTemplateRpstry = new EmailTemplateRepository();
        //			SettingsRepository settingsRpstry = new SettingsRepository();

        //			entry.dateCreated = DateTime.Now;
        //			entry.dateModified = DateTime.Now;
        //			rpstry.Add(entry);
        //			rpstry.Save();

        //			#region Send Email to Admin 
        //			try
        //			{
        //				ContactInfoRepository contactRpstry = new ContactInfoRepository();
        //				var contactInfo = contactRpstry.GetFirst();

        //				var emailTemplateObj = emailTemplateRpstry.GetById(projectConfigKeys.emailTpContactUsAdminId);
        //				var emailTemplate = emailTemplateObj.description
        //					.Replace("%userName%", string.IsNullOrEmpty(entry.fullName) ? entry.FullName : entry.fullName)
        //					.Replace("%userEmail%", entry.email)
        //					.Replace("%userMobile%", entry.mobile)
        //					.Replace("%inquirySubject%", entry.InquiryType.title)
        //					.Replace("%inquiryMessage%", entry.message)
        //					.Replace("%inquiryLink%", projectConfigKeys.kmsUrl + "ekomproductinquiries/FormPage/" + entry.id)
        //					.Replace("%phone%", contactInfo.phone)
        //					.Replace("%email%", contactInfo.email);

        //				string from = projectConfigKeys.projectName + "<" + settingsEntry.defaultSendFromEmail + ">";
        //				string to = settingsRpstry.GetFirstOrDefault().defaultSendToEmail;
        //				string subject = projectConfigKeys.projectName + " - " + emailTemplateObj.subject;

        //				utilsHelper.sendEmail(from, to, "", "", subject, emailTemplate, "");
        //			}
        //			catch (Exception ee) { }
        //			#endregion

        //			#region Send Email to User 
        //			try
        //			{
        //				ContactInfoRepository contactRpstry = new ContactInfoRepository();
        //				var contactInfo = contactRpstry.GetFirst();

        //				var emailTemplateObj = emailTemplateRpstry.GetById(projectConfigKeys.emailTpContactUsId);
        //				var emailTemplate = emailTemplateObj.description
        //					.Replace("%websitelink%", projectConfigKeys.frontUrl)
        //					.Replace("%phone%", contactInfo.phone)
        //					.Replace("%email%", contactInfo.email);

        //				string from = projectConfigKeys.projectName + "<" + settingsEntry.defaultSendFromEmail + ">";
        //				string to = entry.email;
        //				string subject = projectConfigKeys.projectName + " - " + emailTemplateObj.subject;

        //				utilsHelper.sendEmail(from, to, "", "", subject, emailTemplate, "");
        //			}
        //			catch (Exception ee) { }
        //			#endregion

        //			return Request.CreateResponse(HttpStatusCode.OK);
        //		}
        //		catch (Exception e)
        //		{
        //			return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = e.Message });
        //		}
        //	}
        //	else
        //	{
        //		return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", ModelState.Values.SelectMany(d => d.Errors.Select(r => r.ErrorMessage))) });
        //	}
        //}
        #endregion
    }

}