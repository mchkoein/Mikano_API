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
    [RoutePrefix("api/CorporatePageSectionRepeaters")]
    public class CorporatePageSectionRepeatersController : SharedController<SocketHub>
    {
        private CorporatePageSectionRepository rpstry = new CorporatePageSectionRepository();

        private string kSectionName = "corporatepagesectionrepeaters";
        private string kActionName = "";

        #region Back End
        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, int parentId)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            AdministratorRepository adminRpstry = new AdministratorRepository();

            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.read);
            if (hasPermissions)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), false);


                    return Request.CreateResponse(HttpStatusCode.OK, rpstry.GetAllByCorporatePageSectionParentId(parentId).Select(d => new
                    {
                        id = d.id,
                        templateImgSrc = GetGridImage(d.CorporatePageTemplate.title, d.CorporatePageTemplate.imgSrc),
                        templateTitle = d.CorporatePageTemplate.title,
                        #region Repeated Section
                        title = d.title,
                        imgSrc = string.IsNullOrEmpty(d.title) ? "" : GetGridImage(d.title, d.imgSrc),
                        #endregion
                        isPublished = d.isPublished,
                        dateCreated = d.dateCreated,
                        //createdBy = rpstry.db.fnGetUserFullNameById(d.createdBy),
                        dateModified = d.dateModified,
                        //modifiedBy = rpstry.db.fnGetUserFullNameById(d.modifiedBy),
                    }).ToDataSourceResult(request));
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
        public HttpResponseMessage GetById(int id, int? languageId = null, int? parentId = 0, int? refId = 0)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();
            AdministratorRepository adminRpstry = new AdministratorRepository();
            CorporatePageTemplateRepository templatesRpstry = new CorporatePageTemplateRepository();


            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    CorporatePageSection entry = rpstry.GetById(id);

                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    var templates = templatesRpstry.GetAllIsPublished().Select(d => new
                    {
                        id = d.id + "",
                        title = d.title,
                        imgSrc = GetGridImage(d.title, d.imgSrc),
                    });

                    #region Create
                    if (entry == null)
                    {
                        CorporatePageSection parentEntry = rpstry.GetById(parentId.HasValue ? parentId.Value : -1);
                        CorporatePageTemplate repeaterTemplateEntry = rpstry.GetByTemplateId(parentEntry != null ? parentEntry.corporatePageTemplateId : -1);

                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                corporatePageId = parentEntry.corporatePageId,
                                subCorporatePageId = parentEntry.id,
                                templateId = repeaterTemplateEntry != null ? repeaterTemplateEntry.CorporatePageTemplate1.id + "" : (object)DBNull.Value,
                                isPublished = "False",
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                templates = templates,
                            },
                            fieldsStatus = new
                            {
                                templateId = repeaterTemplateEntry != null ? DirectiveStatus.disabled : "",
                                SubSectionsRepeater = DirectiveStatus.hidden,
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
                                templateId = entry.corporatePageTemplateId + "", // Keep name as templateId
                                title = entry.title,
                                subtitle = entry.subtitle,
                                subtitle1 = entry.subtitle1,
                                smallDescription = entry.smallDescription,
                                description = entry.description,
                                link = entry.link,
                                labelLink = entry.labelLink,

                                #region imgSrc
                                imgSrc = new UploadController().GetUploadedFiles(
                                    files: entry.imgSrc,
                                    filesArray: null,
                                    directory: corporatePageSectionsDirectory,
                                    hasCaption: projectConfigKeys.hasImageCaption,
                                    hasSubCaption: projectConfigKeys.hasImageSubCaption,
                                    hasLink: projectConfigKeys.hasImageLink,
                                    hasDescription: projectConfigKeys.hasImageDescription
                                    ),
                                #endregion
                                #region imgSrcSecondary
                                imgSrcSecondary = new UploadController().GetUploadedFiles(
                                    files: entry.imgSrcSecondary,
                                    filesArray: null,
                                    directory: corporatePageSectionsDirectory,
                                    hasCaption: projectConfigKeys.hasImageSecondaryCaption,
                                    hasSubCaption: projectConfigKeys.hasImageSecondarySubCaption,
                                    hasLink: projectConfigKeys.hasImageSecondaryLink,
                                    hasDescription: projectConfigKeys.hasImageSecondaryDescription),
                                #endregion
                                #region videoSrc
                                videoSrc = new UploadController().GetUploadedFiles(
                                    files: entry.videoSrc,
                                    filesArray: null,
                                    directory: corporatePageSectionsDirectory,
                                    hasCaption: projectConfigKeys.hasVideoCaption,
                                    hasSubCaption: projectConfigKeys.hasVideoSubCaption,
                                    hasLink: projectConfigKeys.hasVideoLink,
                                    hasDescription: projectConfigKeys.hasVideoDescription),
                                #endregion
                                #region fileSrc
                                fileSrc = new UploadController().GetUploadedFiles(
                                    files: entry.fileSrc,
                                    filesArray: null,
                                    directory: corporatePageSectionsDirectory,
                                    hasCaption: projectConfigKeys.hasFileCaption,
                                    hasSubCaption: projectConfigKeys.hasFileCaption,
                                    hasLink: projectConfigKeys.hasFileLink,
                                    hasDescription: projectConfigKeys.hasFileDescription),
                                #endregion

                                #region Image Gallery
                                Images = new UploadController().GetUploadedFiles(
                                    files: null,
                                    filesArray: entry.CorporatePageSectionMedias.Where(d => d.mediaType == (int)MediaType.Image).Select(d => new MediaModel
                                    {
                                        mediaSrc = d.mediaSrc,
                                        caption = d.caption,
                                        subCaption = d.subCaption,
                                        description = d.description,
                                        link = d.link
                                    }).ToList(),
                                    directory: corporatePageSectionsDirectory,
                                    hasCaption: projectConfigKeys.hasImageGalleryCaption,
                                    hasSubCaption: projectConfigKeys.hasImageGallerySubCaption,
                                    hasLink: projectConfigKeys.hasImageGalleryLink,
                                    hasDescription: projectConfigKeys.hasImageGalleryDescription
                                ),
                                #endregion

                                #region Video Gallery
                                Videos = new UploadController().GetUploadedFiles(
                                    files: null,
                                    filesArray: entry.CorporatePageSectionMedias.Where(d => d.mediaType == (int)MediaType.Video).Select(d => new MediaModel
                                    {
                                        mediaSrc = d.mediaSrc,
                                        caption = d.caption,
                                        subCaption = d.subCaption,
                                        description = d.description,
                                        link = d.link
                                    }).ToList(),
                                    directory: corporatePageSectionsDirectory,
                                    hasCaption: projectConfigKeys.hasImageGalleryCaption,
                                    hasSubCaption: projectConfigKeys.hasImageGallerySubCaption,
                                    hasLink: projectConfigKeys.hasImageGalleryLink,
                                    hasDescription: projectConfigKeys.hasImageGalleryDescription
                                ),
                                #endregion

                                #region File Gallery
                                Files = new UploadController().GetUploadedFiles(
                                    files: null, filesArray: entry.CorporatePageSectionMedias.Where(d => d.mediaType == (int)MediaType.File).Select(d => new MediaModel
                                    {
                                        mediaSrc = d.mediaSrc,
                                        caption = d.caption,
                                        subCaption = d.subCaption,
                                        description = d.description,
                                        link = d.link
                                    }).ToList(),
                                    directory: corporatePageSectionsDirectory),
                                #endregion

                                priority = entry.priority,
                                isPublished = entry.isPublished,
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                templates = templates,
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, CorporatePageSection entry, SubmissionOptions submissionType, bool inline = false)
        {

            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                ModelState.Remove("entry.imgSrc");
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

                            #region Manage template
                            entry.corporatePageTemplateId = entry.templateId;
                            #endregion

                            entry.dateCreated = DateTime.Now;
                            entry.createdBy = loggedInUserId;
                            entry.dateModified = DateTime.Now;
                            entry.modifiedBy = loggedInUserId;
                            entry.priority = rpstry.GetMaxPriority(entry.corporatePageId) + 1;

                            #region Manage Media

                            #region Manage Main Image
                            if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "imgSrc" && d.mediaSrc == entry.imgSrc) && !string.IsNullOrEmpty(entry.imgSrc))
                            {
                                var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "imgSrc" && d.mediaSrc == entry.imgSrc);
                                entry.imgCaption = fieldEntry.caption;
                                entry.imgSubCaption = fieldEntry.subCaption;
                                entry.imgDescription = fieldEntry.description;
                                entry.imgLink = fieldEntry.link;
                            }
                            #endregion

                            #region Manage Secondary Image
                            if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "imgSrcSecondary" && d.mediaSrc == entry.imgSrcSecondary) && !string.IsNullOrEmpty(entry.imgSrcSecondary))
                            {
                                var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "imgSrcSecondary" && d.mediaSrc == entry.imgSrcSecondary);
                                entry.imgSecondaryCaption = fieldEntry.caption;
                                entry.imgSecondarySubCaption = fieldEntry.subCaption;
                                entry.imgSecondaryDescription = fieldEntry.description;
                                entry.imgSecondaryLink = fieldEntry.link;
                            }
                            #endregion

                            #region Manage Images
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new CorporatePageSectionMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.corporatePageSectionId = entry.id;
                                imageEntry.mediaSrc = item;
                                imageEntry.mediaType = (int)MediaType.Image;
                                if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "Images" && d.mediaSrc == item) && !string.IsNullOrEmpty(item))
                                {
                                    var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "Images" && d.mediaSrc == item);
                                    imageEntry.caption = fieldEntry.caption;
                                    imageEntry.subCaption = fieldEntry.subCaption;
                                    imageEntry.description = fieldEntry.description;
                                    imageEntry.link = fieldEntry.link;
                                }
                                imageEntry.priority = imagesCounter;
                                entry.CorporatePageSectionMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion

                            #region Manage Videos
                            var listOfVideos = (entry.Videos + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var videosCounter = 1;
                            foreach (var item in listOfVideos)
                            {
                                var videoEntry = new CorporatePageSectionMedia();
                                videoEntry.dateCreated = DateTime.Now;
                                videoEntry.dateModified = DateTime.Now;
                                videoEntry.corporatePageSectionId = entry.id;
                                videoEntry.mediaSrc = item;
                                videoEntry.mediaType = (int)MediaType.Video;
                                if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "Videos" && d.mediaSrc == item) && !string.IsNullOrEmpty(item))
                                {
                                    var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "Videos" && d.mediaSrc == item);
                                    videoEntry.caption = fieldEntry.caption;
                                    videoEntry.subCaption = fieldEntry.subCaption;
                                    videoEntry.description = fieldEntry.description;
                                    videoEntry.link = fieldEntry.link;
                                }
                                videoEntry.priority = videosCounter;
                                entry.CorporatePageSectionMedias.Add(videoEntry);
                                videosCounter++;
                            }
                            #endregion

                            #region Manage Files
                            var listOfFiles = (entry.Files + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var filesCounter = 1;
                            foreach (var item in listOfFiles)
                            {
                                var fileEntry = new CorporatePageSectionMedia();
                                fileEntry.dateCreated = DateTime.Now;
                                fileEntry.dateModified = DateTime.Now;
                                fileEntry.corporatePageSectionId = entry.id;
                                fileEntry.mediaSrc = item;
                                fileEntry.mediaType = (int)MediaType.File;
                                if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "Files" && d.mediaSrc == item) && !string.IsNullOrEmpty(item))
                                {
                                    var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "Files" && d.mediaSrc == item);
                                    fileEntry.caption = fieldEntry.caption;
                                    fileEntry.subCaption = fieldEntry.subCaption;
                                    fileEntry.description = fieldEntry.description;
                                    fileEntry.link = fieldEntry.link;
                                }
                                fileEntry.priority = filesCounter;
                                entry.CorporatePageSectionMedias.Add(fileEntry);
                                filesCounter++;
                            }
                            #endregion

                            #endregion

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
                            oldEntry.subtitle = entry.subtitle;
                            oldEntry.subtitle1 = entry.subtitle1;
                            oldEntry.smallDescription = entry.smallDescription;
                            oldEntry.description = entry.description;

                            oldEntry.link = entry.link;
                            oldEntry.labelLink = entry.labelLink;

                            #region Manage template
                            oldEntry.corporatePageTemplateId = entry.templateId;
                            #endregion

                            oldEntry.isPublished = entry.isPublished;
                            oldEntry.dateModified = DateTime.Now;
                            oldEntry.modifiedBy = loggedInUserId;

                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.imgSrcSecondary = entry.imgSrcSecondary;
                            oldEntry.videoUrl = entry.videoUrl;
                            oldEntry.videoSrc = entry.videoSrc;
                            oldEntry.fileSrc = entry.fileSrc;



                            #region Manage Media

                            #region Manage Main Image
                            if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "imgSrc" && d.mediaSrc == entry.imgSrc) && !string.IsNullOrEmpty(entry.imgSrc))
                            {
                                var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "imgSrc" && d.mediaSrc == entry.imgSrc);
                                oldEntry.imgCaption = fieldEntry.caption;
                                oldEntry.imgSubCaption = fieldEntry.subCaption;
                                oldEntry.imgDescription = fieldEntry.description;
                                oldEntry.imgLink = fieldEntry.link;
                            }
                            #endregion

                            #region Manage Secondary Image
                            if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "imgSrcSecondary" && d.mediaSrc == entry.imgSrcSecondary) && !string.IsNullOrEmpty(entry.imgSrcSecondary))
                            {
                                var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "imgSrcSecondary" && d.mediaSrc == entry.imgSrcSecondary);
                                oldEntry.imgSecondaryCaption = fieldEntry.caption;
                                oldEntry.imgSecondarySubCaption = fieldEntry.subCaption;
                                oldEntry.imgSecondaryDescription = fieldEntry.description;
                                oldEntry.imgSecondaryLink = fieldEntry.link;
                            }
                            #endregion

                            #region Manage Images
                            rpstry.DeleteRelatedMedias(oldEntry, (int)MediaType.Image);
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new CorporatePageSectionMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.corporatePageSectionId = entry.id;
                                imageEntry.mediaSrc = item;
                                imageEntry.priority = imagesCounter;
                                imageEntry.mediaType = (int)MediaType.Image;
                                if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "Images" && d.mediaSrc == item) && !string.IsNullOrEmpty(item))
                                {
                                    var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "Images" && d.mediaSrc == item);
                                    imageEntry.caption = fieldEntry.caption;
                                    imageEntry.subCaption = fieldEntry.subCaption;
                                    imageEntry.description = fieldEntry.description;
                                    imageEntry.link = fieldEntry.link;
                                }

                                entry.CorporatePageSectionMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion

                            #region Manage Videos
                            rpstry.DeleteRelatedMedias(oldEntry, (int)MediaType.Video);
                            var listOfVideos = (entry.Videos + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var videosCounter = 1;
                            foreach (var item in listOfVideos)
                            {
                                var videoEntry = new CorporatePageSectionMedia();
                                videoEntry.dateCreated = DateTime.Now;
                                videoEntry.dateModified = DateTime.Now;
                                videoEntry.corporatePageSectionId = entry.id;
                                videoEntry.mediaSrc = item;
                                videoEntry.priority = videosCounter;
                                videoEntry.mediaType = (int)MediaType.Video;
                                if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "Videos" && d.mediaSrc == item) && !string.IsNullOrEmpty(item))
                                {
                                    var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "Videos" && d.mediaSrc == item);
                                    videoEntry.caption = fieldEntry.caption;
                                    videoEntry.subCaption = fieldEntry.subCaption;
                                    videoEntry.description = fieldEntry.description;
                                    videoEntry.link = fieldEntry.link;
                                }
                                entry.CorporatePageSectionMedias.Add(videoEntry);
                                videosCounter++;
                            }
                            #endregion

                            #region Manage Files
                            rpstry.DeleteRelatedMedias(oldEntry, (int)MediaType.File);
                            var listOfFiles = (entry.Files + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var filesCounter = 1;
                            foreach (var item in listOfFiles)
                            {
                                var fileEntry = new CorporatePageSectionMedia();
                                fileEntry.dateCreated = DateTime.Now;
                                fileEntry.dateModified = DateTime.Now;
                                fileEntry.corporatePageSectionId = entry.id;
                                fileEntry.mediaSrc = item;
                                fileEntry.priority = filesCounter;
                                fileEntry.mediaType = (int)MediaType.File;
                                if (entry.MediaFields != null && entry.MediaFields.Any(d => d.fieldName == "Files" && d.mediaSrc == item) && !string.IsNullOrEmpty(item))
                                {
                                    var fieldEntry = entry.MediaFields.FirstOrDefault(d => d.fieldName == "Files" && d.mediaSrc == item);
                                    fileEntry.caption = fieldEntry.caption;
                                    fileEntry.subCaption = fieldEntry.subCaption;
                                    fileEntry.description = fieldEntry.description;
                                    fileEntry.link = fieldEntry.link;
                                }
                                entry.CorporatePageSectionMedias.Add(fileEntry);
                                filesCounter++;
                            }
                            #endregion

                            oldEntry.CorporatePageSectionMedias = entry.CorporatePageSectionMedias;

                            #endregion

                            entry = oldEntry;

                        }
                        #endregion

                        if (submissionType == SubmissionOptions.saveAndPublish)
                        {
                            entry.isPublished = true;
                        }
                        rpstry.Save();

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
                                id = entry.id,
                                languages = languageRpstry.GetAllOptional().Select(e => new
                                {
                                    id = e.id,
                                    title = e.title,
                                    isRightToLeft = e.isRightToLeft,
                                    isFilled = (entry.CorporatePage.CorporatePage1 ?? entry.CorporatePage).CorporatePages.Any(d => !d.isDeleted && d.languageId == e.id)
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
                    var result = rpstry.SortGridRepeaters(model.newIndex, model.oldIndex, model.id, model.specialParam, model.specialParamValue);

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
                        entry.deletedBy = loggedInUserId;
                        entry.dateDeleted = DateTime.Now;
                        rpstry.Save();
                        //parentEntry = entry.ArticleKeyword1;

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    //return Request.CreateResponse(HttpStatusCode.OK, new
                    //{
                    //    languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                    //    {
                    //        id = e.id,
                    //        title = e.title,
                    //        isRightToLeft = e.isRightToLeft,
                    //        isFilled = (parentEntry.ArticleKeyword1 ?? parentEntry).ArticleKeywords.Any(d => !d.isDeleted && d.languageId == e.id)
                    //    })
                    //});
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

        #region Helpers

        #endregion

        #endregion

        #region Front End

        #endregion
    }
}