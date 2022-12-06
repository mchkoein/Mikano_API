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
using Newtonsoft.Json;
using System.Configuration;
using System.Runtime.Caching;
using WebApi.OutputCache.V2;
using System.IO;
using System.Drawing;
using System.Web;
using System.Collections.Generic;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/CorporatePageTemplates")]
    public class CorporatePageTemplatesController : SharedController<SocketHub>
    {
        private CorporatePageTemplateRepository rpstry = new CorporatePageTemplateRepository();
        private string kSectionName = "corporatepagetemplates";
        private string kActionName = "";

        #region Back End
        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
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

                    return Request.CreateResponse(HttpStatusCode.OK, rpstry.GetAll().Select(d => new
                    {
                        id = d.id,
                        imgSrc = GetGridImage(d.title, d.imgSrc),
                        title = d.title,
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
            AdministratorRepository adminRpstry = new AdministratorRepository();

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



                    #region Create
                    if (entry == null)
                    {
                        var otherTemplates = rpstry.GetAllIsPublished().Select(d => new
                        {
                            id = d.id + "",
                            label = d.title + "(# " + d.id + ")",
                        });

                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                isPublished = "False",
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                otherTemplates = otherTemplates,
                            }
                        });
                    }
                    #endregion

                    #region Update
                    else
                    {
                        var otherTemplates = rpstry.GetAllIsPublished().Where(d => d.id != entry.id).Select(d => new
                        {
                            id = d.id + "",
                            label = d.title + "(# " + d.id + ")",
                        });
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                id = entry.id,

                                #region Basic Info
                                title = entry.title,
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, corporatePageTemplatesDirectory),
                                #endregion

                                #region Front Wiring
                                frontHtmlId = entry.frontHtmlId,
                                frontHtmlClass = entry.frontHtmlClass,
                                #endregion

                                #region Layout
                                hasTitle = entry.hasTitle,
                                hasSubtitle = entry.hasSubtitle,
                                hasSubtitle1 = entry.hasSubtitle1,
                                hasDescription = entry.hasDescription,
                                hasSmallDescription = entry.hasSmallDescription,
                                hasImage = entry.hasImage,
                                hasImageSecondary = entry.hasImageSecondary,
                                hasVideo = entry.hasVideo,
                                hasVideoLink = entry.hasVideoLink,
                                hasFile = entry.hasFile,
                                hasLabelLink = entry.hasLabelLink,
                                hasLink = entry.hasLink,
                                hasSubSectionsRepeater = entry.hasSubSectionsRepeater,
                                subSectionsRepeaterId = entry.subSectionsRepeaterId + "",

                                hasImageGallery = entry.hasImageGallery,
                                hasVideoGallery = entry.hasVideoGallery,
                                hasFileGallery = entry.hasFileGallery,
                                imageRecommendedSize = entry.imageRecommendedSize,
                                imageSecondaryRecommendedSize = entry.imageSecondaryRecommendedSize,
                                ImagesRecommendedSize1 = entry.ImagesRecommendedSize1,

                                #endregion

                                #region Layout : Related fields
                                hasRelatedEKomCategories = entry.hasRelatedEKomCategories,
                                hasRelatedEKomCategories1 = entry.hasRelatedEKomCategories1,
                                hasRelatedEKomCollections = entry.hasRelatedEKomCollections,
                                hasRelatedEKomProducts = entry.hasRelatedEKomProducts,
                                #endregion


                                priority = entry.priority,
                                isPublished = entry.isPublished + "",
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                otherTemplates = otherTemplates,
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, CorporatePageTemplate entry, SubmissionOptions submissionType, bool inline = false)
        {

            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                ModelState.Remove("entry.imgSrc");
                ModelState.Remove("entry.imgSrc");
                ModelState.Remove("entry.videoSrc");

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
                            if (entry.datePublished == null)
                            {
                                entry.datePublished = DateTime.Now;
                            }
                            if (entry.isPublished)
                            {
                                entry.publishedBy = loggedInUserId;
                            }
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

                            #region Basic Info
                            oldEntry.title = entry.title;
                            oldEntry.imgSrc = entry.imgSrc;
                            #endregion

                            #region Front Wiring
                            oldEntry.frontHtmlId = entry.frontHtmlId;
                            oldEntry.frontHtmlClass = entry.frontHtmlClass;
                            #endregion

                            #region Layout
                            oldEntry.hasTitle = entry.hasTitle;
                            oldEntry.hasSubtitle = entry.hasSubtitle;
                            oldEntry.hasSubtitle1 = entry.hasSubtitle1;
                            oldEntry.hasDescription = entry.hasDescription;
                            oldEntry.hasSmallDescription = entry.hasSmallDescription;
                            oldEntry.hasImage = entry.hasImage;
                            oldEntry.hasImageSecondary = entry.hasImageSecondary;
                            oldEntry.hasVideo = entry.hasVideo;
                            oldEntry.hasVideoLink = entry.hasVideoLink;
                            oldEntry.hasFile = entry.hasFile;
                            oldEntry.hasLabelLink = entry.hasLabelLink;
                            oldEntry.hasLink = entry.hasLink;

                            oldEntry.hasSubSectionsRepeater = entry.hasSubSectionsRepeater;
                            oldEntry.subSectionsRepeaterId = entry.subSectionsRepeaterId;

                            oldEntry.hasImageGallery = entry.hasImageGallery;
                            oldEntry.hasVideoGallery = entry.hasVideoGallery;
                            oldEntry.hasFileGallery = entry.hasFileGallery;
                            oldEntry.imageRecommendedSize = entry.imageRecommendedSize;
                            oldEntry.imageSecondaryRecommendedSize = entry.imageSecondaryRecommendedSize;
                            oldEntry.ImagesRecommendedSize1 = entry.ImagesRecommendedSize1;

                            #endregion

                            #region Layout : Related fields
                            oldEntry.hasRelatedEKomCategories = entry.hasRelatedEKomCategories;
                            oldEntry.hasRelatedEKomCategories1 = entry.hasRelatedEKomCategories1;
                            oldEntry.hasRelatedEKomCollections = entry.hasRelatedEKomCollections;
                            oldEntry.hasRelatedEKomProducts = entry.hasRelatedEKomProducts;
                            #endregion


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
                                id = entry.id
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
                    CorporatePageTemplate parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        entry.deletedBy = loggedInUserId;
                        entry.dateDeleted = DateTime.Now;
                        rpstry.Save();
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK);
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

        #region Helpers
        [HttpGet]
        public HttpResponseMessage getTemplateRestrictions(int id)
        {
            CorporatePageTemplate selectedTemplate = rpstry.GetById(id);
            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                id = selectedTemplate.id,
                title = selectedTemplate.title,
                layout = new
                {
                    has_title = selectedTemplate.hasTitle,
                    has_subtitle = selectedTemplate.hasSubtitle,
                    has_subtitle1 = selectedTemplate.hasSubtitle1,
                    has_description = selectedTemplate.hasDescription,
                    has_smallDescription = selectedTemplate.hasSmallDescription,
                    has_labelLink = selectedTemplate.hasLabelLink,
                    has_link = selectedTemplate.hasLink,
                    has_imgSrc = selectedTemplate.hasImage,
                    has_imgSrcSecondary = selectedTemplate.hasImageSecondary,
                    has_videoUrl = selectedTemplate.hasVideoLink,
                    has_videoSrc = selectedTemplate.hasVideo,
                    has_fileSrc = selectedTemplate.hasFile,
                    has_imgSrc__recommendedSize = selectedTemplate.imageRecommendedSize,
                    has_imgSrcSecondary__recommendedSize = selectedTemplate.imageSecondaryRecommendedSize,
                    has_Images__recommendedSize = selectedTemplate.ImagesRecommendedSize1,


                    has_Images = selectedTemplate.hasImageGallery,
                    has_Videos = selectedTemplate.hasVideoGallery,
                    has_Files = selectedTemplate.hasFileGallery,
                    has_SubSectionsRepeater = selectedTemplate.hasSubSectionsRepeater,


                    has_RelatedEKomCategories = selectedTemplate.hasRelatedEKomCategories,
                    has_RelatedEKomCategories1 = selectedTemplate.hasRelatedEKomCategories1,
                    has_RelatedEKomCollections = selectedTemplate.hasRelatedEKomCollections,
                    has_RelatedEKomProducts = selectedTemplate.hasRelatedEKomProducts,

                }
            });
        }
        #endregion

        #endregion

        #region Front End

        #endregion
    }
}