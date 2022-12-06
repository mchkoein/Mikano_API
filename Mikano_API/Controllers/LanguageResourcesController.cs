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
using WebApi.OutputCache.V2;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/LanguageResources")]
    public class LanguageResourcesController : SharedController<SocketHub>
    {
        private LanguageResourceRepository rpstry = new LanguageResourceRepository();
        private string kSectionName = "LanguageResources";
        private string kActionName = "";


        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
        {
            LanguageRepository languagesRpstry = new LanguageRepository();
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
                    var availableLanguages = languagesRpstry.GetAllIsPublished().OrderBy(d => d.priority);

                    var data = new List<Dictionary<string, object>>();
                    foreach (var item in rpstry.GetAll())
                    {
                        var tempObj = new Dictionary<string, object>();
                        tempObj.Add("id", item.id);
                        tempObj.Add("title", item.title);
                        foreach (var lang in availableLanguages)
                        {
                            var tempTranslation = item.LanguageResourceTranslations.FirstOrDefault(d => d.languageId == lang.id);
                            tempObj.Add("langid_" + lang.id, tempTranslation == null ? "" : tempTranslation.title);
                        }
                        tempObj.Add("dateCreated", item.dateCreated);
                        tempObj.Add("dateModified", item.dateModified);
                        data.Add(tempObj);
                    }


                    return Request.CreateResponse(HttpStatusCode.OK,
                        data.ToDataSourceResult(request)
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
        public HttpResponseMessage GetGridConfiguration(int? parentId = null)
        {
            LanguageRepository languagesRpstry = new LanguageRepository();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.read);
            if (hasPermissions)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var availableLanguages = languagesRpstry.GetAllIsPublished().OrderBy(d => d.priority);
                    var newColumns = new List<Dictionary<string, object>>();
                    foreach (var item in availableLanguages)
                    {
                        var tempColumns = new Dictionary<string, object>();
                        tempColumns.Add("title", item.title);
                        tempColumns.Add("field", "langid_" + item.id);
                        tempColumns.Add("attributes", new
                        {
                            title = "item.title",
                            @class = ""
                        });
                        newColumns.Add(tempColumns);
                    }
                    var results = new
                    {
                        columns = newColumns
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, results);
                }
                catch (Exception e)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { message = "Authorization has been denied for this request." });
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
                    var entry = rpstry.GetById(id);

                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);



                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                            },
                            additionalData = new
                            {
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
                            },
                            additionalData = new
                            {
                                logs = new[] {
                                    new {label = "Created on",value =entry.dateCreated.ToString("dd MMM yyyy HH:mm tt")},
                                    new {label = "Last Modified on",value =entry.dateModified.ToString("dd MMM yyyy HH:mm tt")}
                                },
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
                if (Convert.ToString(data.GetValue("id")) == "0")
                {
                    ModelState.Remove("entry.id");
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        LanguageResource entry = new LanguageResource();
                        var tempEntry = rpstry.GetByTitle(Convert.ToString(data.GetValue("title")));

                        #region Create
                        if (Convert.ToString(data.GetValue("id")) == "0" && hasPermissionsToCreate)
                        {
                            if (tempEntry != null)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "this key already exists" });

                            }

                            kActionName = KActions.create.ToString();

                            entry.title = Convert.ToString(data.GetValue("title"));
                            entry.dateCreated = DateTime.Now;
                            entry.dateModified = DateTime.Now;
                            entry.isDeleted = false;
                            rpstry.Add(entry);

                            foreach (var item in data)
                            {
                                LanguageResourceTranslation resourceByLanguage = new LanguageResourceTranslation();
                                if (item.ToString().Contains("langid_"))
                                {
                                    string[] x = item.Key.ToString().Split('_');
                                    resourceByLanguage.languageId = Convert.ToInt32(x[1]);
                                    resourceByLanguage.title = item.Value.ToString();
                                    resourceByLanguage.dateCreated = DateTime.Now;
                                    resourceByLanguage.dateModified = DateTime.Now;
                                    entry.LanguageResourceTranslations.Add(resourceByLanguage);
                                }
                            }
                        }
                        #endregion
                        #region Update
                        else if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            var oldEntry = rpstry.GetById(Convert.ToInt32(data.GetValue("id")));
                            if (tempEntry != null && tempEntry.id != oldEntry.id)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "this key already exists" });
                            }

                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, Convert.ToInt32(data.GetValue("id")));
                            }

                            oldEntry.title = data["title"].ToObject<string>();
                            oldEntry.dateModified = DateTime.Now;
                            entry = oldEntry;

                            foreach (var item in data)
                            {
                                if (item.ToString().Contains("langid_"))
                                {
                                    string[] x = item.Key.ToString().Split('_');
                                    LanguageResourceTranslation resourceByLanguage = entry.LanguageResourceTranslations.FirstOrDefault(d => d.languageId == Convert.ToInt32(x[1]));
                                    if (resourceByLanguage == null)
                                    {
                                        resourceByLanguage = new LanguageResourceTranslation();
                                        resourceByLanguage.languageId = Convert.ToInt32(x[1]);
                                        resourceByLanguage.title = item.Value.ToString();
                                        resourceByLanguage.dateCreated = DateTime.Now;
                                        resourceByLanguage.dateModified = DateTime.Now;
                                        entry.LanguageResourceTranslations.Add(resourceByLanguage);
                                    }
                                    else
                                    {
                                        resourceByLanguage.languageId = Convert.ToInt32(x[1]);
                                        resourceByLanguage.title = item.Value.ToString();
                                        resourceByLanguage.dateModified = DateTime.Now;
                                    }
                                }
                            }
                        }
                        #endregion

                        rpstry.Save();

                        ClearCache();

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
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, data.GetValue("id") + "", data.GetValue("title") + "", "", GetIpAddress(Request), true);
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
                    ClearCache();
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
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


        #region Languages Dictionary
        [HttpGet]
        [AllowAnonymous]
        [CacheOutput(ServerTimeSpan = 7200)]
        public HttpResponseMessage LanguageDictionary(string lang)
        {
            var requestedLanguageId = GetLanguageId();
            var dictionary = new Dictionary<string, string>();
            var entries = rpstry.GetAll().GroupBy(d => d.title).Select(d => d.FirstOrDefault());
            foreach (var item in entries)
            {
                var temptranslation = requestedLanguageId == -1 ? null : item.LanguageResourceTranslations.FirstOrDefault(d => d.languageId == requestedLanguageId);
                dictionary.Add(item.title, temptranslation == null ? "" : temptranslation.title);
            }
            return Request.CreateResponse(HttpStatusCode.OK, dictionary);
        }
        #endregion

    }
}