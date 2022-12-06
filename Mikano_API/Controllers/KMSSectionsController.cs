using System.Web.Http;
using Mikano_API.Models;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System;
using Microsoft.AspNet.Identity;
using System.Web.Http.ModelBinding;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using static Mikano_API.Models.KMSEnums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/kmssections")]
    public class KMSSectionsController : SharedController<SocketHub>
    {
        private KMSSectionRepository rpstry = new KMSSectionRepository();
        private string kSectionName = "kmssections";
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
                    computername = d.computername,
                    component = d.component,
                    title = d.title,
                    icon = d.icon,
                    isSortable = d.isSortable,
                    hasReadyForPublish = d.hasReadyForPublish,
                    showOnMenu = d.showOnMenu,
                    isPublishable = d.isPublishable,
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
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            //bool hasPermissionsToViewOnly = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.view_only);
            //if ((hasPermissionsToCreate && id == -1) || (hasPermissionsToUpdate && id != -1) || (hasPermissionsToViewOnly && id != -1))
            if ((hasPermissionsToCreate && id == -1) || (hasPermissionsToUpdate && id != -1))
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(id);
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    var config = new object();
                    if (entry != null && !string.IsNullOrEmpty(entry.config))
                    {
                        var token = JToken.Parse(entry.config);
                        if (token is JArray)
                        {
                            config = token.ToObject<List<object>>();
                            //config = JObject.Parse(entry.columns); //JsonConvert.DeserializeObject( entry.columns)

                        }
                        else if (token is JObject)
                        {
                            config = token.ToObject<object>();
                        }
                        //columns = JObject.Parse(entry.columns); //JsonConvert.DeserializeObject( entry.columns)

                    }

                    //config = getObjectByTypeName("KMSSection");
                    //Mobi.Models.KMSSection 
                    // Mobi.VBModels.sp_LaraReportResult
                    var sections = rpstry.GetAllForRoles().Select(d => new
                    {
                        id = d.id + "",
                        label = d.title
                    });

                    var levelSections = rpstry.GetAllSectionsGroup().Select(d => new
                    {
                        id = d.id + "",
                        label = d.title
                    });

                    var controllers = GetControllers(isKMSSection: true);

                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                            },
                            additionalData = new
                            {
                                sections = sections,
                                levelSections = levelSections,
                                controllers = controllers
                            }
                        });
                    }
                    else
                    {
                        levelSections = levelSections.Where(d => d.id != entry.id + "");
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                id = entry.id,
                                nesteLabelUnderId = entry.nesteLabelUnderId + "",
                                parentId = entry.parentId + "",
                                computername = entry.computername.Contains("-sg-") ? "sectionsgroup" : entry.computername,
                                labelInParent = entry.labelInParent,
                                depth = entry.depth,
                                component = entry.component,
                                title = entry.title,
                                icon = entry.icon,
                                showOnMenu = entry.showOnMenu,
                                isUnderline = entry.isUnderline,
                                isSortable = entry.isSortable,
                                isPublishable = entry.isPublishable,
                                hasReadyForPublish = entry.hasReadyForPublish,
                                config = config
                            },
                            additionalData = new
                            {
                                sections = sections,
                                levelSections = levelSections,
                                controllers = controllers
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

        [HttpGet]
        public HttpResponseMessage GetConfiguration(string computername, string component, int? configuration = null)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            //bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            //bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            //if (hasPermissionsToCreate)
            //{
            //kActionName = KActions.read.ToString();
            try
            {
                var entry = rpstry.GetByComputerName(computername, component, configuration);
                var config = new object();
                if (entry != null && !string.IsNullOrEmpty(entry.config))
                {
                    var token = JToken.Parse(entry.config);
                    if (token is JArray)
                    {
                        config = token.ToObject<List<object>>();
                        //config = JObject.Parse(entry.columns); //JsonConvert.DeserializeObject( entry.columns)

                    }
                    else if (token is JObject)
                    {
                        config = token.ToObject<object>();
                    }
                    //columns = JObject.Parse(entry.columns); //JsonConvert.DeserializeObject( entry.columns)
                }

                //config = getObjectByTypeName("KMSSection");
                //Mikano_API.Models.KMSSection 
                // Mikano_API.VBModels.sp_LaraReportResult

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    sectionTitle = entry.title,
                    config = config
                });
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
            }
            //}
            //else
            //{
            //    return Request.CreateResponse(HttpStatusCode.Unauthorized);
            //}
        }

        [HttpGet]
        public HttpResponseMessage GetModelSchemaByName(string id)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            //bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            //bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            //if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            //{
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, getObjectByTypeName(id));
            }
            catch (Exception e)
            {
                logRpstry.AddLog(loggedInUserId, User.Identity.Name, "GetModelSchemaByName", kSectionName, id + "", kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                return Request.CreateResponse(HttpStatusCode.BadRequest, "");
            }
            //}
            //else
            //{
            //    return Request.CreateResponse(HttpStatusCode.Unauthorized);
            //}
        }

        //[HttpGet]
        //public HttpResponseMessage GetPermissions(string section)
        //{
        //    if (section == KSections.home.ToString() || string.IsNullOrEmpty(section))
        //    {
        //        return Request.CreateResponse(HttpStatusCode.OK, new { fullPermissions = true });
        //    }
        //    else if (User.Identity.IsAuthenticated)
        //    {
        //        KPermission permissions = sectionRpstry.GetPermissions(section, loggedInUserId);
        //        return Request.CreateResponse(HttpStatusCode.OK,
        //            new
        //            {
        //                CanCreate = permissions.CanCreate,
        //                CanRead = permissions.CanRead,
        //                CanUpdate = permissions.CanUpdate,
        //                CanDelete = permissions.CanDelete,
        //                CanPublish = permissions.CanPublish,
        //                isSortable = permissions.sectionEntry.isSortable,
        //                isPublishable = permissions.sectionEntry.isPublishable,
        //                hasReadyForPublish = permissions.sectionEntry.hasReadyForPublish,
        //            }
        //        );
        //    }
        //    else
        //    {
        //        return Request.CreateResponse(HttpStatusCode.Unauthorized);
        //    }
        //}

        [HttpPost]
        public HttpResponseMessage Details(KMSSection entry, SubmissionOptions submissionType)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        //create
                        if (entry.id == 0 && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            if (entry.computername.ToLower() == "sectionsgroup")
                            {
                                entry.computername = "-sg-" + entry.title.Trim().Replace(" ", "").ToLower();
                            }
                            else
                            {
                                entry.computername = entry.computername.ToLower();
                            }
                            entry.dateCreated = DateTime.Now;
                            entry.dateModified = DateTime.Now;
                            entry.priority = rpstry.GetMaxPriority() + 1;
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


                            if (entry.computername.ToLower() == "sectionsgroup")
                            {
                                oldEntry.computername = "-sg-" + entry.title.Trim().Replace(" ", "").ToLower();
                            }
                            else
                            {
                                oldEntry.computername = entry.computername.ToLower();
                            }

                            oldEntry.nesteLabelUnderId = entry.nesteLabelUnderId;
                            oldEntry.parentId = entry.parentId;
                            oldEntry.labelInParent = entry.labelInParent;
                            oldEntry.depth = entry.depth;
                            oldEntry.component = entry.component;
                            oldEntry.config = entry.config;
                            oldEntry.title = entry.title;
                            oldEntry.icon = entry.icon;
                            oldEntry.showOnMenu = entry.showOnMenu;
                            oldEntry.isUnderline = entry.isUnderline;
                            oldEntry.isSortable = entry.isSortable;
                            oldEntry.isPublishable = entry.isPublishable;
                            oldEntry.hasReadyForPublish = entry.hasReadyForPublish;
                            oldEntry.dateModified = DateTime.Now;
                            entry = oldEntry;
                        }

                        //if (submissionType == SubmissionOptions.saveAndPublish) {
                        //    entry.isPublished = true;
                        //}
                        rpstry.Save();


                        if (submissionType == SubmissionOptions.saveAndGoToNext)
                        {
                            entry = rpstry.GetNextEntry(entry);
                        }

                        UpdatePermissions();
                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), entry.title, CollectRequestData(Request, entry), GetIpAddress(Request), false);

                        return Request.CreateResponse(HttpStatusCode.OK, new { id = entry.id });
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), kSectionName, CollectRequestData(Request, entry), GetIpAddress(Request), true);
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
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, model.id.ToString(), entry.title, CollectRequestData(Request, model), GetIpAddress(Request), false);

                    return rpstry.SortGrid(model.newIndex, model.oldIndex, model.id);
                }
                catch (Exception e)
                {

                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, model.id.ToString(), kSectionName, CollectRequestData(Request, model), GetIpAddress(Request), true);
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
                        rpstry.Delete(Convert.ToInt32(parId));
                        rpstry.Save();
                        UpdatePermissions();
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

        private object getObjectByTypeName(string type)
        {
            if (type.StartsWith("vb."))
            {
                type = type.Remove(0, 3);
                type = "Mikano_API.VBModels." + type; ;
            }
            else
            {
                type = "Mikano_API.Models." + type; ;
            }
            return Activator.CreateInstance(Type.GetType(type, true, true));
        }


        [HttpGet]
        [AllowAnonymous]
        public void UpdatePermissions()
        {
            #region Manage Left Menu
            RolesRepository roleRpstry = new RolesRepository();
            var sections = rpstry.GetAll().GroupBy(d => d.computername).Select(d => d.OrderByDescending(e => e.component).FirstOrDefault()).OrderByDescending(d => d.priority);
            foreach (var item in roleRpstry.GetAll())
            {
                item.menu = JsonConvert.SerializeObject(sections.Select(d => new
                {
                    id = d.id,
                    nesteLabelUnderId = d.nesteLabelUnderId,
                    isSectionsGroup = d.component.ToLower() == "sectionsgroup",
                    relatedSections = d.KMSSections.Select(q => new
                    {
                        id = q.id,
                        computername = q.computername,
                        component = q.component,
                        labelInParent = q.labelInParent,
                    }),
                    depth = d.depth,
                    computername = d.computername,
                    component = d.component,
                    title = d.title,
                    icon = d.icon,
                    showOnMenu = d.showOnMenu,
                    isUnderline = d.isUnderline,
                    isSortable = d.isSortable,
                    isPublishable = d.isPublishable,
                    hasReadyForPublish = d.hasReadyForPublish,
                    CanCreate = d.KMSPermissions.Any(e => e.roleId == item.Id && e.kmsPermissionTypeId == (int)KActions.create),
                    CanRead = d.component.ToLower() == "sectionsgroup" ? true : d.KMSPermissions.Any(e => e.roleId == item.Id && e.kmsPermissionTypeId == (int)KActions.read),
                    CanUpdate = d.KMSPermissions.Any(e => e.roleId == item.Id && e.kmsPermissionTypeId == (int)KActions.update),
                    CanDelete = d.KMSPermissions.Any(e => e.roleId == item.Id && e.kmsPermissionTypeId == (int)KActions.delete),
                    CanPublish = d.KMSPermissions.Any(e => e.roleId == item.Id && e.kmsPermissionTypeId == (int)KActions.publish)
                }));

                item.menuLevel = JsonConvert.SerializeObject(sections.Where(d => !d.nesteLabelUnderId.HasValue).Select(d => d.NestedMenues(item.Id)));

            }
            roleRpstry.Save();

            #endregion
        }
    }
}