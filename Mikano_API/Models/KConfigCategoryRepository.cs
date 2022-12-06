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
    [MetadataType(typeof(KConfigCategoryValidation))]
    public partial class KConfigCategory
    {
    }

    public class KConfigCategoryValidation
    {
        [Required]
        public string title { get; set; }

        public string subtitle { get; set; }

    }
    #endregion

    #region Methods
    public class KConfigCategoryRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<KConfigCategory> GetAll()
        {
            return db.KConfigCategories.Where(d => !d.isDeleted).OrderByDescending(d => d.priority);
        }

        public IQueryable<KConfigCategory> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished && !d.isDeleted).OrderByDescending(d => d.priority);
        }

        public KConfigCategory GetNextEntry(KConfigCategory entry)
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

        public KConfigCategory GetById(int id)
        {
            return db.KConfigCategories.FirstOrDefault(d => d.id == id && !d.isDeleted);
        }

        public KConfigCategory GetByTitle(string title)
        {
            return db.KConfigCategories.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }

        public int GetMaxPriority()
        {
            return db.KConfigCategories.Any() ? db.KConfigCategories.Max(d => d.priority) : 0;
        }

        public void Add(KConfigCategory entry)
        {
            db.KConfigCategories.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.KConfigCategories.Where(d => !d.isDeleted);
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
        //    db.SocialMedias.DeleteOnSubmit(db.SocialMedias.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }

    }
    #endregion
}