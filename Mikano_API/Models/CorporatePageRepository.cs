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
    [MetadataType(typeof(CorporatePageValidation))]
    public partial class CorporatePage
    {

        public int Level
        {
            get
            {
                try
                {
                    return 1 + (null != parentId ? this.CorporatePage1.Level : 0);
                }
                catch (Exception e)
                {
                    return 1;
                }
            }
        }

    }

    public class CorporatePageValidation
    {
        [Required]
        public string title { get; set; }

    }
    #endregion
    #region Methods
    public class CorporatePageRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<CorporatePage> GetAll(int? parentId = null)
        {
            var results = db.CorporatePages.Where(d => !d.isDeleted && d.languageParentId == null);
            if (parentId.HasValue && parentId != 0)
            {
                results = results.Where(d => d.parentId == parentId.Value);
            }
            else
            {
                results = results.Where(d => d.parentId == null);
            }
            return results.OrderByDescending(d => d.priority);
        }

        public IQueryable<CorporatePage> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }


        public CorporatePage GetNextEntry(CorporatePage entry)
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

        public CorporatePage GetById(int id)
        {
            return db.CorporatePages.Where(d => !d.isDeleted && d.id == id).FirstOrDefault();
        }

        public CorporatePage GetByTitle(string title)
        {
            return db.CorporatePages.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }

        public CorporatePage GetByComputerName(string computerName)
        {
            return db.CorporatePages.FirstOrDefault(d => d.title == computerName);
        }


        public int GetMaxPriority(int? parentId)
        {
            var results = db.CorporatePages.AsQueryable();
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

        public void Add(CorporatePage entry)
        {
            db.CorporatePages.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.CorporatePages.Where(d => !d.isDeleted);

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

        public IQueryable<CorporatePage> GetByParentId(int id)
        {
            return db.CorporatePages.Where(d => !d.isDeleted && d.isPublished && d.parentId == id);
        }

        //public void Delete(int id)
        //{
        //    db.CorporatePages.DeleteOnSubmit(db.CorporatePages.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}