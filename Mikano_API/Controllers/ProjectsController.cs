using Mikano_API.Helpers;
using Mikano_API.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using WebApi.OutputCache.V2;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/projects")]
    public class ProjectsController : SharedController<SocketHub>
    {
        private ProjectsRepository rpstry = new ProjectsRepository();
        private string kSectionName = "projects";
        private string kActionName = "";

        #region Backend
        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))] DataSourceRequest request)
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
                            type = d.ProjectCategory.title,
                            category = d.Division.title,
                            subCategory = d.DivisionSubCategory.title,
                            subtitle = d.subtitle,
                            description = d.description,
                            smallDescription = d.smallDescription,
                            imgSrc = GetGridImage(d.title, d.imgSrc),
                            imgListing = GetGridImage(d.title, d.imgListing),
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
        public HttpResponseMessage GetById(int id)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            ProjectCategoryRepository projectCategoryRepository = new ProjectCategoryRepository();
            DivisionRepository divisionRepo = new DivisionRepository();
            DivisionSubCategoryRepository subDivisionRepo = new DivisionSubCategoryRepository();
            AdministratorRepository adminRpstry = new AdministratorRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            var projectCategories = projectCategoryRepository.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.title });
            var divisions = divisionRepo.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.title });
            var subDivisions = subDivisionRepo.GetAllIsPublished().OrderBy(d => d.division_id).Select(d => new { id = d.id + "", label = d.Division.title + " - "+ d.title });

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
                                isPublished = "False",
                            },
                            fieldsStatus = new
                            {
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                divisions = divisions,
                                projectCategories = projectCategories,
                                subDivisions = subDivisions, 
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
                                subtitle = entry.subtitle,
                                smallDescription = entry.smallDescription,
                                description = entry.description,
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, projectsDirectory),
                                imgListing = new UploadController().GetUploadedFiles(entry.imgListing, null, projectsDirectory),
                                isPublished = entry.isPublished + "",
                                category_id = entry.category_id + "",
                                division_id = entry.division_id + "",
                                sub_division_id = entry.sub_division_id + "",
                                #region Images
                                Images = new UploadController().GetUploadedFiles(
                                    files: null,
                                    filesArray: entry.ProjectMedias.Select(d => new MediaModel
                                    {
                                        mediaSrc = d.imgSrc
                                    }).ToList(),
                                    directory: projectsDirectory),
                                #endregion

                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                projectCategories = projectCategories,
                                divisions = divisions,
                                subDivisions = subDivisions
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))] DataSourceRequest request, Project entry, SubmissionOptions submissionType, bool inline = false)
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
                            #region Manage Images
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new ProjectMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.project_id = entry.id;
                                imageEntry.imgSrc = item;
                                imageEntry.priority = imagesCounter;
                                entry.ProjectMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion
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
                            #region Manage Images
                            rpstry.DeleteRelatedMedias(oldEntry);
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new ProjectMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.project_id = entry.id;
                                imageEntry.imgSrc = item;
                                imageEntry.priority = imagesCounter;
                                oldEntry.ProjectMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion
                            oldEntry.title = entry.title;
                            oldEntry.subtitle = entry.subtitle;
                            oldEntry.smallDescription = entry.smallDescription;
                            oldEntry.description = entry.description;
                            oldEntry.imgListing = entry.imgListing;
                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.division_id = entry.division_id;
                            oldEntry.category_id = entry.category_id;
                            oldEntry.sub_division_id = entry.sub_division_id;
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

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.delete);
            if (hasPermissions)
            {
                kActionName = KActions.delete.ToString();
                try
                {
                    Project parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
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

        #endregion
    }
}