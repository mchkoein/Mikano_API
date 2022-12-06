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
using System.Web;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Models
{
    #region Models

    [MetadataType(typeof(AspNetUserValidation))]
    public partial class AspNetUser
    {
        dblinqDataContext db = new dblinqDataContext();
        public string accessToken { get; set; }

        public string OldEmail { get; set; }
        public string ConfirmNewEmail { get; set; }


        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        public string OldPassword { get; set; }

        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }

        public bool IsLocked { get; set; }
        public string RoleId { get; set; }

        public string googleProviderUserId { get; set; }

        public string googleAccessToken { get; set; }

        public bool subscribeToNewsLetter { get; set; }

        public string FullName
        {
            get
            {
                return this.firstName + " " + this.lastName;
            }
        }

        //public EKomBasket LastUserBasket()
        //{

        //    EKomBasketRepository basketRpstry = new EKomBasketRepository();
        //    int lastCombineBasket = 0;
        //    int lastbasketTake = 2;
        //    int basketType = 0;
        //    EKomBasket lastBasket = this.EKomBaskets.Where(d => d.ekomExecutionStateId < 4).OrderByDescending(d => d.dateCreated).Take(lastbasketTake).FirstOrDefault();
        //    if (lastBasket == null)
        //        return null;
        //    if (lastBasket.hasGift && lastBasket.EgiftCount ==  basketRpstry.GetItemCount(lastBasket))
        //    {
        //        foreach (var item in lastBasket.EKomProductBaskets)
        //        {
        //            if (item.eGiftType != 1)
        //                basketType = 1;
        //        }
        //        if (basketType == 0)
        //        {
        //            while (lastbasketTake > 0)
        //            {
        //                if (lastBasket.EKomCoupon.CouponGiftType == 1 && basketRpstry.GetItemCount(lastBasket) == 1)
        //                {
        //                    lastbasketTake++;
        //                    lastBasket = this.EKomBaskets.Where(d => d.ekomExecutionStateId < 4).OrderByDescending(d => d.dateCreated).Take(lastbasketTake).OrderBy(d => d.dateCreated).FirstOrDefault();
        //                }
        //                else
        //                {
        //                    lastbasketTake = 0;
        //                }

        //            }
        //        }
        //    }
        //    if (db.CombineBaskets.Where(d => d.basketId == lastBasket.id).Count() > 0)
        //        lastCombineBasket = db.CombineBaskets.Where(d => d.basketId == lastBasket.id).FirstOrDefault().mainBasketId;
        //    if (lastCombineBasket != 0)
        //    {
        //        return db.EKomBaskets.SingleOrDefault(d => d.id == lastCombineBasket);
        //    }
        //    else
        //    {
        //        return lastBasket;
        //    }
        //}





    }


    public class AspNetUserValidation
    {
        [Required]
        public string UserName { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }


        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }

        public string RoleId { get; set; }

        public string countryId { get; set; }

    }

    public class CheckoutAsGuestModel
    {
        [Required]
        public string UserName { get; set; }
        public bool subscribeToNewsLetter { get; set; }
    }
    #endregion

    #region Methods
    public class AdministratorRepository : SharedRepository
    {
        internal dblinqDataContext db = new dblinqDataContext();
        public IQueryable<AspNetUser> GetAll()
        {
            return db.AspNetUsers.Where(d => !d.isDeleted && !d.AspNetUserRoles.Any(q => q.AspNetRole.Name == ConfigurationManager.AppSettings["EndUserRole"]));
        }

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


        public AspNetUser GetByIdGlobal(string id, bool refresh = false)
        {
            var entry = db.AspNetUsers.FirstOrDefault(d => !d.isDeleted && d.Id == id);
            if (entry != null && refresh)
            {
                db.Refresh(RefreshMode.OverwriteCurrentValues, entry);
            }
            return entry;
        }


        public AspNetUser GetByUserName(string username)
        {
            return GetAll().FirstOrDefault(d => d.UserName == username);
        }


        public AspNetUser GetByUserNameGlobal(string username)
        {
            return db.AspNetUsers.FirstOrDefault(d => !d.isDeleted && d.UserName == username);
        }


        public void DeleteAllRelatedRoles(string id)
        {
            db.ExecuteCommand("delete AspNetUserRoles where userid = {0}", id);
        }

        public void Save()
        {
            db.SubmitChanges();
        }

    }
    #endregion
}