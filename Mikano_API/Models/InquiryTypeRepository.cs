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
    [MetadataType(typeof(InquiryTypeValidation))]
    public partial class InquiryType
    {
    }

    public class InquiryTypeValidation
    {
        [Required]
        public string title { get; set; }

        //[DataType(DataType.EmailAddress)]
        //[Required]
        //public string email { get; set; }

    }
    #endregion

    #region Methods
    public class InquiryTypeRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<InquiryType> GetAll()
        {
            return db.InquiryTypes.Where(d => !d.isDeleted).OrderByDescending(d => d.priority);
        }

        public IQueryable<InquiryType> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }


        public InquiryType GetNextEntry(InquiryType entry)
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

        public InquiryType GetById(int id)
        {
            return GetAll().FirstOrDefault(d => d.id == id);
        }

        public InquiryType GetByTitle(string title)
        {
            return db.InquiryTypes.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }



        public int GetMaxPriority()
        {
            return db.InquiryTypes.Any() ? db.InquiryTypes.Max(d => d.priority) : 0;
        }

        public void Add(InquiryType entry)
        {
            db.InquiryTypes.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.InquiryTypes.Where(d => !d.isDeleted);
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
        //    db.InquiryTypes.DeleteOnSubmit(db.InquiryTypes.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}