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
using System.Collections.Generic;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/Careers")]
    public class CareersController : SharedController<SocketHub>
    {
        private CareerRepository rpstry = new CareerRepository();
        private string kSectionName = "careers";
        private string kActionName = "";

        #region Backend
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
                            description = d.description,
                            applicationsCount = d.CareerApplications.Count(),
                            companyId = d.companyId,
                            companyTitle = d.Company.title,
                            country = d.Company.Country.name,
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
        public HttpResponseMessage GetById(int id, int? languageId = null, int? parentId = null)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();
            AdministratorRepository adminRpstry = new AdministratorRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(languageId.HasValue ? parentId.Value : id);
                    var languageDirectionIsRtl = false;
                    int? languageParentId = null;
                    string languageTitle = null;

                    if (languageId.HasValue)
                    {
                        languageParentId = parentId;
                        if (entry != null)
                        {
                            entry = entry.Careers.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
                        }
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
                                smallDescription = entry.smallDescription,
                                description = entry.description,
                                companyId = entry.companyId,
                                isPublished = entry.isPublished + ""
                            },
                            languages = languageRpstry.GetAllOptional().Select(e => new
                            {
                                id = e.id,
                                title = e.title,
                                isRightToLeft = e.isRightToLeft,
                                isFilled = entry.Careers.Any(d => !d.isDeleted && d.languageId == e.id)
                            }),
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                infoBarBlocks = new[] {
                                    new
                                    {
                                        title = "Logs",
                                        fields = new[]
                                        {
                                            new {label = "Created on", value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")},
                                            new {label = "Created by", value = string.IsNullOrEmpty(entry.createdBy) ? "" : adminRpstry.GetById(entry.createdBy).FullName},
                                            new {label = "Modified on", value = entry.dateModified.ToString("dd MMM yyyy HH:mm tt")},
                                            new {label = "Modified by", value = string.IsNullOrEmpty(entry.modifiedBy) ? "" : adminRpstry.GetById(entry.modifiedBy).FullName},
                                        }
                                    }
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, Career entry, SubmissionOptions submissionType, bool inline = false)
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
                if (ModelState.IsValid)
                {
                    try
                    {

                        #region Create
                        if (entry.id == 0 && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            entry.dateCreated = DateTime.Now;
                            entry.createdBy = loggedInUserId;
                            entry.dateModified = DateTime.Now;
                            entry.modifiedBy = loggedInUserId;
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
                            oldEntry.smallDescription = entry.smallDescription;
                            oldEntry.description = entry.description;
                            oldEntry.isPublished = entry.isPublished;
                            oldEntry.dateModified = DateTime.Now;
                            oldEntry.modifiedBy = loggedInUserId;
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
                                    isFilled = (entry.Career1 ?? entry).Careers.Any(d => !d.isDeleted && d.languageId == e.id)
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
                    Career parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        entry.deletedBy = loggedInUserId;
                        entry.dateDeleted = DateTime.Now;
                        rpstry.Save();
                        parentEntry = entry.Career1;
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                        {
                            id = e.id,
                            title = e.title,
                            isRightToLeft = e.isRightToLeft,
                            isFilled = (parentEntry.Career1 ?? parentEntry).Careers.Any(d => !d.isDeleted && d.languageId == e.id)
                        })
                    });
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpGet]
        public HttpResponseMessage NavigateThroughEntries(int id, string navigation)
        {
            var targetedId = rpstry.GetTargetedEntry(id, navigation);
            return Request.CreateResponse(HttpStatusCode.OK, targetedId);
        }

        [HttpGet]
        public HttpResponseMessage getCompanies([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
        {
            CompanyRepository companyRpstry = new CompanyRepository();

            var results = companyRpstry.GetAllPublishedCompanies();
            var castedResults = results.Select(d => new
            {
                id = d.id,
                title = d.title,
                imgSrc = GetGridImage(d.title, d.imgSrc),
                countryTitle = d.Country.name,
            });

            return Request.CreateResponse(HttpStatusCode.OK, castedResults.ToDataSourceResult(request));
        }

        #endregion

        #region Front End
        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetData()
        {
            var requestedLanguageId = GetLanguageId();

            var entriesResults = new List<object>();
            var entries = rpstry.GetAllIsPublished();

            foreach (var entryItem in entries)
            {
                var entryTranslatedItem = requestedLanguageId == -1 ? null : entryItem.Careers.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                entriesResults.Add(new
                {
                    id = entryItem.id + "",
                    title = entryTranslatedItem == null ? entryItem.title : entryTranslatedItem.title,
                    description = entryTranslatedItem == null ? entryItem.description : entryTranslatedItem.description,
                });
            }
            return Request.CreateResponse(HttpStatusCode.OK, entriesResults);
        }
        #endregion

    }

}