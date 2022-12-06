using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Mikano_API.Helpers;

namespace Mikano_API.Models
{
    #region Models
    [MetadataType(typeof(RegionValidation))]
    public partial class Region
    {
        public int Level
        {
            get
            {
                try
                {
                    return 1 + (null != parentId ? this.Region1.Level : 0);
                }
                catch (Exception e)
                {
                    return 1;
                }
            }
        }

    }

    public class RegionValidation
    {
        [Required]
        public string title { get; set; }

    }
    #endregion

    #region Methods
    public class RegionRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<Region> GetAll(int? parentId = null)
        {
            var results = db.Regions.Where(d => !d.isDeleted);
            if (parentId.HasValue && parentId != 0)
            {
                results = results.Where(d => d.parentId == parentId.Value);
            }
            else
            {
                results = results.Where(d => d.parentId == null);
            }
            return results.OrderBy(d => d.title);
        }


        public IQueryable<Region> GetRegions()
        {
            return db.Regions.Where(d => !d.isDeleted && !d.parentId.HasValue).OrderBy(d => d.title);
        }

        public IQueryable<Region> GetSubRegions()
        {
            return db.Regions.Where(d => !d.isDeleted && d.parentId.HasValue).OrderBy(d => d.title);
        }

        public IQueryable<Region> GetSubRegionsByParent(int id)
        {
            return db.Regions.Where(d => !d.isDeleted && d.parentId == id).OrderBy(d => d.title);
        }

        public Region GetRegionByPhone(string phone)
        {
            if (!string.IsNullOrEmpty(phone))
            {
                phone = phone.TrimStart('0');
                var firstChar = phone.Substring(0, 1);
                return db.Regions.FirstOrDefault(d => !d.isDeleted && d.phoneCode == firstChar);
            }
            else
            {
                return null;
            }
        }

        public IQueryable<Region> GetAllRegionByPhone(string phone)
        {
            if (!string.IsNullOrEmpty(phone))
            {
                phone = phone.TrimStart('0');
                var firstChar = phone.Substring(0, 1);
                return db.Regions.Where(d => !d.isDeleted && !d.parentId.HasValue && d.phoneCode.Contains(firstChar));
            }
            else
            {
                return null;
            }
        }


        public IQueryable<Region> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }


        public Region GetNextEntry(Region entry)
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

        public Region GetById(int id)
        {
            return db.Regions.FirstOrDefault(d => !d.isDeleted && d.id == id);
        }

        public Region GetByTitle(string title)
        {
            return db.Regions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }


        public Region GetParentByTitle(string title)
        {
            return db.Regions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower() && !d.parentId.HasValue);
        }

        public Region GetChildByTitle(string title)
        {
            return db.Regions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower() && d.parentId.HasValue);
        }

        public int GetMaxPriority(int? parentId)
        {
            var results = db.Regions.AsQueryable();
            if (parentId.HasValue)
            {
                results = results.Where(d => d.parentId == parentId);
            }
            else
            {
                results = results.Where(d => d.parentId == null);

            }
            return results.Any() ? results.Max(d => d.priority) : 0;
        }

        public void Add(Region entry)
        {
            db.Regions.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.Regions.Where(d => !d.isDeleted);

            if (entry.parentId.HasValue)
            {
                allData = allData.Where(d => d.parentId == entry.parentId);
            }
            else
            {
                allData = allData.Where(d => d.parentId == null);
            }
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



        //public void Delete(int id)
        //{
        //    db.Regions.DeleteOnSubmit(db.Regions.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}