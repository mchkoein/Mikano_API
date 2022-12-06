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
using WebApi.OutputCache.V2;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/divisions")]
    public class DivisionsController : SharedController<SocketHub>
    {
        private DivisionRepository rpstry = new DivisionRepository();
        private string kSectionName = "divisions";
        private string kActionName = "";

        #region Backend
        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, int? parentId = null)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
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

                    var data = rpstry.GetAll(parentId).Select(d => new
                    {
                        id = d.id,
                        title = d.title,
                        icon = d.IconList.icon_class,
                        websiteLink = d.link,
                        imgListing = GetGridImage(d.title,d.imgListing),
                        hasProducts = d.hasProducts,
                        priority = d.priority,
                        isPublished = d.isPublished,
                        dateCreated = d.dateCreated,
                        dateModified = d.dateModified,
                    }).ToDataSourceResult(request);

                    int depth = 1;
                    string grandPaId = "";
                    if (parentId.HasValue)
                    {
                        var categoryEntry = rpstry.GetById(parentId.Value);
                        depth += categoryEntry.Level;
                        grandPaId = categoryEntry.parentId.HasValue ? categoryEntry.parentId + "" : "";
                    }

                    return Request.CreateResponse(HttpStatusCode.OK,
                       new
                       {
                           Data = data.Data,
                           Errors = data.Errors,
                           AggregateResults = data.AggregateResults,
                           Total = data.Total,
                           depth = depth,
                           grandPaId = grandPaId
                       }
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
        public HttpResponseMessage GetForMultiselect([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
        {
            return Request.CreateResponse(HttpStatusCode.OK, rpstry.GetAllIsPublished().Select(d => new
            {
                id = d.id,
                title = d.title,
                urlTitle = rpstry.GetUrlTitle(d.title),
            }).ToDataSourceResult(request));
        }

        [HttpGet]
        public HttpResponseMessage GetForDropDown()
        {
            return Request.CreateResponse(HttpStatusCode.OK, rpstry.GetAllIsPublished().Select(d => new
            {
                id = d.title,
                label = d.title,
                urlTitle = rpstry.GetUrlTitle(d.title),
            }));
        }

        [HttpGet]
        public HttpResponseMessage GetForKendoDropDown(int level = 1)
        {
            var results = rpstry.GetAllCategories().ToList().Where(d => d.Level == level).Select(d => new
            {
                id = "," + d.id + ",",
                label = (level == 3 ? (d.Division1.parentId.HasValue ? d.Division1.Division1.title + " » " : "") : "") + (d.parentId.HasValue ? d.Division1.title + " » " : "") + d.title,
            }).OrderBy(d => d.label);

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [HttpGet]
        public HttpResponseMessage GetById(int id, int? parentId = null, int? languageId = null)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();
            AdministratorRepository adminRpstry = new AdministratorRepository();
            IconListRepository iconListRepo = new IconListRepository();

            loggedInUserId = User.Identity.GetUserId();
            var icons = iconListRepo.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.title  });
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    Division oldEntry = null;
                    var entry = oldEntry = rpstry.GetById(id);
                    var languageDirectionIsRtl = false;
                    int? languageParentId = null;
                    string languageTitle = null;

                    if (languageId.HasValue)
                    {
                        entry = oldEntry = rpstry.GetById(parentId.Value);

                        if (entry != null)
                        {
                            languageParentId = entry.id;
                        }
                        entry = entry.Divisions1.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
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
                                languageParentId = languageParentId,
                                parentId = languageId.HasValue ? oldEntry.parentId : parentId,
                            },
                            fieldsStatus = new
                            {
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                icons = icons,
                                showInMenuOptions = showInMenuOptions,
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
                                parentId = entry.parentId,
                                title = entry.title,
                                icon_id = entry.icon_id + "",
                                description = entry.description,
                                link = entry.link,
                                hasProducts = entry.hasProducts,
                                imgListing = new UploadController().GetUploadedFiles(entry.imgListing, null, DivisionsDirectory),
                                heroImg = new UploadController().GetUploadedFiles(entry.heroImg, null, DivisionsDirectory),
                                priority = entry.priority,
                                isPublished = entry.isPublished + "",
                            },
                            fieldsStatus = new
                            {
                            },
                            languages = languageRpstry.GetAllOptional().Select(e => new
                            {
                                id = e.id,
                                title = e.title,
                                isRightToLeft = e.isRightToLeft,
                                isFilled = entry.Divisions1.Any(d => !d.isDeleted && d.languageId == e.id)
                            }),
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                icons = icons,
                                infoBarBlocks = new[] {
                                    new
                                    {
                                        title = "Logs",
                                        fields = new[]
                                        {
                                            new {
                                                label = "Created on",
                                                value = entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")
                                            },
                                            new {
                                                label = "Modified on",
                                                value = entry.dateModified.ToString("dd MMM yyyy HH:mm tt")
                                            },
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, Division entry, SubmissionOptions submissionType, bool inline = false)
        {

            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                //if (entry.id == 0)
                //{
                //    ModelState.Remove("entry.id");
                //}
                if (ModelState.IsValid)
                {
                    try
                    {
                        bool imageHasBeenChanged = false;

                        #region Create
                        if (entry.id == 0 && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            if (inline)
                            {
                                entry.heroImg = null;
                            }
                            imageHasBeenChanged = true;

                            //entry.corporatePageId = entry.corporatePageId != null ? entry.corporatePageId : null;
                            entry.dateCreated = DateTime.Now;
                            entry.dateModified = DateTime.Now;
                            entry.priority = rpstry.GetMaxPriority(entry.parentId) + 1;
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
                            oldEntry.icon_id = entry.icon_id;
                            oldEntry.link = entry.link;
                            oldEntry.description = entry.description;
                            oldEntry.hasProducts = entry.hasProducts;
                            oldEntry.imgListing = entry.imgListing;
                            oldEntry.heroImg = entry.heroImg;
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
                                    imgListing = entry.imgListing == "" || entry.imgListing == null ? (entry.title[0]+""+entry.title[1]) : projectConfigKeys.apiUrl+"/content/uploads/"+ DivisionsDirectory+ "/" + entry.imgListing,
                                    heroImg = entry.heroImg == "" || entry.heroImg == null ? (entry.title[0]+""+entry.title[1]) : projectConfigKeys.apiUrl+"/content/uploads/"+ DivisionsDirectory + "/" + entry.heroImg,
                                    priority = entry.priority,
                                    isPublished = entry.isPublished,
                                    dateCreated = entry.dateCreated,
                                    dateModified = entry.dateModified,
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
                                    isFilled = (entry.Division2 ?? entry).Divisions1.Any(d => !d.isDeleted && d.languageId == e.id)
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

                    RemoveStaticDataCache();
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
                    Division parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();
                        parentEntry = entry.Division1;

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    rpstry.UpdateSubDeletedRecords();
                    RemoveStaticDataCache();
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                        {
                            id = e.id,
                            title = e.title,
                            isRightToLeft = e.isRightToLeft,
                            isFilled = (parentEntry.Division1 ?? parentEntry).Divisions.Any(d => !d.isDeleted && d.languageId == e.id)
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

        #endregion
    }
}