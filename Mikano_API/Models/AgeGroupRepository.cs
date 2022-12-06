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
    [MetadataType(typeof(AgeGroupValidation))]
    public partial class AgeGroup
    {

    }

    public class AgeGroupValidation
    {
        [Required]
        public string title { get; set; }
    }
    #endregion

    #region Methods
    public class AgeGroupRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<AgeGroup> GetAll()
        {
            return db.AgeGroups.Where(d => !d.isDeleted).OrderByDescending(d => d.priority);
        }

        public IQueryable<AgeGroup> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public AgeGroup GetNextEntry(AgeGroup entry)
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

        public AgeGroup GetById(int id)
        {
            return db.AgeGroups.FirstOrDefault(d => !d.isDeleted && d.id == id);
        }

        public AgeGroup GetByTitle(string title)
        {
            return db.AgeGroups.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }


        public int GetMaxPriority()
        {
            return db.AgeGroups.Any() ? db.AgeGroups.Max(d => d.priority) : 0;
        }

        public void Add(AgeGroup entry)
        {
            db.AgeGroups.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.AgeGroups.Where(d => !d.isDeleted);
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

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}