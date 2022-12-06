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
    [RoutePrefix("api/CorporatePages")]
    public class CorporatePagesController : SharedController<SocketHub>
    {
        private CorporatePageRepository rpstry = new CorporatePageRepository();
        private string kSectionName = "corporatepages";
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
                        imgSrcHover = d.imgSrcHover == "" || d.imgSrcHover == null ? (d.title[0] + "" + d.title[1]) : projectConfigKeys.apiUrl + "/content/uploads/" + corporatePageDirectory + "/" + d.imgSrcHover,
                        priority = d.priority,
                        isPublished = d.isPublished,
                        showRedAlert = d.showRedAlert,
                        showOnSitemap = d.showOnSitemap,
                        showOnNavigation = d.showOnNavigation,
                        dateCreated = d.dateCreated,
                        dateModified = d.dateModified,
                    }).ToDataSourceResult(request);

                    int depth = 1;
                    string grandPaId = "";
                    if (parentId.HasValue)
                    {
                        var CorporatePageEntry = rpstry.GetById(parentId.Value);
                        depth += CorporatePageEntry.Level;
                        grandPaId = CorporatePageEntry.parentId.HasValue ? CorporatePageEntry.parentId + "" : "";
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

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    CorporatePage oldEntry = null;
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
                        entry = entry.CorporatePages.FirstOrDefault(d => !d.isDeleted && d.languageId == languageId);
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
                                showRedAlert = "False",
                                languageId = languageId,
                                languageParentId = languageParentId
                            },
                            fieldsStatus = new
                            {
                                //subTitle = parentId.HasValue ? DirectiveStatus.hidden : "",
                                //imgSrc = parentId.HasValue ? DirectiveStatus.hidden : "",
                                //videoUrl = parentId.HasValue ? DirectiveStatus.hidden : "",
                                RelatedSections = DirectiveStatus.hidden
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                redAlertOptions = redAlertOptions,
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
                                subTitle = entry.subTitle,
                                subTitle1 = entry.subTitle1,
                                link = entry.link,
                                labelLink = entry.labelLink,
                                description = entry.description,
                                smallDescription = entry.smallDescription,
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, corporatePageDirectory),
                                imgSrcHover = new UploadController().GetUploadedFiles(entry.imgSrcHover, null, corporatePageDirectory),
                                showOnNavigation = entry.showOnNavigation,
                                showOnSitemap = entry.showOnSitemap,
                                #region Metas and Custom fields
                                customPageTitle = entry.customPageTitle,
                                customH1Content = entry.customH1Content,
                                customUrlTitle = entry.customUrlTitle,
                                metaImgSrc = new UploadController().GetUploadedFiles(entry.metaImgSrc, null, corporatePageDirectory),
                                metaDescription = entry.metaDescription,
                                metaKeywords = entry.metaKeywords,
                                #endregion
                                showRedAlert = entry.showRedAlert + "",
                                priority = entry.priority,
                                isPublished = entry.isPublished + "",
                            },
                            languages = languageRpstry.GetAllOptional().Select(e => new
                            {
                                id = e.id,
                                title = e.title,
                                isRightToLeft = e.isRightToLeft,
                                isFilled = entry.CorporatePages.Any(d => !d.isDeleted && d.languageId == e.id)
                            }),
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                redAlertOptions = redAlertOptions,
                                logs = new[] {
                                    new {label = "Created on",value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")},
                                    new {label = "Last Modified on",value =entry.dateModified.ToString("dd MMM yyyy HH:mm tt")}
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
                Models.CorporatePage entry = data.ToObject<Models.CorporatePage>();
                if (entry.id == 0)
                {
                    ModelState.Remove("entry.id");
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        bool imageHasBeenChanged = false;
                        #region Create
                        if (entry.id == 0 && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

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

                            oldEntry.parentId = entry.parentId;
                            oldEntry.title = entry.title;
                            oldEntry.subTitle = entry.subTitle;
                            oldEntry.subTitle1 = entry.subTitle1;
                            oldEntry.link = entry.link;
                            oldEntry.labelLink = entry.labelLink;
                            oldEntry.smallDescription = entry.smallDescription;
                            oldEntry.description = entry.description;
                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.imgSrcHover = entry.imgSrcHover;

                            oldEntry.showOnSitemap = entry.showOnSitemap;
                            oldEntry.showOnNavigation = entry.showOnNavigation;

                            #region Metas and Custom fields
                            oldEntry.customPageTitle = entry.customPageTitle;
                            oldEntry.customH1Content = entry.customH1Content;
                            oldEntry.customUrlTitle = entry.customUrlTitle;
                            oldEntry.metaImgSrc = entry.metaImgSrc;
                            oldEntry.metaDescription = entry.metaDescription;
                            oldEntry.metaKeywords = entry.metaKeywords;
                            #endregion

                            oldEntry.showRedAlert = entry.showRedAlert;
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
                                    isFilled = (entry.CorporatePage1 ?? entry).CorporatePages.Any(d => !d.isDeleted && d.languageId == e.id)
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
                    CorporatePage parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();
                        parentEntry = entry.CorporatePage1;

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        languages = parentEntry == null ? null : languageRpstry.GetAllOptional().Select(e => new
                        {
                            id = e.id,
                            title = e.title,
                            isRightToLeft = e.isRightToLeft,
                            isFilled = (parentEntry.CorporatePage1 ?? parentEntry).CorporatePages.Any(d => !d.isDeleted && d.languageId == e.id)
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

        #region Front End
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetAllRegistrationInfos()
        {

            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            var entries = rpstry.GetById(Convert.ToInt32(ConfigurationManager.AppSettings["other-pages-registration-info"])).CorporatePages.Where(d => !d.isDeleted && d.isPublished).OrderByDescending(d => d.priority);
            return Request.CreateResponse(HttpStatusCode.OK,
            entries.Select(d => new
            {
                id = d.id,
                title = d.title,
                description = d.description,
                imgSrc = d.imgSrc == "" || d.imgSrc == null ? null : projectConfigKeys.apiUrl + "/content/uploads/" + corporatePageDirectory + "/" + d.imgSrc,
                imgSrcHover = d.imgSrcHover == "" || d.imgSrcHover == null ? null : projectConfigKeys.apiUrl + "/content/uploads/" + corporatePageDirectory + "/" + d.imgSrcHover,
                infos = d.CorporatePages.Where(f => !f.isDeleted && f.isPublished).OrderByDescending(f => f.priority).Select(f => new
                {
                    id = f.id,
                    title = f.title,
                    description = f.description,
                    imgSrc = f.imgSrc == "" || f.imgSrc == null ? null : projectConfigKeys.apiUrl + "/content/uploads/" + corporatePageDirectory + "/" + f.imgSrc,
                    imgSrcHover = f.imgSrcHover == "" || f.imgSrcHover == null ? null : projectConfigKeys.apiUrl + "/content/uploads/" + corporatePageDirectory + "/" + f.imgSrcHover,
                })
            })
        );
        }

        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GetPageDataById(int id, string imgsize = "", bool? hasDynamicCorporatePageSections = false)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            var requestedLanguageId = GetLanguageId();

            var entry = rpstry.GetById(id);

            var results = new List<object>();
            foreach (var item in entry.CorporatePages.Where(d => !d.isDeleted && d.isPublished).OrderByDescending(d => d.priority))
            {
                var subResults = new List<object>();
                foreach (var subItem in item.CorporatePages.Where(d => !d.isDeleted && d.isPublished).OrderByDescending(d => d.priority))
                {

                    var subSubResults = new List<object>();
                    foreach (var subSubItem in subItem.CorporatePages.Where(d => !d.isDeleted && d.isPublished).OrderByDescending(d => d.priority))
                    {
                        var subSubSubtranslatedItem = requestedLanguageId == -1 ? null : subSubItem.CorporatePages1.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                        subSubResults.Add(new
                        {
                            title = subSubSubtranslatedItem == null ? subSubItem.title : subSubSubtranslatedItem.title,
                            urlTitle = subSubSubtranslatedItem == null ? rpstry.GetUrlTitle(subSubItem.title) : rpstry.GetUrlTitle(subSubSubtranslatedItem.title),
                            subTitle = subSubSubtranslatedItem == null ? subSubItem.subTitle : subSubSubtranslatedItem.subTitle,
                            subTitle1 = subSubSubtranslatedItem == null ? subSubItem.subTitle1 : subSubSubtranslatedItem.subTitle1,
                            smallDescription = subSubSubtranslatedItem == null ? subSubItem.smallDescription : subSubSubtranslatedItem.smallDescription,
                            description = subSubSubtranslatedItem == null ? subSubItem.description : subSubSubtranslatedItem.description,
                            link = subSubItem.link,
                            labelLink = subSubSubtranslatedItem == null ? subSubItem.labelLink : subSubSubtranslatedItem.labelLink,
                            imgSrc = subSubItem.imgSrc == "" || subSubItem.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + subSubItem.imgSrc,
                            imgSrcHover = subSubItem.imgSrcHover == "" || subSubItem.imgSrcHover == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + subSubItem.imgSrcHover,
                        });
                    }

                    var subSubtranslatedItem = requestedLanguageId == -1 ? null : subItem.CorporatePages1.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                    subResults.Add(new
                    {
                        title = subSubtranslatedItem == null ? subItem.title : subSubtranslatedItem.title,
                        urlTitle = subSubtranslatedItem == null ? rpstry.GetUrlTitle(subItem.title) : rpstry.GetUrlTitle(subSubtranslatedItem.title),
                        subTitle = subSubtranslatedItem == null ? subItem.subTitle : subSubtranslatedItem.subTitle,
                        subTitle1 = subSubtranslatedItem == null ? subItem.subTitle1 : subSubtranslatedItem.subTitle1,
                        smallDescription = subSubtranslatedItem == null ? subItem.smallDescription : subSubtranslatedItem.smallDescription,
                        description = subSubtranslatedItem == null ? subItem.description : subSubtranslatedItem.description,
                        link = subItem.link,
                        labelLink = subSubtranslatedItem == null ? subItem.labelLink : subSubtranslatedItem.labelLink,
                        imgSrc = subItem.imgSrc == "" || subItem.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + subItem.imgSrc,
                        imgSrcHover = subItem.imgSrcHover == "" || subItem.imgSrcHover == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + subItem.imgSrcHover,
                        entries = subSubResults
                    });

                }
                var subTranslatedItem = requestedLanguageId == -1 ? null : item.CorporatePages1.FirstOrDefault(lang => lang.languageId == requestedLanguageId);
                results.Add(new
                {
                    title = subTranslatedItem == null ? item.title : subTranslatedItem.title,
                    urlTitle = subTranslatedItem == null ? rpstry.GetUrlTitle(item.title) : rpstry.GetUrlTitle(subTranslatedItem.title),
                    subTitle = subTranslatedItem == null ? item.subTitle : subTranslatedItem.subTitle,
                    subTitle1 = subTranslatedItem == null ? item.subTitle1 : subTranslatedItem.subTitle1,
                    smallDescription = subTranslatedItem == null ? item.smallDescription : subTranslatedItem.smallDescription,
                    description = subTranslatedItem == null ? item.description : subTranslatedItem.description,
                    link = item.link,
                    labelLink = subTranslatedItem == null ? item.labelLink : subTranslatedItem.labelLink,
                    imgSrc = item.imgSrc == "" || item.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + item.imgSrc,
                    imgSrcHover = item.imgSrcHover == "" || item.imgSrcHover == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + item.imgSrcHover,
                    entries = subResults
                });
            }
            var translatedItem = requestedLanguageId == -1 ? null : entry.CorporatePages1.FirstOrDefault(lang => lang.languageId == requestedLanguageId);

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                title = translatedItem == null ? entry.title : translatedItem.title,
                urlTitle = translatedItem == null ? rpstry.GetUrlTitle(entry.title) : rpstry.GetUrlTitle(translatedItem.title),
                subTitle = translatedItem == null ? entry.subTitle : translatedItem.subTitle,
                subTitle1 = translatedItem == null ? entry.subTitle1 : translatedItem.subTitle1,
                smallDescription = translatedItem == null ? entry.smallDescription : translatedItem.smallDescription,
                description = translatedItem == null ? entry.description : translatedItem.description,
                labelLink = translatedItem == null ? entry.labelLink : translatedItem.labelLink,
                link = entry.link,
                imgSrc = entry.imgSrc == "" || entry.imgSrc == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + entry.imgSrc,
                imgSrcHover = entry.imgSrcHover == "" || entry.imgSrcHover == null ? null : projectConfigKeys.apiUrl + (imgsize == "" ? "content/uploads/corporatepages/" : "images/" + imgsize + "/") + entry.imgSrcHover,
                entries = results,
                corporatePageSections = hasDynamicCorporatePageSections.Value ? entry.CorporatePageSections.Where(d => !d.isDeleted).Select(d => new
                {
                    id = d.id,
                    frontHtmlId = d.CorporatePageTemplate.frontHtmlId,
                    frontHtmlClass = d.CorporatePageTemplate.frontHtmlClass,
                }) : null
            });
        }
        #endregion
    }
}