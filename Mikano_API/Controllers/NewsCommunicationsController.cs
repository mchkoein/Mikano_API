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
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/NewsCommunications")]
    public class NewsCommunicationsController : SharedController<SocketHub>
    {
        private NewsCommunicationRepository rpstry = new NewsCommunicationRepository();
        private string kSectionName = "newscommunications";
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

                    return Request.CreateResponse(HttpStatusCode.OK, rpstry.GetAll().Select(d => new
                    {
                        id = d.id,
                        title = d.title,
                        imgSrc = GetGridImage(d.title, d.imgSrc),
                        date = d.date,
                        isFeatured = d.isFeatured,
                        isFeatured1 = d.isFeatured1,
                        isFeatured2 = d.isFeatured2,
                        priority = d.priority,
                        isPublished = d.isPublished,
                        isArchived = d.isArchived,
                        dateCreated = d.dateCreated,
                        dateModified = d.dateModified
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
                            entry = entry.NewsCommunications.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
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
                                isArchived = "False",
                                languageId = languageId,
                                languageParentId = languageParentId
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                archiveOptions = archiveOptions,
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
                                subtitle = entry.subtitle,
                                smallDescription = entry.smallDescription,
                                description = entry.description,
                                date = entry.date.HasValue ? entry.date.Value.ToString("ddd, dd MMM yyyy") : "",
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, newsCommunicationsDirectory),
                                isPublished = entry.isPublished + "",
                                isArchived = entry.isArchived + "",
                                datePublished = entry.datePublished.Value.ToString("ddd, dd MMM yyyy"),
                                dateUnPublished = entry.dateUnPublished.HasValue ? entry.dateUnPublished.Value.ToString("ddd, dd MMM yyyy") : "",

                                #region Images
                                Images = new UploadController().GetUploadedFiles(
                                    files: null,
                                    filesArray: entry.NewsCommunicationMedias.Where(d => d.mediaType == (int)MediaType.Image).Select(d => new MediaModel
                                    {
                                        mediaSrc = d.mediaSrc,
                                        caption = d.caption,
                                        subCaption = d.subCaption,
                                        description = d.description,
                                        link = d.link
                                    }).ToList(),
                                    directory: newsCommunicationsDirectory),
                                #endregion

                                #region Metas and Custom fields
                                customPageTitle = entry.customPageTitle,
                                customH1Content = entry.customH1Content,
                                customUrlTitle = entry.customUrlTitle,
                                metaImgSrc = new UploadController().GetUploadedFiles(entry.metaImgSrc, null, newsCommunicationsDirectory),
                                metaDescription = entry.metaDescription,
                                metaDescriptionSecondary = entry.metaDescriptionSecondary,
                                metaKeywords = entry.metaKeywords,
                                #endregion
                            },
                            languages = languageRpstry.GetAllOptional().Select(e => new
                            {
                                id = e.id,
                                title = e.title,
                                isRightToLeft = e.isRightToLeft,
                                isFilled = entry.NewsCommunications.Any(d => !d.isDeleted && d.languageId == e.id)
                            }),
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                archiveOptions = archiveOptions,
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, NewsCommunication entry, SubmissionOptions submissionType, bool inline = false)
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

                            entry.customUrlTitle = rpstry.GetUrlTitle(entry.title);

                            if (entry.date == null)
                            {
                                entry.date = DateTime.Now;
                            }
                            if (entry.datePublished == null)
                            {
                                entry.datePublished = DateTime.Now;
                            }
                            if (entry.isPublished)
                            {
                                entry.publishedBy = loggedInUserId;
                            }
                            if (entry.isArchived)
                            {
                                entry.archivedBy = loggedInUserId;
                                entry.dateArchived = DateTime.Now;
                            }

                            #region Set Old Featured as false
                            if (entry.isFeatured)
                            {
                                var oldFeatured = rpstry.GetFeatured();
                                if (oldFeatured != null)
                                {
                                    oldFeatured.isFeatured = false;
                                }
                            }

                            #endregion

                            #region Set Old Featured1 as false
                            if (entry.isFeatured1)
                            {
                                var oldFeatured1 = rpstry.GetFeatured1();
                                if (oldFeatured1 != null)
                                {
                                    oldFeatured1.isFeatured1 = false;
                                }
                            }
                            #endregion

                            #region Set Old Featured2 as false
                            if (entry.isFeatured2)
                            {
                                var oldFeatured2 = rpstry.GetFeatured2();
                                if (oldFeatured2 != null)
                                {
                                    oldFeatured2.isFeatured2 = false;
                                }
                            }
                            #endregion

                            entry.dateCreated = DateTime.Now;
                            entry.createdBy = loggedInUserId;
                            entry.dateModified = DateTime.Now;
                            entry.modifiedBy = loggedInUserId;
                            entry.priority = rpstry.GetMaxPriority() + 1;

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

                            #region Manage Images
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new NewsCommunicationMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.newsCommunicationId = entry.id;
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
                                entry.NewsCommunicationMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion

                            #region Manage Videos
                            var listOfVideos = (entry.Videos + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var videosCounter = 1;
                            foreach (var item in listOfVideos)
                            {
                                var videoEntry = new NewsCommunicationMedia();
                                videoEntry.dateCreated = DateTime.Now;
                                videoEntry.dateModified = DateTime.Now;
                                videoEntry.newsCommunicationId = entry.id;
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
                                entry.NewsCommunicationMedias.Add(videoEntry);
                                videosCounter++;
                            }
                            #endregion

                            #region Manage Files
                            var listOfFiles = (entry.Files + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var filesCounter = 1;
                            foreach (var item in listOfFiles)
                            {
                                var fileEntry = new NewsCommunicationMedia();
                                fileEntry.dateCreated = DateTime.Now;
                                fileEntry.dateModified = DateTime.Now;
                                fileEntry.newsCommunicationId = entry.id;
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
                                entry.NewsCommunicationMedias.Add(fileEntry);
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
                            oldEntry.date = entry.date == null ? DateTime.Now : entry.date;
                            oldEntry.smallDescription = entry.smallDescription;
                            oldEntry.description = entry.description;

                            oldEntry.isFeatured = entry.isFeatured;
                            #region Set Old Featured as false
                            if (entry.isFeatured)
                            {
                                var oldFeatured = rpstry.GetFeatured();
                                if (oldFeatured != null)
                                {
                                    oldFeatured.isFeatured = false;
                                }
                            }

                            #endregion

                            oldEntry.isFeatured1 = entry.isFeatured1;
                            #region Set Old Featured as false
                            if (entry.isFeatured1)
                            {
                                var oldFeatured1 = rpstry.GetFeatured1();
                                if (oldFeatured1 != null)
                                {
                                    oldFeatured1.isFeatured1 = false;
                                }
                            }
                            #endregion

                            oldEntry.isFeatured2 = entry.isFeatured2;
                            #region Set Old Featured as false
                            if (entry.isFeatured2)
                            {
                                var oldFeatured2 = rpstry.GetFeatured2();
                                if (oldFeatured2 != null)
                                {
                                    oldFeatured2.isFeatured2 = false;
                                }
                            }
                            #endregion

                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.videoUrl = entry.videoUrl;
                            oldEntry.videoSrc = entry.videoSrc;
                            oldEntry.fileSrc = entry.fileSrc;

                            #region Metas and Custom fields
                            oldEntry.customPageTitle = entry.customPageTitle;
                            oldEntry.customH1Content = entry.customH1Content;
                            oldEntry.customUrlTitle = entry.customUrlTitle;
                            oldEntry.metaImgSrc = entry.metaImgSrc;
                            oldEntry.metaDescription = entry.metaDescription;
                            oldEntry.metaDescriptionSecondary = entry.metaDescriptionSecondary;
                            oldEntry.metaKeywords = entry.metaKeywords;
                            #endregion

                            if (oldEntry.isPublished == false && entry.isPublished == true)
                            {
                                oldEntry.publishedBy = loggedInUserId;
                            }
                            else if (oldEntry.isPublished == true && entry.isPublished == false)
                            {
                                oldEntry.unPublishedBy = loggedInUserId;
                            }
                            oldEntry.isPublished = entry.isPublished;
                            oldEntry.datePublished = entry.datePublished;
                            oldEntry.dateUnPublished = entry.dateUnPublished;

                            if (oldEntry.isArchived == false && entry.isArchived == true)
                            {
                                oldEntry.archivedBy = loggedInUserId;
                                oldEntry.dateArchived = DateTime.Now;
                            }
                            oldEntry.isArchived = entry.isArchived;

                            oldEntry.dateModified = DateTime.Now;
                            oldEntry.modifiedBy = loggedInUserId;

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

                            #region Manage Images
                            rpstry.DeleteRelatedMedias(oldEntry, (int)MediaType.Image);
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new NewsCommunicationMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.newsCommunicationId = entry.id;
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

                                entry.NewsCommunicationMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion

                            #region Manage Videos
                            rpstry.DeleteRelatedMedias(oldEntry, (int)MediaType.Video);
                            var listOfVideos = (entry.Videos + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var videosCounter = 1;
                            foreach (var item in listOfVideos)
                            {
                                var videoEntry = new NewsCommunicationMedia();
                                videoEntry.dateCreated = DateTime.Now;
                                videoEntry.dateModified = DateTime.Now;
                                videoEntry.newsCommunicationId = entry.id;
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
                                entry.NewsCommunicationMedias.Add(videoEntry);
                                videosCounter++;
                            }
                            #endregion

                            #region Manage Files
                            rpstry.DeleteRelatedMedias(oldEntry, (int)MediaType.File);
                            var listOfFiles = (entry.Files + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var filesCounter = 1;
                            foreach (var item in listOfFiles)
                            {
                                var fileEntry = new NewsCommunicationMedia();
                                fileEntry.dateCreated = DateTime.Now;
                                fileEntry.dateModified = DateTime.Now;
                                fileEntry.newsCommunicationId = entry.id;
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
                                entry.NewsCommunicationMedias.Add(fileEntry);
                                filesCounter++;
                            }
                            #endregion



                            oldEntry.NewsCommunicationMedias = entry.NewsCommunicationMedias;

                            #endregion
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
                                    fullName = entry.title,
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
                                    isFilled = (entry.NewsCommunication1 ?? entry).NewsCommunications.Any(d => !d.isDeleted && d.languageId == e.id)
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
                    NewsCommunication parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        entry.deletedBy = loggedInUserId;
                        entry.dateDeleted = DateTime.Now;
                        rpstry.Save();
                        parentEntry = entry.NewsCommunication1;
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                        {
                            id = e.id,
                            title = e.title,
                            isRightToLeft = e.isRightToLeft,
                            isFilled = (parentEntry.NewsCommunication1 ?? parentEntry).NewsCommunications.Any(d => !d.isDeleted && d.languageId == e.id)
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
                var entryTranslatedItem = requestedLanguageId == -1 ? null : entryItem.NewsCommunications.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                entriesResults.Add(new
                {
                    id = entryItem.id + "",
                    title = entryTranslatedItem == null ? entryItem.title : entryTranslatedItem.title,
                });
            }
            return Request.CreateResponse(HttpStatusCode.OK, entriesResults);
        }
        #endregion
    }
}