using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Http;
using WebApi.OutputCache.V2;
using Mikano_API.Helpers;
using Mikano_API.Models;
using static Mikano_API.Models.KMSEnums;
using System.Net.Security;
using System.Text.RegularExpressions;
using Mikano_API.EtisalatMEssagingReference;
using System.Collections.Specialized;
using System.Dynamic;

namespace Mikano_API.Controllers
{
    public abstract class SharedController<THub> : ApiController
        where THub : IHub
    {
        //internal KMSLogRepository logRpstry = new KMSLogRepository();
        //internal EKomBasketLogRepository basketLogRpstry = new EKomBasketLogRepository();
        //internal UtilsHelper utilsHelper = new UtilsHelper();
        //private SettingsRepository settingsRpstry = new SettingsRepository();
        //internal EndUsersRepository accountRpstry = new EndUsersRepository();
        //internal EKomBasketRepository basketRpstry = new EKomBasketRepository();
        //internal BillingAddressRepository billingRpstry = new BillingAddressRepository();
        //internal ShippingAddressRepository shippingRpstry = new ShippingAddressRepository();
        //internal EmailTemplateRepository emailTemplateRpstry = new EmailTemplateRepository();
        //internal LanguageRepository languageRpstry = new LanguageRepository();

        //internal ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

        //internal static KConfigRepository kConfigStaticRpstry = new KConfigRepository();



        //internal string apiUrl = kConfigStaticRpstry.db.fnConfigKey("ApiUrl", "project urls");
        //internal string frontUrl = kConfigStaticRpstry.db.fnConfigKey("FrontUrl", "project urls");
        //internal string kmsUrl = kConfigStaticRpstry.db.fnConfigKey("KmsUrl", "project urls");
        //internal string projectName = kConfigStaticRpstry.db.fnConfigKey("name", "project info");
        //internal string defaultGiftCardImage = kConfigStaticRpstry.db.fnConfigKey("default-giftCard-image", "project info");
        //internal string defaultProductImage = kConfigStaticRpstry.db.fnConfigKey("default-product-image", "project info");

        //internal int giftCardTypeId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("giftcard-type-id", "display ins"));
        //internal int giftWrapTypeId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("giftwrap-type-id", "display ins"));
        //internal int showinMenuTypeId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("show-in-menu-type-id", "display ins"));
        //internal int specialOfferTypeId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("special-offer-type-id", "display ins"));
        //internal int loyaltyProgramTypId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("loyalty-program-type-id", "display ins"));

        //internal bool validationBySmsIsEnabled = Convert.ToBoolean(kConfigStaticRpstry.db.fnConfigKey("validation-by-sms-is-enabled", "project info"));

        //internal string requestOriginHeaderKey = kConfigStaticRpstry.db.fnConfigKey("RequestOriginHeaderKey", "project info");
        //internal int pushNotificationAppId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("PushNotificationAppId", "project info"));
        //internal int shippingCompanyAramexId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("ShippingCompany_AramexId", "shipping"));

        //internal int emailTpWelcomeId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("email-welcome-id", "email templates"));
        //internal int emailTpOrderStatusId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("email-orderstatus-id", "email templates"));
        //internal int emailTpProductReviewAdminId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("email-product-review-admin-id", "email templates"));
        //internal int emailTpResetpasswordId = Convert.ToInt32(kConfigStaticRpstry.db.fnConfigKey("email-resetpassword-id", "email templates"));



        //Keep them in alphabetical order
        internal string boutiquesDirectory = "boutiques";
        internal string brandDirectory = "brands";
        internal string careerApplicationsDirectory = "careerApplications";
        internal string categoriesDirectory = "ekomcategories";
        internal string colorDirectory = "colors";
        internal string companiesDirectory = "companies";
        internal string contactInfoDirectory = "contactinfo";
        internal string corporatePageDirectory = "corporatepages";
        internal string corporatePageSectionsDirectory = "corporatePageSections";
        internal string corporatePageTemplatesDirectory = "corporatePageTemplates";
        internal string customBoxDirectory = "customboxes";
        internal string dailyDishesDirectory = "dailyDishes";
        internal string directorsDirectory = "directors";
        internal string eKomBlogArticlesDirectory = "ekomblogarticles";
        internal string eKomCollectionsDirectory = "ekomcollections";
        internal string eKomNewsDirectory = "eKomNews";
        internal string eKomPaymentMethodsDirectory = "ekompaymentmethods";
        internal string eKomProductsDirectory = "ekomproducts";
        internal string franchiseInquiryDirectory = "franchiseinquiries";
        internal string homePageBoxDirectory = "homepageboxes";
        internal string inquiryDirectory = "inquiries";
        internal string managementsDirectory = "managements";
        internal string mediaPhotoGallerydirectory = "mediaphotogalleries";
        internal string menuBoxDirectory = "menuboxes";
        internal string newsCommunicationsDirectory = "newsCommunications";
        internal string projectDirectory = "project";
        internal string reportsDirectory = "reports";
        internal string settingsDirectory = "settings";
        internal string slideshowMediaDirectory = "slideshows";
        internal string newsDirectory = "news";
        internal string BlogPostsDirectory = "BlogPosts";
        internal string subDivisionDirectory = "DivisionSubCategories";
        internal string DivisionsDirectory = "divisions";
        internal string ProductsDirectory = "products";
        internal string partnersDirectory = "partners";
        internal string projectsDirectory = "projects";
        internal string historyDirectory = "histories";
        internal string appsettingDirectory = "appsettings";
        internal string CustomCareerAppsDirectory = "CustomCareerApps";
        internal string IconsDirectory = "IconLists";

        internal string loggedInUserId;
        internal object publishOptions = new[] { new { id = "False", label = "Unpublished" }, new { id = "True", label = "Published" } };
        internal object showInMenuOptions = new[] { new { id = "False", label = "no" }, new { id = "True", label = "yes" } };
        internal object redAlertOptions = new[] { new { id = "False", label = "Don't Show" }, new { id = "True", label = "Show" } };
        internal object templateOptions = new[] { new { id = "template1", label = "Template 1" }, new { id = "template2", label = "Template 2" }, new { id = "template3", label = "Template 3" } };
        internal object completedOptions = new[] { new { id = "False", label = "Incomplete" }, new { id = "True", label = "Completed" } };
        internal object feasibilityRequest = new[] { new { id = "False", label = "False" }, new { id = "True", label = "True" } };
        internal object genderOptions = new[] { new { id = "False", label = "Male" }, new { id = "True", label = "Female" } };
        internal object weeklyRecurrenceOptions = new[] {
            new {
                id = weeklyRecurrence.Monday.ToString(), label = "Every Monday"
            },
            new {
                id = weeklyRecurrence.Tuesday.ToString(), label = "Every Tuesday"
            },
            new {
                id = weeklyRecurrence.Wednesday.ToString(), label = "Every Wednesday"
            },
            new {
                id = weeklyRecurrence.Thursday.ToString(), label = "Every Thursday"
            },
            new {
                id = weeklyRecurrence.Friday.ToString(), label = "Every Friday"
            },
            new {
                id = weeklyRecurrence.Saturday.ToString(), label = "Every Saturday"
            },
            new {
                id = weeklyRecurrence.Daily.ToString(), label = "Every Day"
            }
        };
        internal object archiveOptions = new[] { new { id = "False", label = "Unarchived" }, new { id = "True", label = "Archived" } };


        internal NameValueCollection ProjectKeysConfiguration = (NameValueCollection)ConfigurationManager.GetSection("ProjectKeysConfig");

        internal Lazy<IHubContext> hub = new Lazy<IHubContext>(() => GlobalHost.ConnectionManager.GetHubContext<THub>());

        internal Setting settingsEntry = new SettingsRepository().GetFirstOrDefault();

        protected IHubContext Hub
        {
            get { return hub.Value; }
        }

        internal string CollectRequestData(HttpRequestMessage request, object postData)
        {
            return "";
            request = request ?? Request;

            string result = "{";
            if (request.Headers != null && request.Headers.Any())
            {
                result += "Headers:" + JsonConvert.SerializeObject(request.Headers) + ",";
            }
            if (request.GetQueryNameValuePairs() != null && request.GetQueryNameValuePairs().Any())
            {
                result += "Query:" + JsonConvert.SerializeObject(request.GetQueryNameValuePairs()) + ",";
            }
            if (postData != null)
            {
                var objectType = postData.GetType();
                var properties = objectType.GetProperties().Where(d => d.PropertyType.IsSerializable);
                Dictionary<string, object> newData = new Dictionary<string, object>();
                foreach (var prop in properties)
                {
                    newData.Add(prop.Name, prop.GetValue(postData, null));
                }

                result += "Post Data:" + JsonConvert.SerializeObject(newData) + ",";
            }

            if (User.Identity != null && ((ClaimsIdentity)User.Identity).Claims != null && ((ClaimsIdentity)User.Identity).Claims.Any())
            {
                result += "User:" + JsonConvert.SerializeObject(((ClaimsIdentity)User.Identity).Claims.Select(s => new
                {
                    Issuer = s.Issuer,
                    OriginalIssuer = s.OriginalIssuer,
                    Type = s.Type,
                    Value = s.Value,
                    ValueType = s.ValueType
                })) + ",";
            }
            if (request.RequestUri != null)
            {
                result += "Uri:" + JsonConvert.SerializeObject(request.RequestUri) + ",";
            }

            return result + "}";
        }

        internal string GetIpAddress(HttpRequestMessage request)
        {
            request = request ?? Request;
            const string HttpContext = "MS_HttpContext";
            const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }
            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }
            return null;
        }

        internal string GetBase64(string path)
        {
            using (Image image = Image.FromFile(path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();

                    // Convert byte[] to Base64 String
                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }

        internal string CsvEscape(string value)
        {
            if (!string.IsNullOrEmpty(value) && value.Contains(","))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        internal bool SendSmsMessage(string text, string mobile)
        {
            try
            {
                ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
                SettingsRepository settingsRpstry = new SettingsRepository();

                var regex0 = new Regex(Regex.Escape("+"));
                var regex = new Regex(Regex.Escape("00"));
                var regex1 = new Regex(Regex.Escape("96103"));
                mobile = mobile.Replace(" ", "");
                if (mobile.Length <= 8)
                {
                    mobile = "961" + mobile;
                }
                mobile = (mobile + "").StartsWith("+") ? regex0.Replace(mobile, "", 1) : mobile;
                mobile = mobile.StartsWith("00") ? regex.Replace(mobile, "", 1) : mobile;
                mobile = mobile.StartsWith("96103") ? regex1.Replace(mobile, "9613", 1) : mobile;
                #region EBeirut

                string smsUrl = projectConfigKeys.SendSMSURL;
                string username = projectConfigKeys.SendSMSUsername;
                string password = projectConfigKeys.SendSMSPassword;
                string sender = projectConfigKeys.projectName;

                using (var client = new WebClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                        new RemoteCertificateValidationCallback(
                        delegate
                        { return true; }
                        );
                    var results = client.DownloadString(smsUrl + "?user=" + username + "&password=" + password + "&sender=" + sender + "&SMSText=" + text + "&GSM=" + mobile);
                    return !results.StartsWith("-");
                }
                #endregion

                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        internal void RemoveStaticDataCache(bool manualRemove = false, bool imagesHasBeenChanged = false)
        {
            //var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
            //cache.RemoveStartsWith(Configuration.CacheOutputConfiguration().MakeBaseCachekey((SettingsController t) => t.GetStaticData()));
            //cache.RemoveStartsWith(Configuration.CacheOutputConfiguration().MakeBaseCachekey((SettingsController t) => t.GetStaticFileData()));
            //cache.RemoveStartsWith(Configuration.CacheOutputConfiguration().MakeBaseCachekey((SettingsController t) => t.GetStaticDataLastChanged()));
            //cache.RemoveStartsWith(Configuration.CacheOutputConfiguration().MakeBaseCachekey((SettingsController t) => t.GetStaticImages()));

            //if (!manualRemove)
            //{
            //    var config = settingsRpstry.GetFirstOrDefault();
            //    config.staticDataLastChanged = DateTime.Now;
            //    if (imagesHasBeenChanged)
            //    {
            //        config.staticImageLastChanged = DateTime.Now;
            //    }
            //    settingsRpstry.Save();
            //}
        }

        internal AspNetUser GetLoggedUser()
        {
            if (User.Identity.IsAuthenticated)
            {
                EndUsersRepository accountRpstry = new EndUsersRepository();
                return accountRpstry.GetByUserName(User.Identity.Name);
            }
            else
            {
                return null;
            }
        }



        internal int GetLanguageId()
        {
            var languageCodeObject = Request.Headers.FirstOrDefault(d => d.Key.ToLower() == "language");

            if (languageCodeObject.Value == null || languageCodeObject.Value.FirstOrDefault() == "null")
            {
                return -1;
            }
            else
            {
                LanguageRepository languageRpstry = new LanguageRepository();

                Language entryLanguage = languageRpstry.GetByCode(languageCodeObject.Value.FirstOrDefault());

                return entryLanguage == null || entryLanguage.isDefault ? -1 : entryLanguage.id;
            }
        }

        internal dynamic GetLanguageTranslation(dynamic obj, string name, dynamic objTranslated)
        {

            Type objType = obj.GetType();
            var objVal = objType.GetProperty(name);


            //if (objType == typeof(ExpandoObject))
            //{
            //    return ((IDictionary<string, object>)obj).ContainsKey(name);
            //}
            var objTranslatedCount = 1;
            var hasCount = true;

            try
            {
                if (objTranslated != null)
                {
                    objTranslatedCount = objTranslated.Count;
                }
            }
            catch (Exception e)
            {
                hasCount = false;
            }

            if (objTranslated != null && objTranslatedCount > 0 && objType.GetProperty(name) != null) // check if obj Translated exists
            {
                Type objTypeTranslated = objTranslated.GetType();
                if (hasCount)
                {
                    if (objType.GetProperty(name).GetValue(objTranslated[0], null) == null) // If obj Translated doesn't have filled field, return empty string OR default translation
                    {
                        return "";
                        //return objType.GetProperty(name).GetValue(obj, null).ToString();
                    }
                    else  // If obj Translated has filled field, return it
                    {
                        return objType.GetProperty(name).GetValue(objTranslated[0], null);
                    }
                }
                else
                {
                    if (objType.GetProperty(name).GetValue(objTranslated, null) == null) // If obj Translated doesn't have filled field, return empty string OR default translation
                    {
                        return "";
                        //return objType.GetProperty(name).GetValue(obj, null).ToString();
                    }
                    else  // If obj Translated has filled field, return it
                    {
                        return objType.GetProperty(name).GetValue(objTranslated, null);
                    }
                }


            }

            return objType.GetProperty(name).GetValue(obj, null); //If obj Translated doesn't exist, return the default translation
        }

        internal string GetResizedImagePath(ProjectKeysModel projectConfigKeys, string imgSrc, string imgsize, string imgDirectory = "", bool isProduct = false, bool isGiftCard = false)
        {
            if (isProduct)
            {
                if (isGiftCard)
                {
                    return projectConfigKeys.apiUrl + "images/" + imgsize + "/" + (imgSrc == "" || imgSrc == null ? projectConfigKeys.defaultGiftCardImage : imgSrc);
                }
                else
                {
                    return projectConfigKeys.apiUrl + "images/" + imgsize + "/" + (imgSrc == "" || imgSrc == null ? projectConfigKeys.defaultProductImage : imgSrc);
                }
            }
            else
            {
                if (imgsize == "" || imgsize == null)
                {
                    return imgSrc == "" || imgSrc == null ? null : projectConfigKeys.apiUrl + "content/uploads/" + imgDirectory + "/" + imgSrc;
                }
                else
                {
                    return imgSrc == "" || imgSrc == null ? null : projectConfigKeys.apiUrl + "images/" + imgsize + "/" + imgSrc;
                }
            }
        }

        internal string GetGridImage(string title, string imgSrc)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            return imgSrc == "" || imgSrc == null ? (title[0] + "" + title[1]) : projectConfigKeys.apiUrl + "images/120x120xi/" + imgSrc;
        }


        internal void ClearCache()
        {
            var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);
            foreach (var item in cache.AllKeys)
            {
                cache.Remove(item);
            }
        }

        internal IOrderedEnumerable<SharedControllerModel> GetControllers(bool? isKMSSection = false)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            var allControllers = asm.GetTypes().Where(d => d.Namespace == Assembly.GetExecutingAssembly().FullName.Split(',')[0] + ".Controllers" && d.Name.Contains("Controller") && !d.Name.Equals("SharedController`1"));

            var allControllersList = allControllers.ToList();

            List<SharedControllerModel> list = allControllers.Select(x => new SharedControllerModel
            {
                id = isKMSSection.HasValue && isKMSSection == true ? x.Name.Replace("Controller", "").ToLower() : x.Name.Replace("Controller", ""),
                label = x.Name.Replace("Controller", "")
            }).ToList();

            if (isKMSSection.HasValue && isKMSSection == true)
            {
                list.Add(new SharedControllerModel { id = "sectionsgroup", label = "**Create as Sections Group**" });

            }


            return list.OrderBy(x => x.label);


        }
    }
}