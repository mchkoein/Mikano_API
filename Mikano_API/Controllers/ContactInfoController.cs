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
    [RoutePrefix("api/ContactInfo")]
    public class ContactInfoController : SharedController<SocketHub>
    {
        private ContactInfoRepository rpstry = new ContactInfoRepository();
        private string kSectionName = "contactinfo";
        private string kActionName = "";


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

                    return Request.CreateResponse(HttpStatusCode.OK,
                        rpstry.GetAll().Select(d => new
                        {
                            id = d.id,
                            imgSrc = d.imgSrc == "" || d.imgSrc == null ? (d.title[0] + "" + d.title[1]) : projectConfigKeys.apiUrl + "/content/uploads/" + contactInfoDirectory + "/" + d.imgSrc,
                            title = d.title,
                            email = d.email,
                            phone = d.phone,
                            priority = d.priority,
                            isMainAddress = d.isMainAddress,
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
                    ContactInfo oldEntry = null;
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
                        entry = entry.ContactInfos.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
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
                                address = entry.address,
                                link = entry.link,
                                email = entry.email,
                                phone = entry.phone,
                                mobile = entry.mobile,
                                fax = entry.fax,
                                whatsapp = entry.whatsapp,
                                directPhone = entry.directPhone,
                                postCode = entry.postCode,
                                hasOrderPickup = entry.hasOrderPickup,
                                openingHour = entry.openingHour.HasValue ? entry.openingHour.Value.ToString("yyyy/MM/dd HH:mm:ss") : "",
                                closingHour = entry.closingHour.HasValue ? entry.closingHour.Value.ToString("yyyy/MM/dd HH:mm:ss") : "",
                                processingTime = entry.processingTime,
                                isMainAddress = entry.isMainAddress,
                                googleMapLatitude = entry.googleMapLatitude,
                                googleMapLongitude = entry.googleMapLongitude,
                                googleMapZoom = entry.googleMapZoom,
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, contactInfoDirectory),
                                priority = entry.priority,
                                isPublished = entry.isPublished + ""
                            },
                            languages = languageRpstry.GetAllOptional().Select(e => new
                            {
                                id = e.id,
                                title = e.title,
                                isRightToLeft = e.isRightToLeft,
                                isFilled = entry.ContactInfos.Any(d => !d.isDeleted && d.languageId == e.id)
                            }),
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                //logs = new[] {
                                //    new {label = "Created on",value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")},
                                //    new {label = "Last Modified on",value =entry.dateModified.ToString("dd MMM yyyy HH:mm tt")}
                                //},
                                infoBarBlocks = new[] {
                                    new
                                    {
                                        title = "Logs",
                                        fields = new[]
                                        {
                                            new {label = "Created on", value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")},
                                            new {label = "Modified on", value = entry.dateModified.ToString("dd MMM yyyy HH:mm tt")},
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, ContactInfo entry, SubmissionOptions submissionType, bool inline = false)
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


                            oldEntry.email = entry.email;
                            oldEntry.title = entry.title;
                            oldEntry.address = entry.address;
                            oldEntry.link = entry.link;
                            oldEntry.phone = entry.phone;
                            oldEntry.fax = entry.fax;
                            oldEntry.mobile = entry.mobile;
                            oldEntry.whatsapp = entry.whatsapp;
                            oldEntry.directPhone = entry.directPhone;
                            oldEntry.postCode = entry.postCode;
                            oldEntry.googleMapLatitude = entry.googleMapLatitude;
                            oldEntry.googleMapLongitude = entry.googleMapLongitude;
                            oldEntry.googleMapZoom = entry.googleMapZoom;
                            oldEntry.hasOrderPickup = entry.hasOrderPickup;
                            oldEntry.isMainAddress = entry.isMainAddress;
                            oldEntry.imgSrc = entry.imgSrc;
                            if (entry.openingHour.HasValue)
                            {
                                oldEntry.openingHour = entry.openingHour.Value.ToLocalTime();
                            }
                            else
                            {
                                oldEntry.openingHour = null;
                            }
                            if (entry.closingHour.HasValue)
                            {
                                oldEntry.closingHour = entry.closingHour.Value.ToLocalTime();
                            }
                            else
                            {
                                oldEntry.closingHour = null;
                            }
                            oldEntry.processingTime = entry.processingTime;
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
                                    isFilled = (entry.ContactInfo1 ?? entry).ContactInfos.Any(d => !d.isDeleted && d.languageId == e.id)
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
                    ContactInfo parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();
                        parentEntry = entry.ContactInfo1;

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                        {
                            id = e.id,
                            title = e.title,
                            isRightToLeft = e.isRightToLeft,
                            isFilled = (parentEntry.ContactInfo1 ?? parentEntry).ContactInfos.Any(d => !d.isDeleted && d.languageId == e.id)
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
        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetData(string imgsize = "", string imgsize1 = "", string imgsize2 = "")
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            var requestedLanguageId = GetLanguageId();

            var entriesResults = new List<object>();
            var entries = rpstry.GetAllIsPublished();

            foreach (var entryItem in entries)
            {
                var entryTranslatedItem = requestedLanguageId == -1 ? null : entryItem.ContactInfos.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                entriesResults.Add(new
                {
                    id = entryItem.id,
                    title = entryTranslatedItem == null ? entryItem.title : entryTranslatedItem.title,
                    address = entryTranslatedItem == null ? entryItem.address : entryTranslatedItem.address,
                    fax = entryItem.fax,
                    phone = entryItem.phone,
                    email = entryItem.email,
                    link = entryItem.link,
                    mobile = entryItem.mobile,
                    postCode = entryItem.postCode,
                    directPhone = entryItem.directPhone,
                    googleMapLatitude = entryItem.googleMapLatitude,
                    googleMapLongitude = entryItem.googleMapLongitude,
                    googleMapZoom = entryItem.googleMapZoom,
                    whatsapp = entryItem.whatsapp,
                    isMainAddress = entryItem.isMainAddress,
                    #region imgSrc
                    imgSrc = GetResizedImagePath(projectConfigKeys, entryItem.imgSrc, imgsize, contactInfoDirectory),
                    smallImgSrc = GetResizedImagePath(projectConfigKeys, entryItem.imgSrc, imgsize1, contactInfoDirectory),
                    largeImgSrc = GetResizedImagePath(projectConfigKeys, entryItem.imgSrc, imgsize2, contactInfoDirectory),
                    #endregion
                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, entriesResults);
        }
        #endregion
    }
}