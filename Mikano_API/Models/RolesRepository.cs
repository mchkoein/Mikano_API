using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web;

namespace Mikano_API.Models
{
    #region Models
    [MetadataType(typeof(AspNetRoleValidation))]
    public partial class AspNetRole
    {
    }

    public class AspNetRoleValidation
    {
        [Required]
        public string Name { get; set; }
    }
    #endregion

    public class KPermission
    {
        public KMSSection sectionEntry { get; set; }
        public bool CanRead { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPublish { get; set; }
    }

    #region Methods
    public class RolesRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<AspNetRole> GetAll()
        {
            return db.AspNetRoles;
        }
        public bool IsUserInRole(string userId, string roleName)
        {
            return db.AspNetUserRoles.Any(d => d.UserId == userId && d.AspNetRole.Name == roleName);
        }

        public AspNetRole GetNextEntry(AspNetRole entry)
        {
            var allRoles = GetAll().ToList();
            var indexOfCurrentItem = allRoles.IndexOf(entry);

            AspNetRole nextEntry = allRoles.Count >= indexOfCurrentItem + 1 ? null : allRoles[indexOfCurrentItem + 1];

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

        public AspNetRole GetById(string id)
        {
            return db.AspNetRoles.FirstOrDefault(d => d.Id == id);
        }

        public AspNetRole GetByName(string name)
        {
            return db.AspNetRoles.FirstOrDefault(d => d.Name == name);
        }

        public void DeleteOldPermissions(AspNetRole entry)
        {
            db.KMSPermissions.DeleteAllOnSubmit(db.KMSPermissions.Where(d => d.roleId == entry.Id));
            Save();
        }

        public void Delete(string id)
        {
            db.AspNetRoles.DeleteOnSubmit(db.AspNetRoles.FirstOrDefault(d => d.Id == id));
        }

        public void Save()
        {
            db.SubmitChanges();
        }

    }
    #endregion
}