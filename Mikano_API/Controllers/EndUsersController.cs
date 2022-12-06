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
using static Mikano_API.Models.KMSEnums;
using System.Web;
using Microsoft.AspNet.Identity.Owin;
using System.Configuration;
using Mikano_API.Helpers;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Text;
using System.Net.Security;
using Newtonsoft.Json.Linq;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/endusers")]
    public class EndUsersController : SharedController<SocketHub>
    {
        private EndUsersRepository rpstry = new EndUsersRepository();
        private CountryRepository countryRpstry = new CountryRepository();

        private MembershipHelper membershipHelper = new MembershipHelper();

        private string kSectionName = "endusers";
        private string kActionName = "";

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

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
                    var results = rpstry.GetAll().Select(d => new
                    {
                        id = d.Id,
                        fullName = d.firstName + (d.lastName == null ? "" : " " + d.lastName),
                        UserName = d.UserName,
                        gender = d.gender,
                        mobile = d.mobile,
                        phone = d.phone,
                        country = d.countryId.HasValue ? d.Country.name : "",
                        activation = d.AspNetUsersActivation == null ? "" : d.AspNetUsersActivation.title,
                        region = d.Region == null ? "" : d.Region.title,
                        city = d.Region1 == null ? "" : d.Region1.title,
                        ageGroup = d.AgeGroup == null ? "" : d.AgeGroup.title,

                        #region App
                        ipAddress = d.ipAddress,
                        appInstallDate = d.appInstallDate,
                        //deviceOsPlatform = d.deviceOsPlatform,
                        appVersion = d.appVersion,
                        appNbOfCrashes = d.appNbOfCrashes,
                        #endregion

                        #region Login & Registration
                        lastLoginDate = d.lastLoginDate,
                        registrationDate = d.dateCreated,
                        #endregion

                        utmSource = d.utm_source,

                        #region Orders
                        //lastOrderId = d.EKomBaskets.Any(e => e.workflowStep == (int)EKomWorkflowStep.order ) ? d.EKomBaskets.OrderByDescending(z => z.orderDate).FirstOrDefault(e => e.workflowStep == (int)EKomWorkflowStep.order ).id : -1,
                        //lastOrderDate = d.EKomBaskets.Any(e => e.workflowStep == (int)EKomWorkflowStep.order ) ? d.EKomBaskets.OrderByDescending(z => z.orderDate).FirstOrDefault(e => e.workflowStep == (int)EKomWorkflowStep.order ).orderDate : null,

                        //firstOrderId = d.EKomBaskets.Any(e => e.workflowStep == (int)EKomWorkflowStep.order ) ? d.EKomBaskets.OrderBy(z => z.orderDate).FirstOrDefault(e => e.workflowStep == (int)EKomWorkflowStep.order ).id : -1,
                        //firstOrderDate = d.EKomBaskets.Any(e => e.workflowStep == (int)EKomWorkflowStep.order ) ? d.EKomBaskets.OrderBy(z => z.orderDate).FirstOrDefault(e => e.workflowStep == (int)EKomWorkflowStep.order ).orderDate : null,

                        //totalOrders = d.EKomBaskets.Where(e => e.workflowStep == (int)EKomWorkflowStep.order ).Count(),
                        //totalSale = d.EKomBaskets.Any(e => e.workflowStep == (int)EKomWorkflowStep.order ) ? d.EKomBaskets.Where(e => e.workflowStep == (int)EKomWorkflowStep.order ).Sum(w => w.pricePaid) : 0,
                        //averageOrderPrice = d.EKomBaskets.Any(e => e.workflowStep == (int)EKomWorkflowStep.order ) ? d.EKomBaskets.Where(e => e.workflowStep == (int)EKomWorkflowStep.order ).Sum(w => w.pricePaid) / d.EKomBaskets.Where(e => e.workflowStep == (int)EKomWorkflowStep.order ).Count() : 0,
                        #endregion

                        totalVisits = d.totalVisits,

                        syncedWithTP = d.syncedWithTP,
                        IsLocked = (d.LockoutEndDateUtc.HasValue && d.LockoutEndDateUtc.Value > DateTime.UtcNow),

                    }).OrderByDescending(d => d.registrationDate).ToDataSourceResult(request);

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        Data = results.Data,
                        Errors = results.Errors,
                        AggregateResults = results.AggregateResults,
                        Total = results.Total
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
        public HttpResponseMessage GetForMultiselect([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
        {
            return Request.CreateResponse(HttpStatusCode.OK,
            rpstry.GetAll().Select(d => new
            {
                id = d.Id,
                title = d.firstName + " " + d.lastName
            }).ToDataSourceResult(request)
        );
        }

        [HttpGet]
        public HttpResponseMessage GetById(string id)
        {
            ContactTitleRepository contactTitlesRpstry = new ContactTitleRepository();
            RegionRepository regionsRpstry = new RegionRepository();
            AspNetUsersActivationRepository activationsRpstry = new AspNetUsersActivationRepository();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == "-1" || hasPermissionsToUpdate && id != "-1")
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(id);
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.UserName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    #region Activations
                    var activations = activationsRpstry.GetAllIsPublished().Select(d => new
                    {
                        id = d.id + "",
                        hasReason = d.hasReason,
                        label = d.title
                    });
                    #endregion

                    #region Regions
                    var regions = regionsRpstry.GetRegions().Select(d => new
                    {
                        id = d.id + "",
                        label = d.title
                    });
                    #endregion

                    #region Contact Titles
                    var contactTitles = contactTitlesRpstry.GetAllIsPublished().Select(d => new
                    {
                        id = d.id + "",
                        label = d.title
                    });
                    #endregion

                    #region Countries
                    var countries = new CountryRepository().GetAll().Select(d => new
                    {
                        id = d.id + "",
                        label = d.CountryName
                    });
                    #endregion

                    #region Age Group
                    var ageGroups = new AgeGroupRepository().GetAllIsPublished().Select(d => new
                    {
                        id = d.id + "",
                        label = d.title
                    });
                    #endregion

                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                            },
                            additionalData = new
                            {
                                contactTitles = contactTitles,
                                activations = activations,
                                regions = regions,
                                countries = countries,
                                ageGroups = ageGroups,
                                genderOptions = genderOptions
                            }
                        });
                    }
                    else
                    {
                        var lastAction = rpstry.GetLastAction(entry.Id, false, "update");
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                #region End User Info
                                id = entry.Id,
                                contactTitleId = entry.contactTitleId + "",
                                fullName = entry.FullName,
                                firstName = entry.firstName,
                                lastName = entry.lastName,
                                gender = entry.gender + "",
                                address = entry.address,
                                mobile = entry.mobile,
                                phone = entry.phone,
                                countryId = entry.countryId + "",
                                ageGroupId = entry.ageGroupId + "",
                                UserName = entry.UserName,
                                googleMapsCoordsLatitude = entry.googleMapsCoordsLatitude,
                                googleMapsCoordsLongitude = entry.googleMapsCoordsLongitude,
                                googleMapZoom = entry.googleMapZoom,
                                #endregion

                                #region Activation
                                aspNetUsersActivationId = entry.aspNetUsersActivationId + "",
                                //activationReason = entry.activationReason,
                                #endregion

                                #region Region & SubRegion
                                //walletBalance = entry.walletBalance,
                                regionId = entry.regionId + "",
                                subRegionId = entry.subRegionId + "",
                                //lowCreditAlertThreshold = entry.lowCreditAlertThreshold,
                                //canRedeemLoyaltyPoints = entry.canRedeemLoyaltyPoints,
                                //IsLocked = (entry.LockoutEndDateUtc.HasValue && entry.LockoutEndDateUtc.Value > DateTime.UtcNow) + "",
                                #endregion

                                #region Account Credits & Loyalty Points
                                accountCredit = entry.accountCredit == null ? 0 : entry.accountCredit,
                                loyaltyPoints = entry.loyaltyPoints,
                                #endregion

                                dateCreated = entry.dateCreated,
                            },
                            fieldsStatus = new
                            {
                                ConfirmNewPassword = DirectiveStatus.hidden,
                                NewPassword = DirectiveStatus.hidden,
                                ConfirmPassword = DirectiveStatus.hidden,
                                Password = DirectiveStatus.hidden,
                                accountCredit = DirectiveStatus.disabled,
                                loyaltyPoints = DirectiveStatus.disabled,
                            },
                            additionalData = new
                            {
                                contactTitles = contactTitles,
                                activations = activations,
                                regions = regions,
                                countries = countries,
                                ageGroups = ageGroups,
                                genderOptions = genderOptions,
                                logs = new[] {
                                    new {label = "Last Updated By",value = lastAction == null ? "-" : lastAction.AspNetUser.FullName},
                                    //new {label = "Approved By",value =(entry.AspNetUser2.firstName + " " + entry.AspNetUser2.lastName ?? "")+""},
                                    new {label = "Registered On",value = entry.dateCreated.HasValue ? entry.dateCreated.Value.ToString("dd MMM yyyy HH:mm tt") : ""},
                                    new {label = "Last Login On",value =entry.lastLoginDate.HasValue ? entry.lastLoginDate.Value.ToString("dd MMM yyyy HH:mm tt") : ""},
                                    new {label = "Total Visits",value =(entry.totalVisits ?? 0)+""},
                                    //new {label = "Top Payment Method",value =  (entry.RefillTransactionRequests.Any(s => s.transactionRequestStatusId == (int)RequestStatus.Completed && s.RefillTransactions.Any(e => !e.isDeleted)) ?
                                    //entry.RefillTransactionRequests.Where(s => s.transactionRequestStatusId == (int)RequestStatus.Completed && s.RefillTransactions.Any(e => !e.isDeleted)).GroupBy(e => e.EkomPaymentMethod).OrderByDescending(w => w.Count()).FirstOrDefault().Key.title :
                                    //"")},
                                    //new {label = "Install Date",value =entry.appInstallDate.HasValue ? entry.appInstallDate.Value.ToString("dd MMM yyyy HH:mm tt") : ""},
                                    #region Device, OS, App Info
                                    //new {label = "Device Manufacturer",value =entry.deviceManufacturer},
                                    //new {label = "Device Model",value =entry.deviceModel},
                                    //new {label = "OS Platform",value =entry.deviceOsPlatform},
                                    //new {label = "OS Version",value =entry.deviceOsVersion},

									new {label = "Browser",value = entry.browserName},
                                    new {label = "Browser v.",value = entry.browserVersion},
                                    new {label = "OS",value = entry.osName},
                                    new {label = "OS v.",value = entry.osVersion},

									//new {label = "App Version",value =entry.appVersion},
                                    //new {label = "Nb Of Crashes",value =(entry.appNbOfCrashes ?? 0)+""},
                                    
                                    #endregion
                                    //new {label = "Loyalty Points",value =entry.loyaltyPoints+""}
                                }
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
        public HttpResponseMessage Details(AspNetUser entry, SubmissionOptions submissionType, string source = null)
        {
            SettingsRepository settingsRpstry = new SettingsRepository();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                ModelState.Remove("entry.RoleId");
                ModelState.Remove("entry.aspNetUsersCategoryId");
                ModelState.Remove("entry.aspNetUsersStatusId");
                ModelState.Remove("entry.aspNetUsersTypeId");

                if (ModelState.IsValid)
                {
                    try
                    {
                        entry.Email = entry.UserName;
                        var settingsEntry = settingsRpstry.GetFirstOrDefault();

                        #region Create
                        if (rpstry.GetById(entry.Id) == null && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            var user = new ApplicationUser() { UserName = entry.UserName, Email = entry.UserName };
                            IdentityResult resulot = UserManager.Create(user, entry.Password);
                            if (!resulot.Succeeded)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", resulot.Errors.Where(d => !d.Contains("Name " + entry.UserName))) });
                            }

                            var oldEntry = rpstry.GetByUserNameForFront(entry.UserName);


                            #region End User Info
                            oldEntry.contactTitleId = entry.contactTitleId;
                            oldEntry.firstName = entry.firstName;
                            oldEntry.lastName = entry.lastName;
                            oldEntry.gender = entry.gender;
                            oldEntry.mobile = entry.mobile;
                            oldEntry.phone = entry.phone;
                            oldEntry.countryId = entry.countryId;
                            oldEntry.regionId = entry.regionId;
                            oldEntry.subRegionId = entry.subRegionId;
                            oldEntry.address = entry.address;
                            oldEntry.googleMapsCoordsLongitude = entry.googleMapsCoordsLongitude;
                            oldEntry.googleMapsCoordsLatitude = entry.googleMapsCoordsLatitude;
                            oldEntry.googleMapZoom = entry.googleMapZoom;
                            #endregion

                            #region Activation
                            oldEntry.aspNetUsersActivationId = entry.aspNetUsersActivationId;
                            #endregion

                            oldEntry.dateCreated = DateTime.Now;
                            oldEntry.dateModified = DateTime.Now;

                            //if (entry.IsLocked)
                            //{
                            //    oldEntry.LockoutEndDateUtc = DateTime.UtcNow.AddYears(5);
                            //}

                            rpstry.Save();
                            UserManager.AddToRole(oldEntry.Id, ConfigurationManager.AppSettings["EndUserRole"]);
                            UserManager.SetLockoutEnabled(oldEntry.Id, Convert.ToBoolean(ConfigurationManager.AppSettings["UserLockoutEnabledByDefault"]));
                            entry = oldEntry;


                        }
                        #endregion

                        #region Edit
                        else if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            var oldEntry = rpstry.GetById(entry.Id);

                            //if (!string.IsNullOrEmpty(entry.NewPassword))
                            //{
                            //    UserManager.RemovePassword(oldEntry.Id);
                            //    IdentityResult resulot = UserManager.AddPassword(oldEntry.Id, entry.NewPassword);
                            //    if (!resulot.Succeeded)
                            //    {
                            //        return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", resulot.Errors) });
                            //    }
                            //}

                            if (rpstry.GetByUserName(entry.UserName) != null && oldEntry.UserName != entry.UserName)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "Email " + entry.UserName + " already registered." });
                            }

                            if (rpstry.GetByMobile(entry.mobile) != null && oldEntry.mobile != entry.mobile)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "Mobile " + entry.mobile + " already registered." });
                            }

                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, entry.Id);
                            }

                            //the object was changed from outside the current dbcontext, so we need to get it again
                            oldEntry = rpstry.GetById(entry.Id, true);

                            #region Activation
                            oldEntry.aspNetUsersActivationId = entry.aspNetUsersActivationId;
                            #endregion

                            #region End User Info
                            oldEntry.UserName = entry.UserName;
                            oldEntry.contactTitleId = entry.contactTitleId;
                            oldEntry.firstName = entry.firstName;
                            oldEntry.lastName = entry.lastName;
                            oldEntry.gender = entry.gender;
                            oldEntry.mobile = entry.mobile;
                            oldEntry.phone = entry.phone;
                            oldEntry.countryId = entry.countryId;
                            oldEntry.regionId = entry.regionId;
                            oldEntry.subRegionId = entry.subRegionId;
                            oldEntry.address = entry.address;
                            oldEntry.googleMapsCoordsLongitude = entry.googleMapsCoordsLongitude;
                            oldEntry.googleMapsCoordsLatitude = entry.googleMapsCoordsLatitude;
                            oldEntry.googleMapZoom = entry.googleMapZoom;
                            #endregion

                            oldEntry.dateModified = DateTime.Now;

                            //if (entry.IsLocked)
                            //{
                            //    oldEntry.LockoutEndDateUtc = DateTime.UtcNow.AddYears(5);
                            //}
                            //else
                            //{
                            //    oldEntry.LockoutEndDateUtc = null;
                            //}


                            rpstry.Save();
                            //rpstry.DeleteAllRelatedRoles(oldEntry.Id);
                            //UserManager.AddToRole(oldEntry.Id, entry.RoleId);+

                            //#region Send Email
                            //try
                            //{
                            //    BackgroundTaskManager.Run(async () =>
                            //    {
                            //        using (var client = new WebClient())
                            //        {
                            //            client.Encoding = System.Text.Encoding.UTF8;
                            //            var emailTemplate = client.DownloadString(apiUrl + "/emails/EmailAccountActivated?id=" + oldEntry.Id);

                            //            string from = ConfigurationManager.AppSettings["EU_ProjectName"] + "<" +  settingsEntry.defaultSendFromEmail  + ">";
                            //            string to = oldEntry.UserName;
                            //            string subject = ConfigurationManager.AppSettings["EU_ProjectName"] + ConfigurationManager.AppSettings["AccountActivationSubject"];

                            //            utilsHelper.sendEmail(from, to, "", "", subject, emailTemplate, "");
                            //            client.Dispose();
                            //        }
                            //    });
                            //}
                            //catch (Exception ee) { }
                            //#endregion
                        }
                        #endregion

                        if (submissionType == SubmissionOptions.saveAndGoToNext)
                        {
                            entry = rpstry.GetNextEntry(entry);
                        }
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.Id, entry.UserName, CollectRequestData(Request, entry), GetIpAddress(Request), false);

                        return Request.CreateResponse(HttpStatusCode.OK, new { id = entry.Id });
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.Id, entry.UserName, CollectRequestData(Request, entry), GetIpAddress(Request), true);
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
                        var entry = rpstry.GetById(parId);
                        entry.isDeleted = true;
                        entry.UserName = entry.UserName + "~" + entry.Id + "deleted";
                        entry.Email = entry.Email + "~" + entry.Id + "deleted";
                        entry.providerUserId = entry.providerUserId + "~" + entry.Id + "deleted";
                        entry.mobile = entry.Id + "~" + entry.mobile + "~" + entry.Id + "deleted";
                        entry.phone = entry.Id + "~" + entry.phone + "~" + entry.Id + "deleted";

                        //rpstry.Delete(parId);
                        rpstry.Save();
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.UserName, CollectRequestData(Request, null), GetIpAddress(Request), false);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "");
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
        #endregion

        #region front end


        #endregion
    }
}