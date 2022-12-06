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

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/SocialMedias")]
    public class SocialMediasController : SharedController<SocketHub>
    {
        private SocialMediaRepository rpstry = new SocialMediaRepository();
        private string kSectionName = "socialmedias";
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
                            title = d.title,
                            priority = d.priority,
                            isPublished = d.isPublished,
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
        public HttpResponseMessage GetById(int id, int? languageId = null)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    SocialMedia oldEntry = null;
                    var entry = oldEntry = rpstry.GetById(id);
                    var languageDirectionIsRtl = false;
                    int? languageParentId = null;
                    string languageTitle = null;

                    if (languageId.HasValue)
                    {
                        if (entry != null)
                        {
                            languageParentId = entry.id;
                        }
                        entry = entry.SocialMedias.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
                        var languageEntry = languageRpstry.GetById(languageId.Value);
                        languageTitle = languageEntry.title;
                        languageDirectionIsRtl = languageEntry.isRightToLeft;
                    }

                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);



                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                isPublished = "False",
                                languageId = languageId,
                                languageParentId = languageParentId
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                languageTitle = languageTitle,
                                languageDirectionIsRtl = languageDirectionIsRtl
                            }
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                id = entry.id,
                                title = entry.title,
                                icon = entry.icon,
                                link = entry.link,
                                priority = entry.priority,
                                isPublished = entry.isPublished + ""
                            },
                            languages = languageRpstry.GetAllOptional().Select(e => new
                            {
                                id = e.id,
                                title = e.title,
                                isRightToLeft = e.isRightToLeft,
                                isFilled = entry.SocialMedias.Any(d => !d.isDeleted && d.languageId == e.id)
                            }),
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                logs = new[] {
                                    new {label = "Created on",value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")},
                                    new {label = "Last Modified on",value =entry.dateModified.ToString("dd MMM yyyy HH:mm tt")}
                                },
                                languageTitle = languageTitle,
                                languageDirectionIsRtl = languageDirectionIsRtl
                            }
                        });
                    }
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, SocialMedia entry, SubmissionOptions submissionType, bool inline = false)
        {

            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                if (entry.id == 0)
                {
                    ModelState.Remove("entry.id");
                }
                if (entry.languageId.HasValue)
                {
                    ModelState.Remove("entry.link");
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        bool imageHasBeenChanged = false;

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

                            oldEntry.title = entry.title;
                            oldEntry.link = entry.link;
                            oldEntry.icon = entry.icon;
                            oldEntry.isPublished = entry.isPublished;
                            oldEntry.dateModified = DateTime.Now;
                            entry = oldEntry;

                        }
                        #endregion

                        if (submissionType == SubmissionOptions.saveAndPublish)
                        {
                            entry.isPublished = true;
                        }
                        rpstry.Save();

                        if (submissionType == SubmissionOptions.saveAndGoToNext)
                        {
                            entry = rpstry.GetNextEntry(entry);
                        }

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), entry.title, CollectRequestData(Request, entry), GetIpAddress(Request), false);

                        if (inline)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new[] {
                                new {
                                    id = entry.id,
                                    title = entry.title,
                                    priority = entry.priority,
                                    isPublished = entry.isPublished,
                                    dateCreated = entry.dateCreated,
                                    dateModified = entry.dateModified
                                } }.ToDataSourceResult(request));
                        }
                        else
                        {

                            return Request.CreateResponse(HttpStatusCode.OK, new
                            {
                                id = entry.id,
                                languages = languageRpstry.GetAllOptional().Select(e => new
                                {
                                    id = e.id,
                                    title = e.title,
                                    isRightToLeft = e.isRightToLeft,
                                    isFilled = (entry.SocialMedia1 ?? entry).SocialMedias.Any(d => !d.isDeleted && d.languageId == e.id)
                                })
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), entry.title, CollectRequestData(Request, entry), GetIpAddress(Request), true);
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
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, model.id.ToString(), entry.title, CollectRequestData(Request, model), GetIpAddress(Request), false);
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
                    SocialMedia parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();
                        parentEntry = entry.SocialMedia1;

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                        {
                            id = e.id,
                            title = e.title,
                            isRightToLeft = e.isRightToLeft,
                            isFilled = (parentEntry.SocialMedia1 ?? parentEntry).SocialMedias.Any(d => !d.isDeleted && d.languageId == e.id)
                        })
                    });
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
        public HttpResponseMessage GetData()
        {
            var socialMedias = rpstry.GetAllIsPublished().Select(d => new
            {
                id = d.id,
                title = d.title,
                icon = d.icon,
                link = d.link
            });
            return Request.CreateResponse(HttpStatusCode.OK, socialMedias);
        }
        #endregion
    }

}