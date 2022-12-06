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
using Newtonsoft.Json.Linq;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/Regions")]
    public class RegionsController : SharedController<SocketHub>
    {
        private RegionRepository rpstry = new RegionRepository();
        private string kSectionName = "regions";
        private string kActionName = "";

        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, int? parentId = null)
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

                    var data = rpstry.GetAll(parentId).Select(d => new
                    {
                        id = d.id,
                        title = d.title,
                        priority = d.priority,
                        isPublished = d.isPublished,
                        dateCreated = d.dateCreated,
                        dateModified = d.dateModified,
                    }).ToDataSourceResult(request);

                    int depth = 1;
                    string grandPaId = "";
                    if (parentId.HasValue)
                    {
                        var regionEntry = rpstry.GetById(parentId.Value);
                        depth += regionEntry.Level;
                        grandPaId = regionEntry.parentId.HasValue ? regionEntry.parentId + "" : "";
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
        public HttpResponseMessage GetById(int id, int? parentId = null)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(id);
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);



                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                isPublished = "False"
                            },
                            fieldsStatus = new
                            {
                                phoneCode = parentId.HasValue ? DirectiveStatus.hidden : "",
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions
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
                                phoneCode = entry.phoneCode,
                                priority = entry.priority,
                                isPublished = entry.isPublished + ""
                            },
                            fieldsStatus = new
                            {
                                phoneCode = entry.parentId.HasValue ? DirectiveStatus.hidden : "",
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                logs = new[] {
                                    new {
                                        label = "Created on",
                                        value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")
                                        },
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, JObject data, SubmissionOptions submissionType, bool inline = false)
        {

            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                Models.Region entry = data.ToObject<Models.Region>();
                if (entry.id == 0)
                {
                    ModelState.Remove("entry.id");
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        bool imageHasBeenChanged = false;
                        //create
                        if (entry.id == 0 && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            entry.dateCreated = DateTime.Now;
                            entry.dateModified = DateTime.Now;
                            entry.priority = rpstry.GetMaxPriority(entry.parentId) + 1;

                            rpstry.Add(entry);
                        }
                        //edit
                        else if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            var oldEntry = rpstry.GetById(entry.id);

                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, entry.id);
                            }

                            oldEntry.parentId = entry.parentId;
                            oldEntry.title = entry.title;
                            oldEntry.phoneCode = entry.phoneCode;
                            oldEntry.isPublished = entry.isPublished;
                            oldEntry.dateModified = DateTime.Now;
                            entry = oldEntry;
                        }

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
                            return Request.CreateResponse(HttpStatusCode.OK, new { id = entry.id });
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
                    return e.Message;
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

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

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

        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetSubRegion(int id)
        {
            var subRegions = rpstry.GetSubRegionsByParent(id).Select(d => new
            {
                id = d.id + "",
                parentId = d.parentId + "",
                label = d.title
            });


            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                subRegions = subRegions
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetCities(int provinceId)
        {
            var cities = rpstry.GetSubRegionsByParent(provinceId).Select(d => new
            {
                id = d.id + "",
                label = d.title
            });

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                cities = cities
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetRegionIdByPhone(string phone)
        {
            var entries = rpstry.GetAllRegionByPhone(phone);

            //var allRegions = allEntries.Select(
            //   d => new
            //   {
            //       id = d.id,
            //       label = d.title,
            //       parentId = d.parentId
            //   });

            //var regions = new[] { new {
            //    id = entry.id + "",
            //    parentId = entry.parentId + "",
            //    label = entry.title
            //}};

            var regions = entries.Select(d => new
            {
                id = d.id + "",
                parentId = d.parentId + "",
                label = d.title
            });
            var selectedRegion = entries.FirstOrDefault();

            var subRegions = rpstry.GetSubRegionsByParent(selectedRegion == null ? -1 : selectedRegion.id).Select(d => new
            {
                id = d.id + "",
                parentId = d.parentId + "",
                label = d.title
            });
            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                id = selectedRegion == null ? "" : selectedRegion.id + "",
                regions = regions,
                subRegions = subRegions
            });
        }

        #region front end

        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetData()
        {
            var entries = rpstry.GetAllIsPublished().Select(d => new
            {
                id = d.id + "",
                title = d.title
            }); ;
            return Request.CreateResponse(HttpStatusCode.OK, entries);
        }
        #endregion
    }
}