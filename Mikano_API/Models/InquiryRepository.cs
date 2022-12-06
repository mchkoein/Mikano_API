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
    [MetadataType(typeof(InquiryValidation))]
    public partial class Inquiry
    {
        public string FullName
        {
            get
            {
                return this.firstName + " " + this.lastName;
            }
        }

    }

    public class InquiryValidation
    {
        //[Required]
        public int? inquiryTypeId { get; set; }
        //[Required]
        public string firstName { get; set; }
        //[Required]
        public string lastName { get; set; }

        public string fullName { get; set; }

        public string mobile { get; set; }

        [Required]
        public string email { get; set; }

        public int? countryId { get; set; }

        public string fileSrc { get; set; }


        [Required]
        public string message { get; set; }

    }
    #endregion

    #region Methods
    public class InquiryRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<Inquiry> GetAll()
        {
            return db.Inquiries.Where(d => !d.isDeleted).OrderByDescending(d => d.dateCreated);
        }

        public IQueryable<Inquiry> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public Inquiry GetNextEntry(Inquiry entry)
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

        public Inquiry GetById(int id)
        {
            return GetAll().FirstOrDefault(d => d.id == id);
        }

        public int GetMaxPriority()
        {
            return db.Inquiries.Any() ? db.Inquiries.Max(d => d.priority) : 0;
        }

        public void Add(Inquiry entry)
        {
            db.Inquiries.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.Inquiries.Where(d => !d.isDeleted);
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
        //    db.Inquiries.DeleteOnSubmit(db.Inquiries.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }

    }
    #endregion
}