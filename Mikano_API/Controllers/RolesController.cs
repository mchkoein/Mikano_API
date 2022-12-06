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
using Newtonsoft.Json;
using System.Configuration;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/roles")]
    public class RolesController : SharedController<SocketHub>
    {
        private RolesRepository rpstry = new RolesRepository();
        private string kSectionName = "roles";
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
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    var results = rpstry.GetAll();
                    results = results.Where(d => d.Name != ConfigurationManager.AppSettings["EndUserRole"]);

                    if (!rpstry.IsUserInRole(User.Identity.GetUserId(), ConfigurationManager.AppSettings["DevteamRole"]))
                    {
                        results = results.Where(d => d.Name != ConfigurationManager.AppSettings["DevteamRole"]);
                        if (!rpstry.IsUserInRole(User.Identity.GetUserId(), ConfigurationManager.AppSettings["ManagementRole"]))
                        {
                            results = results.Where(d => d.Name != ConfigurationManager.AppSettings["ManagementRole"]);
                        }
                    }

                    return Request.CreateResponse(HttpStatusCode.OK,
                results.Select(d => new
                {
                    id = d.Id,
                    Name = d.Name,
                    members = d.AspNetUserRoles.Count(e => !e.AspNetUser.isDeleted),
                    dateModified = d.dateModified
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

        [HttpGet]
        public HttpResponseMessage GetById(string id)
        {
            KMSPermissionTypeRepository permissionTypeRpstry = new KMSPermissionTypeRepository();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if ((hasPermissionsToCreate && id == "") || (hasPermissionsToUpdate && id != ""))
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(id);
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.Name, CollectRequestData(Request, null), GetIpAddress(Request), false);
                    var sections = sectionRpstry.GetAllForRoles();

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        model = new
                        {
                            id = entry == null ? "" : entry.Id,
                            Name = entry == null ? "" : entry.Name,
                            Permissions = sections.Select(d => new
                            {
                                sectionId = d.id,
                                section = (d.KMSSection2 == null ? "" : d.KMSSection2.title + " - ") + d.title,
                                canDo = d.KMSPermissions.Where(e => e.roleId == (entry == null ? "" : entry.Id)).Select(z => z.kmsPermissionTypeId),
                                hide = sectionRpstry.db.KMSPermissionTypes.Where(e => e.isPublished).Select(z => z.id),
                                //canRead = d.KMSPermissions.Any(e => e.roleId == (entry == null ? "" : entry.Id) && e.kmsPermissionTypeId == (int)KActions.read),
                                //canCreate = d.KMSPermissions.Any(e => e.roleId == (entry == null ? "" : entry.Id) && e.kmsPermissionTypeId == (int)KActions.create),
                                //canUpdate = d.KMSPermissions.Any(e => e.roleId == (entry == null ? "" : entry.Id) && e.kmsPermissionTypeId == (int)KActions.update),
                                //canDelete = d.KMSPermissions.Any(e => e.roleId == (entry == null ? "" : entry.Id) && e.kmsPermissionTypeId == (int)KActions.delete),
                                //canPublish = d.KMSPermissions.Any(e => e.roleId == (entry == null ? "" : entry.Id) && e.kmsPermissionTypeId == (int)KActions.publish)

                            })
                        },
                        additionalData = new
                        {
                            permissionTypes = permissionTypeRpstry.GetAllIsPublished().Select(s => new
                            {
                                id = s.id,
                                title = s.title
                            }),
                            logs = new[] {
                                    new {
                                        label = "Last Modified on",
                                        value = entry != null && entry.dateModified.HasValue ? entry.dateModified.Value.ToString("dd MMM yyyy HH:mm tt") : ""
                                        }
                                }
                        }
                    });
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, id + "", kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpPost]
        public HttpResponseMessage Details(AspNetRole entry, SubmissionOptions submissionType)
        {
            RoleManager<IdentityRole> roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
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
                        if (rpstry.GetById(entry.Id) == null && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            IdentityResult resulot = roleManager.Create(new IdentityRole(entry.Name));
                            if (!resulot.Succeeded)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", resulot.Errors) });
                            }
                            var oldEntry = rpstry.GetByName(entry.Name);

                            foreach (var item in entry.KMSPermissions)
                            {
                                var newEntry = new KMSPermission();
                                newEntry.dateCreated = DateTime.Now;
                                newEntry.roleId = entry.Id;
                                newEntry.kmsSectionId = item.kmsSectionId;
                                newEntry.kmsPermissionTypeId = item.kmsPermissionTypeId;
                                oldEntry.KMSPermissions.Add(newEntry);
                            }
                            entry = oldEntry;
                        }
                        //edit
                        else if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            rpstry.DeleteOldPermissions(entry);
                            foreach (var item in entry.KMSPermissions)
                            {
                                item.dateCreated = DateTime.Now;
                            }
                            var oldEntry = rpstry.GetById(entry.Id);
                            if (roleManager.RoleExists(entry.Name) && oldEntry.Name != entry.Name)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "Name " + entry.Name + " already taken." });
                            }
                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, entry.Id);
                            }

                            oldEntry.KMSPermissions = entry.KMSPermissions;
                            oldEntry.Name = entry.Name;
                            entry = oldEntry;
                        }
                        entry.dateModified = DateTime.Now;
                        rpstry.Save();

                        if (submissionType == SubmissionOptions.saveAndGoToNext)
                        {
                            entry = rpstry.GetNextEntry(entry);
                        }


                        #region Manage Left Menu

                        var sections = sectionRpstry.GetAll().Where(d => d.KMSPermissions.Any(e => e.roleId == entry.Id) || d.component.ToLower() == "sectionsgroup").GroupBy(d => d.computername).Select(d => d.OrderByDescending(e => e.component).FirstOrDefault()).OrderByDescending(d => d.priority);
                        entry.menu = JsonConvert.SerializeObject(sections.Select(d => new
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
                            parentId = d.parentId,
                            component = d.component,
                            title = d.title,
                            icon = d.icon,
                            showOnMenu = d.showOnMenu,
                            isUnderline = d.isUnderline,
                            isSortable = d.isSortable,
                            isPublishable = d.isPublishable,
                            hasReadyForPublish = d.hasReadyForPublish,
                            CanCreate = d.KMSPermissions.Any(e => e.roleId == entry.Id && e.kmsPermissionTypeId == (int)KMSEnums.KActions.create),
                            CanRead = d.component.ToLower() == "sectionsgroup" ? true : d.KMSPermissions.Any(e => e.roleId == entry.Id && e.kmsPermissionTypeId == (int)KMSEnums.KActions.read),
                            CanUpdate = d.KMSPermissions.Any(e => e.roleId == entry.Id && e.kmsPermissionTypeId == (int)KMSEnums.KActions.update),
                            CanDelete = d.KMSPermissions.Any(e => e.roleId == entry.Id && e.kmsPermissionTypeId == (int)KMSEnums.KActions.delete),
                            CanPublish = d.KMSPermissions.Any(e => e.roleId == entry.Id && e.kmsPermissionTypeId == (int)KMSEnums.KActions.publish),
                            //CanViewOnly = d.KMSPermissions.Any(e => e.roleId == entry.Id && e.kmsPermissionTypeId == (int)KMSEnums.KActions.view_only)

                        }));

                        entry.menuLevel = JsonConvert.SerializeObject(sections.Where(d => !d.nesteLabelUnderId.HasValue).Select(d => d.NestedMenues(entry.Id)));

                        rpstry.Save();

                        #endregion

                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, entry.Id, entry.Name, CollectRequestData(Request, entry), GetIpAddress(Request), false);

                        return Request.CreateResponse(HttpStatusCode.OK, new { id = entry.Id });
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, entry.Id, kSectionName, CollectRequestData(Request, entry), GetIpAddress(Request), true);

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
                        rpstry.Delete(parId);
                        rpstry.Save();
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
    }
}