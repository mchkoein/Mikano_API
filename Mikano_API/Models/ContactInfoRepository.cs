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
    [MetadataType(typeof(ContactInfoValidation))]
    public partial class ContactInfo
    {
    }

    public class ContactInfoValidation
    {
        public string title { get; set; }
    }
    #endregion

    #region Methods
    public class ContactInfoRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<ContactInfo> GetAll()
        {
            return db.ContactInfos.Where(d => !d.isDeleted && d.languageParentId == null).OrderByDescending(d => d.priority);
        }

        public IQueryable<ContactInfo> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public IQueryable<ContactInfo> GetAllHasOrderPickup()
        {
            return GetAll().Where(d => d.hasOrderPickup);
        }

        public ContactInfo GetNextEntry(ContactInfo entry)
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

        public ContactInfo GetById(int id)
        {
            return db.ContactInfos.FirstOrDefault(d => d.id == id && !d.isDeleted);
        }

        public ContactInfo GetByTitle(string title)
        {
            return db.ContactInfos.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }



        public int GetMaxPriority()
        {
            return db.ContactInfos.Any() ? db.ContactInfos.Max(d => d.priority) : 0;
        }

        public void Add(ContactInfo entry)
        {
            db.ContactInfos.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.ContactInfos.Where(d => !d.isDeleted);
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

        public ContactInfo GetFirst()
        {
            return db.ContactInfos.FirstOrDefault(d => !d.isDeleted);
        }
        public ContactInfo GetFirstIsPublished()
        {
            return db.ContactInfos.FirstOrDefault(d => !d.isDeleted && d.isPublished);
        }

    }
    #endregion
}