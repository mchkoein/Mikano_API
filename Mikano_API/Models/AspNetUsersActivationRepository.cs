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
    [MetadataType(typeof(AspNetUsersActivationValidation))]
    public partial class AspNetUsersActivation
    {
    }

    public class AspNetUsersActivationValidation
    {
        [Required]
        public string title { get; set; }

    }
    #endregion

    #region Methods
    public class AspNetUsersActivationRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<AspNetUsersActivation> GetAll()
        {
            return db.AspNetUsersActivations.Where(d => !d.isDeleted).OrderByDescending(d => d.priority);
        }

        public IQueryable<AspNetUsersActivation> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }


        public AspNetUsersActivation GetNextEntry(AspNetUsersActivation entry)
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

        public AspNetUsersActivation GetById(int id)
        {
            return GetAll().FirstOrDefault(d => d.id == id);
        }

        public AspNetUsersActivation GetByTitle(string title)
        {
            return db.AspNetUsersActivations.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }



        public int GetMaxPriority()
        {
            return db.AspNetUsersActivations.Any() ? db.AspNetUsersActivations.Max(d => d.priority) : 0;
        }

        public void Add(AspNetUsersActivation entry)
        {
            db.AspNetUsersActivations.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.AspNetUsersActivations.Where(d => !d.isDeleted);
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
        //    db.AspNetUsersActivations.DeleteOnSubmit(db.AspNetUsersActivations.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}