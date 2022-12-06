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
    [RoutePrefix("api/Companies")]
    public class CompaniesController : SharedController<SocketHub>
    {
        private CompanyRepository rpstry = new CompanyRepository();
        private string kSectionName = "Companies";
        private string kActionName = "";

        #region Back end
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
                        imgSrc = GetGridImage(d.title, d.imgSrc),
                        priority = d.priority,
                        isPublished = d.isPublished,
                        dateCreated = d.dateCreated,
                        dateModified = d.dateModified,
                    }).ToDataSourceResult(request);

                    int depth = 1;
                    string grandPaId = "";
                    if (parentId.HasValue)
                    {
                        var CompanyEntry = rpstry.GetById(parentId.Value);
                        depth += CompanyEntry.Level;
                        grandPaId = CompanyEntry.parentId.HasValue ? CompanyEntry.parentId + "" : "";
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
        public HttpResponseMessage GetById(int id, int? parentId = null, int? languageId = null)
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
                    Company oldEntry = null;
                    var entry = oldEntry = rpstry.GetById(id);
                    var languageDirectionIsRtl = false;
                    int? languageParentId = null;
                    string languageTitle = null;

                    if (languageId.HasValue)
                    {
                        entry = oldEntry = rpstry.GetById(parentId.Value);
                        //if (entry != null)
                        //{
                        //    languageParentId = entry.languageParentId;
                        //    entry = entry.Companies.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
                        //}
                        //else
                        //{
                        //    languageParentId = parentId;
                        //}

                        if (entry != null)
                        {
                            languageParentId = entry.id;
                        }
                        entry = entry.Companies.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
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
                            fieldsStatus = new
                            {
                                //subTitle = parentId.HasValue ? DirectiveStatus.hidden : "",
                                //imgSrc = parentId.HasValue ? DirectiveStatus.hidden : "",
                                //videoUrl = parentId.HasValue ? DirectiveStatus.hidden : "",
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
                                parentId = entry.parentId,
                                title = entry.title,
                                aliasTitle = entry.aliasTitle,
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, companiesDirectory),
                                imgSrcSecondary = new UploadController().GetUploadedFiles(entry.imgSrcSecondary, null, companiesDirectory),
                                description = entry.description,
                                smallDescription = entry.smallDescription,

                                #region Contact Info
                                address = entry.address,
                                countryId = entry.countryId,
                                email = entry.email,
                                phone = entry.phone,
                                mobile = entry.mobile,
                                whatsapp = entry.whatsapp,
                                directPhone = entry.directPhone,
                                postCode = entry.postCode,
                                fax = entry.fax,
                                link = entry.link,
                                googleMapLatitude = entry.googleMapLatitude,
                                googleMapLongitude = entry.googleMapLongitude,
                                googleMapZoom = entry.googleMapZoom,
                                #endregion

                                #region Social Media
                                facebook = entry.facebook,
                                twitter = entry.twitter,
                                linkedin = entry.linkedin,
                                instagram = entry.instagram,
                                #endregion

                                #region Metas and Custom fields
                                customPageTitle = entry.customPageTitle,
                                customH1Content = entry.customH1Content,
                                customUrlTitle = entry.customUrlTitle,
                                metaImgSrc = new UploadController().GetUploadedFiles(entry.metaImgSrc, null, companiesDirectory),
                                metaDescription = entry.metaDescription,
                                metaKeywords = entry.metaKeywords,
                                #endregion

                                priority = entry.priority,
                                isPublished = entry.isPublished + "",
                            },
                            languages = languageRpstry.GetAllOptional().Select(e => new
                            {
                                id = e.id,
                                title = e.title,
                                isRightToLeft = e.isRightToLeft,
                                isFilled = entry.Companies.Any(d => !d.isDeleted && d.languageId == e.id)
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
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, JObject data, SubmissionOptions submissionType, bool inline = false)
        {

            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            LanguageRepository languageRpstry = new LanguageRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                Models.Company entry = data.ToObject<Models.Company>();
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

                            entry.dateCreated = DateTime.Now;
                            entry.createdBy = loggedInUserId;
                            entry.dateModified = DateTime.Now;
                            entry.modifiedBy = loggedInUserId;
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

                            oldEntry.parentId = entry.parentId;
                            oldEntry.title = entry.title;
                            oldEntry.aliasTitle = entry.aliasTitle;
                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.imgSrcSecondary = entry.imgSrcSecondary;
                            oldEntry.description = entry.description;
                            oldEntry.smallDescription = entry.smallDescription;

                            #region Contact Info
                            oldEntry.address = entry.address;
                            oldEntry.countryId = entry.countryId;
                            oldEntry.email = entry.email;
                            oldEntry.phone = entry.phone;
                            oldEntry.mobile = entry.mobile;
                            oldEntry.whatsapp = entry.whatsapp;
                            oldEntry.directPhone = entry.directPhone;
                            oldEntry.postCode = entry.postCode;
                            oldEntry.fax = entry.fax;
                            oldEntry.link = entry.link;
                            oldEntry.googleMapLatitude = entry.googleMapLatitude;
                            oldEntry.googleMapLongitude = entry.googleMapLongitude;
                            oldEntry.googleMapZoom = entry.googleMapZoom;
                            #endregion

                            #region Social Media
                            oldEntry.facebook = entry.facebook;
                            oldEntry.twitter = entry.twitter;
                            oldEntry.linkedin = entry.linkedin;
                            oldEntry.instagram = entry.instagram;
                            #endregion

                            #region Metas and Custom fields
                            oldEntry.customPageTitle = entry.customPageTitle;
                            oldEntry.customH1Content = entry.customH1Content;
                            oldEntry.customUrlTitle = entry.customUrlTitle;
                            oldEntry.metaImgSrc = entry.metaDescription;
                            oldEntry.metaDescription = entry.metaDescription;
                            oldEntry.metaKeywords = entry.metaKeywords;
                            #endregion

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
                                languages = languageRpstry.GetAllOptional().Select(e => new
                                {
                                    id = e.id,
                                    title = e.title,
                                    isRightToLeft = e.isRightToLeft,
                                    isFilled = (entry.Company1 ?? entry).Companies.Any(d => !d.isDeleted && d.languageId == e.id)
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
                    Company parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();
                        parentEntry = entry.Company1;

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                        {
                            id = e.id,
                            title = e.title,
                            isRightToLeft = e.isRightToLeft,
                            isFilled = (parentEntry.Company1 ?? parentEntry).Companies.Any(d => !d.isDeleted && d.languageId == e.id)
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


        [HttpGet]
        public HttpResponseMessage getCountries([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
        {
            CountryRepository countryRpstry = new CountryRepository();

            var results = countryRpstry.GetAll();
            var castedResults = results.Select(d => new
            {
                id = d.id,
                title = d.name,
            });

            return Request.CreateResponse(HttpStatusCode.OK, castedResults.ToDataSourceResult(request));
        }
        #endregion

        #region Front End
        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetPageDataById(int id, string imgsize = "", bool? hasDynamicCorporatePageSections = false)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            var requestedLanguageId = GetLanguageId();

            var entry = rpstry.GetById(id);

            var results = new List<object>();
            foreach (var item in entry.Companies.Where(d => !d.isDeleted && d.isPublished).OrderByDescending(d => d.priority))
            {
                var subResults = new List<object>();
                foreach (var subItem in item.Companies.Where(d => !d.isDeleted && d.isPublished).OrderByDescending(d => d.priority))
                {

                    var subSubResults = new List<object>();
                    foreach (var subSubItem in subItem.Companies.Where(d => !d.isDeleted && d.isPublished).OrderByDescending(d => d.priority))
                    {
                        var subSubSubtranslatedItem = requestedLanguageId == -1 ? null : subSubItem.Companies.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                        subSubResults.Add(new
                        {
                            title = subSubSubtranslatedItem == null ? subSubItem.title : subSubSubtranslatedItem.title,
                            urlTitle = subSubSubtranslatedItem == null ? rpstry.GetUrlTitle(subSubItem.title) : rpstry.GetUrlTitle(subSubSubtranslatedItem.title),
                            smallDescription = subSubSubtranslatedItem == null ? subSubItem.smallDescription : subSubSubtranslatedItem.smallDescription,
                            description = subSubSubtranslatedItem == null ? subSubItem.description : subSubSubtranslatedItem.description,
                            link = subSubItem.link,
                            imgSrc = subSubItem.imgSrc == "" || subSubItem.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/Companies/" : "images/" + imgsize + "/") + subSubItem.imgSrc,
                        });
                    }

                    var subSubtranslatedItem = requestedLanguageId == -1 ? null : subItem.Companies.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                    subResults.Add(new
                    {
                        title = subSubtranslatedItem == null ? subItem.title : subSubtranslatedItem.title,
                        urlTitle = subSubtranslatedItem == null ? rpstry.GetUrlTitle(subItem.title) : rpstry.GetUrlTitle(subSubtranslatedItem.title),
                        smallDescription = subSubtranslatedItem == null ? subItem.smallDescription : subSubtranslatedItem.smallDescription,
                        description = subSubtranslatedItem == null ? subItem.description : subSubtranslatedItem.description,
                        link = subItem.link,
                        imgSrc = subItem.imgSrc == "" || subItem.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/Companies/" : "images/" + imgsize + "/") + subItem.imgSrc,
                        entries = subSubResults
                    });

                }
                var subTranslatedItem = requestedLanguageId == -1 ? null : item.Companies.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                results.Add(new
                {
                    title = subTranslatedItem == null ? item.title : subTranslatedItem.title,
                    urlTitle = subTranslatedItem == null ? rpstry.GetUrlTitle(item.title) : rpstry.GetUrlTitle(subTranslatedItem.title),
                    smallDescription = subTranslatedItem == null ? item.smallDescription : subTranslatedItem.smallDescription,
                    description = subTranslatedItem == null ? item.description : subTranslatedItem.description,
                    link = item.link,
                    imgSrc = item.imgSrc == "" || item.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/Companies/" : "images/" + imgsize + "/") + item.imgSrc,
                    entries = subResults
                });
            }
            var translatedItem = requestedLanguageId == -1 ? null : entry.Companies.FirstOrDefault(lang => lang.languageId == requestedLanguageId);

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                title = translatedItem == null ? entry.title : translatedItem.title,
                urlTitle = translatedItem == null ? rpstry.GetUrlTitle(entry.title) : rpstry.GetUrlTitle(translatedItem.title),
                smallDescription = translatedItem == null ? entry.smallDescription : translatedItem.smallDescription,
                description = translatedItem == null ? entry.description : translatedItem.description,
                link = entry.link,
                imgSrc = entry.imgSrc == "" || entry.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/Companies/" : "images/" + imgsize + "/") + entry.imgSrc,
                entries = results,

            });
        }
        #endregion
    }
}