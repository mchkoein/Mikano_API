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
using Newtonsoft.Json;
using Mikano_API.Helpers;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/administrators")]
    public class AdministratorsController : SharedController<SocketHub>
    {
        private AdministratorRepository rpstry = new AdministratorRepository();

        private string directory = "account";
        private string kSectionName = "administrators";
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

        [HttpPost]
        [Route("CheckToken")]
        public IHttpActionResult CheckToken()
        {
            return Ok();
        }

        [HttpPost]
        [Route("UpdatelastLogin")]
        public IHttpActionResult UpdatelastLogin()
        {
            var entry = rpstry.GetByUserNameGlobal(User.Identity.Name);
            entry.lastLoginDate = DateTime.Now;
            entry.totalVisits = !entry.totalVisits.HasValue ? 1 : entry.totalVisits + 1;
            rpstry.Save();
            return Ok();
        }



        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request)
        {
            //logRpstry.AddLog(new Guid(User.Identity.GetUserId()), "Get KMS Section", "GetAll", "read");
            RolesRepository rolesRpstry = new RolesRepository();
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
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, null, kActionName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    var results = rpstry.GetAll();

                    if (!rolesRpstry.IsUserInRole(User.Identity.GetUserId(), ConfigurationManager.AppSettings["DevteamRole"]))
                    {
                        results = results.Where(d => d.AspNetUserRoles.Any(e => e.AspNetRole.Name != ConfigurationManager.AppSettings["DevteamRole"]));
                        if (!rolesRpstry.IsUserInRole(User.Identity.GetUserId(), ConfigurationManager.AppSettings["ManagementRole"]))
                        {
                            results = results.Where(d => d.AspNetUserRoles.Any(e => e.AspNetRole.Name != ConfigurationManager.AppSettings["ManagementRole"]));
                        }
                    }

                    return Request.CreateResponse(HttpStatusCode.OK,
                results.Select(d => new
                {
                    id = d.Id,
                    imgSrc = d.imgSrc == "" || d.imgSrc == null ? (d.firstName == null && d.lastName == null ? d.UserName[0] + "" + d.UserName[1] : d.firstName[0] + "" + d.lastName[0]) : projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + d.imgSrc,
                    fullName = d.firstName + " " + d.lastName,
                    UserName = d.UserName,
                    countryId = d.Country == null ? "" : d.Country.name,
                    RoleId = d.AspNetUserRoles.Any() ? d.AspNetUserRoles.FirstOrDefault().AspNetRole.Name : "",
                    dateCreated = d.dateCreated,
                    lastLoginDate = d.lastLoginDate,
                    lastAction = d.KMSLogs.OrderByDescending(e => e.date).FirstOrDefault() == null ? DateTime.Now : d.KMSLogs.OrderByDescending(e => e.date).FirstOrDefault().date,
                    status = (d.LockoutEndDateUtc.HasValue && d.LockoutEndDateUtc.Value > DateTime.UtcNow) ? "Inactive" : "Active",
                }).ToDataSourceResult(request)
            );
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetById(string id)
        {
            RolesRepository rolesRpstry = new RolesRepository();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate && id == "" || hasPermissionsToUpdate && id != "")
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(id);
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.UserName, CollectRequestData(Request, null), GetIpAddress(Request), false);


                    var roles = rolesRpstry.GetAll();
                    roles = roles.Where(d => d.Name != ConfigurationManager.AppSettings["EndUserRole"]);

                    if (!rolesRpstry.IsUserInRole(User.Identity.GetUserId(), ConfigurationManager.AppSettings["DevteamRole"]))
                    {
                        roles = roles.Where(d => d.Name != ConfigurationManager.AppSettings["DevteamRole"]);
                        if (!rolesRpstry.IsUserInRole(User.Identity.GetUserId(), ConfigurationManager.AppSettings["ManagementRole"]))
                        {
                            roles = roles.Where(d => d.Name != ConfigurationManager.AppSettings["ManagementRole"]);
                        }
                    }

                    var lockOptions = new[] { new { id = "True", label = "Inactive" }, new { id = "False", label = "Active" } };

                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                IsLocked = "False"
                            },
                            additionalData = new
                            {
                                //countries = countryRpstry.GetAll().Select(s => new { id = s.id, label = s.title }),
                                lockOptions = lockOptions,
                                roles = roles.Select(s => new { id = s.Name, label = s.Name })
                            }
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = entry == null ? null : new
                            {
                                id = entry.Id,
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, directory),
                                firstName = entry.firstName,
                                lastName = entry.lastName,
                                UserName = entry.UserName,
                                //gender = entry.gender + "",
                                //countryId = entry.countryId + "",
                                //cityId = entry.cityId,
                                mobile = entry.mobile,
                                // dob = entry.dob,
                                phone = entry.phone,
                                PhoneNumberExt = entry.PhoneNumberExt,
                                RoleId = entry.AspNetUserRoles.Any() ? entry.AspNetUserRoles.FirstOrDefault().AspNetRole.Name : null,
                                IsLocked = (entry.LockoutEndDateUtc.HasValue && entry.LockoutEndDateUtc.Value > DateTime.UtcNow) + "",
                            },
                            additionalData = new
                            {
                                //countries = countryRpstry.GetAll().Select(s => new { id = s.id, label = s.title }),
                                lockOptions = lockOptions,
                                roles = roles.Select(s => new { id = s.Name, label = s.Name }),
                                lastAction = entry.KMSLogs.OrderByDescending(e => e.date).FirstOrDefault() == null ? DateTime.Now : entry.KMSLogs.OrderByDescending(e => e.date).FirstOrDefault().date,
                                lastLoginDate = entry.lastLoginDate,
                                dateCreated = entry.dateCreated,
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, id + "", kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetMenu()
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            var userEntry = rpstry.GetByUserName(User.Identity.Name);
            var role = userEntry.AspNetUserRoles.Any() ? userEntry.AspNetUserRoles.FirstOrDefault().AspNetRole : null;
            bool hasPermissionsToRefillTransaction = sectionRpstry.GetPermission("refilltransactions", loggedInUserId, (int)KActions.create);

            if (role == null)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    leftMenu = JsonConvert.DeserializeObject(role.menu),
                    leftMenuLevels = JsonConvert.DeserializeObject(role.menuLevel),
                    notifications = new { },
                    messages = new { },
                    user = new
                    {
                        fullName = userEntry.firstName + " " + userEntry.lastName,
                        id = userEntry.Id,
                        userName = userEntry.UserName,
                        role = role.Name,
                        picture = string.IsNullOrEmpty(userEntry.imgSrc) ? null : projectConfigKeys.apiUrl + "/content/uploads/" + directory + "/" + userEntry.imgSrc,
                        nameAbr = (string.IsNullOrEmpty(userEntry.firstName) && string.IsNullOrEmpty(userEntry.lastName) ? userEntry.UserName[0] + "" + userEntry.UserName[1] : userEntry.firstName[0] + "" + userEntry.lastName[0])
                    },
                    //totalOrders = basketRpstry.GetAllOrders(null, true).Count()
                });
            }
        }

        [HttpGet]
        public HttpResponseMessage GetCustomization()
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

            loggedInUserId = User.Identity.GetUserId();

            var userEntry = rpstry.GetByUserName(User.Identity.Name);
            var role = userEntry.AspNetUserRoles.Any() ? userEntry.AspNetUserRoles.FirstOrDefault().AspNetRole : null;

            if (role == null)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    #region Primary Color
                    colorPrimary = projectConfigKeys.colorPrimary,
                    colorPrimaryTextOver = projectConfigKeys.colorPrimaryTextOver,
                    colorPrimaryHover = projectConfigKeys.colorPrimaryHover,
                    colorPrimaryHoverTextOver = projectConfigKeys.colorPrimaryHoverTextOver,
                    #endregion

                    #region Secondary Color
                    colorSecondary = projectConfigKeys.colorSecondary,
                    colorSecondaryTextOver = projectConfigKeys.colorSecondaryTextOver,
                    colorSecondaryHover = projectConfigKeys.colorSecondaryHover,
                    colorSecondaryHoverTextOver = projectConfigKeys.colorSecondaryHoverTextOver,
                    #endregion

                    #region Tertiary Color
                    colorTertiary = projectConfigKeys.colorTertiary,
                    colorTertiaryTextOver = projectConfigKeys.colorTertiaryTextOver,
                    colorTertiaryHover = projectConfigKeys.colorTertiaryHover,
                    colorTertiaryHoverTextOver = projectConfigKeys.colorTertiaryHoverTextOver,
                    #endregion

                    #region KMS Logo
                    kmsLogo = !string.IsNullOrEmpty(projectConfigKeys.kmsLogo) && !projectConfigKeys.kmsLogo.Contains("empty") ? projectConfigKeys.kmsLogo : null,
                    #endregion

                    boardBackgroundColor = projectConfigKeys.boardBackgroundColor,
                });
            }
        }

        [HttpPost]
        public HttpResponseMessage Details(AspNetUser entry, SubmissionOptions submissionType)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                if (string.IsNullOrEmpty(entry.NewPassword))
                {
                    ModelState.Remove("entry.NewPassword");
                    ModelState.Remove("entry.ConfirmNewPassword");
                }

                if (rpstry.GetById(entry.Id) != null)
                {
                    ModelState.Remove("entry.Password");
                    ModelState.Remove("entry.ConfirmPassword");
                }

                ModelState.Remove("entry.aspNetUsersActivationId");
                ModelState.Remove("entry.aspNetUsersStatusId");
                ModelState.Remove("entry.aspNetUsersTypeId");

                if (ModelState.IsValid)
                {
                    try
                    {

                        entry.Email = entry.UserName;
                        //create
                        if (rpstry.GetById(entry.Id) == null && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();

                            var user = new ApplicationUser() { UserName = entry.UserName, Email = entry.UserName };
                            IdentityResult resulot = UserManager.Create(user, entry.Password);
                            if (!resulot.Succeeded)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", resulot.Errors.Where(d => !d.Contains("Name " + entry.UserName))) });
                            }

                            var oldEntry = rpstry.GetByUserName(entry.UserName);



                            oldEntry.firstName = entry.firstName;
                            oldEntry.lastName = entry.lastName;
                            oldEntry.gender = entry.gender;
                            oldEntry.dob = entry.dob;
                            oldEntry.countryId = entry.countryId;
                            oldEntry.mobile = entry.mobile;
                            oldEntry.phone = entry.phone;
                            oldEntry.PhoneNumber = entry.PhoneNumber;
                            oldEntry.PhoneNumberExt = entry.PhoneNumberExt;
                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.aspNetUsersActivationId = (int)UserActivation.Active;
                            oldEntry.dateCreated = DateTime.Now;
                            oldEntry.dateModified = DateTime.Now;
                            if (entry.IsLocked)
                            {
                                oldEntry.LockoutEndDateUtc = DateTime.UtcNow.AddYears(5);
                            }
                            rpstry.Save();
                            UserManager.AddToRole(oldEntry.Id, entry.RoleId);
                            UserManager.SetLockoutEnabled(oldEntry.Id, Convert.ToBoolean(ConfigurationManager.AppSettings["UserLockoutEnabledByDefault"]));

                        }
                        //edit
                        else if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            rpstry.DeleteAllRelatedRoles(entry.Id);
                            UserManager.AddToRole(entry.Id, entry.RoleId);

                            var oldEntry = rpstry.GetById(entry.Id);

                            if (!string.IsNullOrEmpty(entry.NewPassword))
                            {
                                UserManager.RemovePassword(oldEntry.Id);
                                IdentityResult resulot = UserManager.AddPassword(oldEntry.Id, entry.NewPassword);
                                if (!resulot.Succeeded)
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", resulot.Errors) });
                                }
                            }



                            //the object was changed from outside the current dbcontext, so we need to get it again
                            oldEntry = rpstry.GetById(entry.Id, true);

                            if (rpstry.GetByUserName(entry.UserName) != null && oldEntry.UserName != entry.UserName)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "Email " + entry.UserName + " already taken." });
                            }

                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, entry.Id);
                            }



                            oldEntry.firstName = entry.firstName;
                            oldEntry.lastName = entry.lastName;
                            oldEntry.UserName = entry.UserName;
                            oldEntry.gender = entry.gender;
                            oldEntry.dob = entry.dob;
                            oldEntry.countryId = entry.countryId;
                            oldEntry.mobile = entry.mobile;
                            oldEntry.phone = entry.phone;
                            oldEntry.PhoneNumber = entry.PhoneNumber;
                            oldEntry.PhoneNumberExt = entry.PhoneNumberExt;
                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.dateModified = DateTime.Now;
                            if (entry.IsLocked)
                            {
                                oldEntry.LockoutEndDateUtc = DateTime.UtcNow.AddYears(5);
                            }
                            else
                            {
                                oldEntry.LockoutEndDateUtc = null;
                            }
                            rpstry.Save();

                        }

                        if (submissionType == SubmissionOptions.saveAndGoToNext)
                        {
                            entry = rpstry.GetNextEntry(entry);
                        }

                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, entry.Id, entry.UserName, CollectRequestData(Request, entry), GetIpAddress(Request), false);

                        return Request.CreateResponse(HttpStatusCode.OK, new { id = entry.Id });
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, entry.Id, entry.UserName, CollectRequestData(Request, entry), GetIpAddress(Request), true);
                        return Request.CreateResponse(HttpStatusCode.BadRequest, e);
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
                        //rpstry.Delete(parId);
                        var entry = rpstry.GetById(parId);
                        entry.UserName = entry.UserName + "~" + entry.Id + "deleted";
                        entry.Email = entry.Email + "~" + entry.Id + "deleted";
                        entry.isDeleted = true;
                        rpstry.Save();
                        logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, parId, entry.UserName, CollectRequestData(Request, null), GetIpAddress(Request), false);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "");
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(User.Identity.GetUserId(), User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }
    }
}


