using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Models
{
    #region Models
    [MetadataType(typeof(KMSSectionValidation))]
    public partial class KMSSection
    {
        public int Level
        {
            get
            {
                try
                {
                    return 1 + (this.nesteLabelUnderId != null ? this.KMSSection2.Level : 0);
                }
                catch (Exception e)
                {
                    return 1;
                }
            }
        }
        public object NestedMenues(string itemId)
        {
            return new
            {
                id = this.id,
                relatedSections = this.KMSSections.Select(q => new
                {
                    id = q.id,
                    computername = q.computername,
                    component = q.component,
                    labelInParent = q.labelInParent,
                }),
                depth = this.depth,
                computername = this.computername,
                component = this.component,
                title = this.title,
                icon = this.icon,
                showOnMenu = this.showOnMenu,
                isUnderline = this.isUnderline,
                isSortable = this.isSortable,
                isPublishable = this.isPublishable,
                hasReadyForPublish = this.hasReadyForPublish,
                CanCreate = this.KMSPermissions.Any(e => e.roleId == itemId && e.kmsPermissionTypeId == (int)KActions.create),
                CanRead = this.KMSPermissions.Any(e => e.roleId == itemId && e.kmsPermissionTypeId == (int)KActions.read),
                CanUpdate = this.KMSPermissions.Any(e => e.roleId == itemId && e.kmsPermissionTypeId == (int)KActions.update),
                CanDelete = this.KMSPermissions.Any(e => e.roleId == itemId && e.kmsPermissionTypeId == (int)KActions.delete),
                CanPublish = this.KMSPermissions.Any(e => e.roleId == itemId && e.kmsPermissionTypeId == (int)KActions.publish),
                nestedSections = !this.KMSSections1.Any() ? null : this.KMSSections1.Select(e => e.NestedMenues(itemId))
            };
        }



    }

    public class KMSSectionValidation
    {
        [Required]
        public string title { get; set; }

        [Required]
        public string computername { get; set; }
    }
    #endregion

    #region Methods
    public class KMSSectionRepository : SharedRepository
    {
        public dblinqDataContext db = new dblinqDataContext();
        public IQueryable<KMSSection> GetAll()
        {
            return db.KMSSections.OrderByDescending(d => d.priority);
        }

        public IQueryable<KMSSection> GetAllForRoles()
        {
            return db.KMSSections.Where(d => d.component.ToLower() != "sectionsgroup").GroupBy(d => d.computername).Select(d => d.OrderByDescending(e => e.component).FirstOrDefault()).OrderByDescending(d => d.priority);
        }

        public IQueryable<KMSSection> GetAllSectionsGroup()
        {
            return db.KMSSections.Where(d => d.component.ToLower() == "sectionsgroup");
        }

        public KMSSection GetNextEntry(KMSSection entry)
        {
            var nextEntry = GetAll().Where(d => d.priority < entry.priority).FirstOrDefault();

            if (nextEntry == null)
            {
                nextEntry = GetAll().Where(d => d.priority != entry.priority).FirstOrDefault();
            }
            if (nextEntry == null)
            {
                nextEntry = entry;
            }
            return nextEntry;
        }

        public KMSSection GetById(int id)
        {
            return db.KMSSections.FirstOrDefault(d => d.id == id);
        }

        public KMSSection GetByComputerName(string computername, string component = null, int? configuration = null)
        {
            if (configuration.HasValue)
            {
                return db.KMSSections.FirstOrDefault(d => d.id == configuration);
            }
            else
            {
                var results = db.KMSSections.Where(d => d.computername.ToLower() == computername.ToLower());
                if (!string.IsNullOrEmpty(component))
                {
                    results = results.Where(d => d.component.ToLower() == component.ToLower());
                }
                return results.FirstOrDefault();
            }
        }

        public bool GetPermission(string sectionName, string userId, int permissionTypeId)
        {
            return db.KMSPermissions.Any(d => d.kmsPermissionTypeId == permissionTypeId && d.KMSSection.computername.ToLower() == sectionName.ToLower() && d.AspNetRole.AspNetUserRoles.Any(e => e.UserId == userId));
        }

        //public KPermission GetPermissions(string sectionName, string userId)
        //{
        //    KPermission entry = new KPermission();
        //    entry.sectionEntry = GetByComputerName(sectionName);
        //    entry.CanRead = GetPermission(sectionName, userId, (int)KActions.read);
        //    entry.CanCreate = GetPermission(sectionName, userId, (int)KActions.create);
        //    entry.CanUpdate = GetPermission(sectionName, userId, (int)KActions.update);
        //    entry.CanDelete = GetPermission(sectionName, userId, (int)KActions.delete);
        //    entry.CanPublish = GetPermission(sectionName, userId, (int)KActions.publish);
        //    return entry;
        //}

        public int GetMaxPriority()
        {
            return db.KMSSections.Any() ? db.KMSSections.Max(d => d.priority) : 0;
        }

        public void Add(KMSSection entry)
        {
            db.KMSSections.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.KMSSections;
            var steps = newIndex - oldIndex;
            //decreasing priority
            if (steps > 0)
            {
                int lastRow = 0;
                int counter = 0;
                var tempAllData = allData.OrderByDescending(d => d.priority).Where(d => d.priority < entry.priority).Take(steps).ToList();
                foreach (var item in tempAllData)
                {
                    counter++;
                    if (counter == tempAllData.Count)
                    {
                        lastRow = item.priority;
                    }
                    item.priority++;
                    Save();
                }

                entry.priority = lastRow;
                Save();
            }
            else
            {
                //increasing priority
                int lastRow = 0;
                int counter = 0;
                var tempAllData = allData.OrderBy(d => d.priority).Where(d => d.priority > entry.priority).Take(Math.Abs(steps)).ToList();
                foreach (var item in tempAllData)
                {
                    counter++;
                    if (counter == tempAllData.Count)
                    {
                        lastRow = item.priority;
                    }
                    item.priority--;
                    Save();
                }
                entry.priority = lastRow;
                Save();
            }
            return "success";
        }

        public void Delete(int id)
        {
            db.KMSSections.DeleteOnSubmit(db.KMSSections.FirstOrDefault(d => d.id == id));
        }

        public void Save()
        {
            db.SubmitChanges();
        }
    }
    #endregion
}