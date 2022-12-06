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
    [MetadataType(typeof(IconListValidation))]
    public partial class IconList
    {

    }

    public class IconListValidation
    {
        [Required]
        public string title { get; set; }
    }
    #endregion

    #region Methods
    public class IconListRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<IconList> GetAll()
        {
            return db.IconLists.Where(d => !d.isDeleted).OrderByDescending(d => d.priority);
        }

        public IQueryable<IconList> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public IconList GetNextEntry(IconList entry)
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

        public IconList GetById(int id)
        {
            return db.IconLists.FirstOrDefault(d => !d.isDeleted && d.id == id);
        }

        public IconList GetByTitle(string title)
        {
            return db.IconLists.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }


        public int GetMaxPriority()
        {
            return db.IconLists.Any() ? db.IconLists.Max(d => d.priority) : 0;
        }

        public void Add(IconList entry)
        {
            db.IconLists.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.IconLists.Where(d => !d.isDeleted);
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