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
    [MetadataType(typeof(CompanyValidation))]
    public partial class Company
    {

        public int Level
        {
            get
            {
                try
                {
                    return 1 + (null != parentId ? this.Company2.Level : 0);
                }
                catch (Exception e)
                {
                    return 1;
                }
            }
        }

    }

    public class CompanyValidation
    {
        [Required]
        public string title { get; set; }

    }
    #endregion

    #region Methods
    public class CompanyRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<Company> GetAll(int? parentId = null)
        {
            var results = db.Companies.Where(d => !d.isDeleted && d.languageParentId == null);
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

        public IQueryable<Company> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public IQueryable<Company> GetAllPublishedCompanies()
        {
            return db.Companies.Where(d => !d.isDeleted && d.languageParentId == null && d.isPublished).OrderByDescending(d => d.priority);
        }


        public Company GetNextEntry(Company entry)
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

        public Company GetById(int id)
        {
            return db.Companies.Where(d => !d.isDeleted && d.id == id).FirstOrDefault();
        }

        public Company GetByTitle(string title)
        {
            return db.Companies.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }

        public Company GetByComputerName(string computerName)
        {
            return db.Companies.FirstOrDefault(d => d.title == computerName);
        }


        public int GetMaxPriority(int? parentId)
        {
            var results = db.Companies.AsQueryable();
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

        public void Add(Company entry)
        {
            db.Companies.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.Companies.Where(d => !d.isDeleted);

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

        public IQueryable<Company> GetByParentId(int id)
        {
            return db.Companies.Where(d => !d.isDeleted && d.isPublished && d.parentId == id);
        }

        //public void Delete(int id)
        //{
        //    db.Companies.DeleteOnSubmit(db.Companies.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}