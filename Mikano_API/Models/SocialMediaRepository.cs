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
    [MetadataType(typeof(SocialMediaValidation))]
    public partial class SocialMedia
    {
    }

    public class SocialMediaValidation
    {
        [Required]
        public string title { get; set; }

        public string link { get; set; }

    }
    #endregion

    #region Methods
    public class SocialMediaRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<SocialMedia> GetAll()
        {
            return db.SocialMedias.Where(d => !d.isDeleted && d.languageParentId == null).OrderByDescending(d => d.priority);
        }

        public IQueryable<SocialMedia> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished && !d.isDeleted).OrderByDescending(d => d.priority);
        }


        public SocialMedia GetNextEntry(SocialMedia entry)
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

        public SocialMedia GetById(int id)
        {
            return db.SocialMedias.FirstOrDefault(d => d.id == id && !d.isDeleted);
        }

        public SocialMedia GetByTitle(string title)
        {
            return db.SocialMedias.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }



        public int GetMaxPriority()
        {
            return db.SocialMedias.Any() ? db.SocialMedias.Max(d => d.priority) : 0;
        }

        public void Add(SocialMedia entry)
        {
            db.SocialMedias.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.SocialMedias.Where(d => !d.isDeleted);
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