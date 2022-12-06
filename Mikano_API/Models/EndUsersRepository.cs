using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data.Entity;
using System.Data.Linq;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text.RegularExpressions;
using System.Web;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Models
{
    #region Models
    public class UserUpdateModel
    {
        public bool hasOldPassword { get; set; }

        [Required]
        public string firstName { get; set; }

        [Required]
        public string lastName { get; set; }

        [Required]
        public DateTime? dob { get; set; }

        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }

        public string gender { get; set; }

        public string CategoryToRelateTo { get; set; }

        [Required]
        public int? countryId { get; set; }

        [Required]
        public string phone { get; set; }

        public string mobile { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Email Address")]
        [System.Web.Mvc.Compare("UserName", ErrorMessage = "The Email Address and confirmation Email Address do not match.")]
        public string ConfirmUserName { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "NewPassword")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.Web.Mvc.Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }

        public bool SubToNewsletter { get; set; }

        public string provider { get; set; }

        #region blogger

        public bool? isBlogger { get; set; }
        public int? bloggerDiscountValue { get; set; }
        public double? bloggerCommissionValue { get; set; }
        public double? bloggerFundValue { get; set; }
        public DateTime? bloggerExpiryDate { get; set; }
        public string bloggerCouponCode { get; set; }

        #endregion
    }
    #endregion

    #region CRUD Methods
    public class EndUsersRepository : SharedRepository
    {
        public dblinqDataContext db = new dblinqDataContext();
        public IQueryable<AspNetUser> GetAll(string keywords = null, string order = null, DateTime? dateFrom = null, DateTime? dateTo = null, int? branch = null)
        {
            var recordsToReturn = db.AspNetUsers.Where(d => !d.isDeleted && d.AspNetUserRoles.Any(q => q.AspNetRole.Name == ConfigurationManager.AppSettings["EndUserRole"]));

            if (!string.IsNullOrEmpty(keywords))
            {
                keywords = keywords.Trim().ToLower();

                recordsToReturn = recordsToReturn.Where(e =>
                (e.firstName + e.lastName).ToLower().Contains(keywords)
                || e.UserName.ToLower().Contains(keywords)
                || e.Country.name.ToLower().Contains(keywords)
                || e.mobile.ToLower().Contains(keywords)
                || e.PhoneNumber.ToLower().Contains(keywords)
                || e.PhoneNumberExt.ToLower().Contains(keywords)
                );
            }


            if (dateFrom.HasValue)
            {
                recordsToReturn = recordsToReturn.Where(d => d.dateCreated >= dateFrom);
            }

            if (dateTo.HasValue)
            {
                recordsToReturn = recordsToReturn.Where(d => d.dateCreated <= dateTo);
            }

            return recordsToReturn.OrderByDescending(d => d.dateCreated);
        }

        #region Read

        public AspNetUser GetNextEntry(AspNetUser entry)
        {
            var allRoles = GetAll().ToList();
            var indexOfCurrentItem = allRoles.IndexOf(entry);

            AspNetUser nextEntry = allRoles.Count >= indexOfCurrentItem + 1 ? null : allRoles[indexOfCurrentItem + 1];

            if (nextEntry == null)
            {
                nextEntry = allRoles.Where(d => d.Id != entry.Id).FirstOrDefault();
            }
            if (nextEntry == null)
            {
                nextEntry = entry;
            }
            return nextEntry;
        }

        public AspNetUser GetById(string id, bool refresh = false)
        {
            var entry = GetAll().FirstOrDefault(d => d.Id == id);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }

        public AspNetUser GetUserByGoogleProviderUserId(string providerUserId)
        {
            return db.AspNetUsers.FirstOrDefault(d => !d.isDeleted && d.googleProviderUserId == providerUserId);
        }

        public AspNetUser GetByName(string fullName, bool refresh = false)
        {
            fullName = fullName.Trim().ToLower();
            var entry = GetAll().FirstOrDefault(d => (d.firstName + " " + d.lastName).ToLower() == fullName);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }

        public AspNetUser GetByIdForLogin(string id, bool refresh = false)
        {
            var entry = GetAll().FirstOrDefault(d => d.Id == id && d.aspNetUsersActivationId == (int)UserActivation.Active);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }

        public AspNetUser GetByUserName(string username, bool refresh = false)
        {
            var entry = GetAll().FirstOrDefault(d => d.UserName == username);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }

        public AspNetUser GetByUserNameForLogin(string username)
        {
            return GetAll().FirstOrDefault(d => d.UserName == username
            && d.aspNetUsersActivationId == (int)UserActivation.Active);
        }

        public AspNetUser GetByUserNameForFront(string username)
        {
            return db.AspNetUsers.FirstOrDefault(d => !d.isDeleted && d.UserName.ToLower() == username.ToLower());
        }

        public AspNetUser GetByMobileForFront(string mobile, bool refresh = false)
        {
            mobile = Regex.Replace(mobile, @"\s+", "");
            var last7Nb = mobile.Substring((mobile.Length > 7 ? (mobile.Length - 7) : 0), (mobile.Length > 6 ? 7 : mobile.Length));
            var entry = GetAll().FirstOrDefault(d => !d.isDeleted && d.mobile.Substring((d.mobile.Length > 7 ? (d.mobile.Length - 7) : 0), (d.mobile.Length > 6 ? 7 : d.mobile.Length)) == last7Nb);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }

        public AspNetUser GetByMobile(string mobile, bool refresh = false)
        {
            mobile = Regex.Replace(mobile, @"\s+", "");
            var last7Nb = mobile.Substring((mobile.Length > 7 ? (mobile.Length - 7) : 0), (mobile.Length > 6 ? 7 : mobile.Length));
            var entry = GetAll().FirstOrDefault(d => !d.isDeleted && d.mobile.Substring((d.mobile.Length > 7 ? (d.mobile.Length - 7) : 0), (d.mobile.Length > 6 ? 7 : d.mobile.Length)) == last7Nb);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }

        public AspNetUser GetByMobileForLogin(string mobile, bool refresh = false)
        {
            mobile = Regex.Replace(mobile, @"\s+", "");
            var last7Nb = mobile.Substring((mobile.Length > 7 ? (mobile.Length - 7) : 0), (mobile.Length > 6 ? 7 : mobile.Length));
            var entry = GetAll().FirstOrDefault(d => !d.isDeleted && d.aspNetUsersActivationId == (int)UserActivation.Active
            && d.mobile.Substring((d.mobile.Length > 7 ? (d.mobile.Length - 7) : 0), (d.mobile.Length > 6 ? 7 : d.mobile.Length)) == last7Nb);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }

        public AspNetUser GetUserByProviderUserId(string providerUserId)
        {
            return db.AspNetUsers.FirstOrDefault(d => !d.isDeleted && d.providerUserId == providerUserId);
        }

        public KMSLog GetLastAction(string id, bool onlyView = false, string action = null)
        {
            if (!string.IsNullOrEmpty(id))
            {

                string assetId = id.ToString();
                string assetType = "resellers";
                var results = db.KMSLogs.Where(d => d.assetId.Equals(assetId) && d.assetType.Equals(assetType) && !d.hasError).AsQueryable();
                if (!string.IsNullOrEmpty(action))
                {
                    results = results.Where(d => d.action == action);
                }
                if (onlyView)
                {
                    results = results.Where(d => d.subAction == null);
                }
                return results.OrderByDescending(d => d.id).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public dynamic GetLastActionParams(string id)
        {
            KMSLog lastAction = this.GetLastAction(id);
            if (lastAction != null)
            {
                return new
                {
                    lastActionName = string.IsNullOrEmpty(lastAction.subAction) ? "Viewed" : lastAction.subAction,
                    lastActionUserId = lastAction.aspNetUsersId,
                    lastActionUserFullName = lastAction.AspNetUser.firstName + " " + lastAction.AspNetUser.lastName,
                    lastActionDate = lastAction.date
                };
            }
            else
            {
                return new { };
            }
        }






        #endregion

        #region Update

        public void Save()
        {
            db.SubmitChanges();
        }

        #endregion

        #region Delete

        public void Delete(string id)
        {
            db.AspNetUsers.DeleteOnSubmit(GetAll().FirstOrDefault(d => d.Id == id));
        }

        public void DeleteAllRelatedRoles(string id)
        {
            db.ExecuteCommand("delete AspNetUserRoles where userid = {0}", id);
        }



        public void DeleteAllRelatedMobiles(string id)
        {
            db.ExecuteCommand("delete AspNetUsersMobileNumber where aspNetUsersId = {0}", id);
        }



        #endregion

    }
    #endregion
}