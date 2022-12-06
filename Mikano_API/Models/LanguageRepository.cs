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
    [MetadataType(typeof(LanguageValidation))]
    public partial class Language
    {

    }

    public class LanguageValidation
    {
    }
    #endregion

    #region Methods
    public class LanguageRepository : SharedRepository
    {
        public dblinqDataContext db = new dblinqDataContext();

        public IQueryable<Language> GetAll()
        {
            return db.Languages.Where(d => !d.isDeleted).OrderByDescending(d => d.priority);
        }

        public IQueryable<Language> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public Language GetNextEntry(Language entry)
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

        public Language GetById(int id)
        {
            return GetAll().FirstOrDefault(d => d.id == id);
        }

        public Language GetByIdIsPublished(int id)
        {
            return GetAllIsPublished().FirstOrDefault(d => d.id == id);
        }

        public Language GetByCode(string code)
        {
            return GetAllIsPublished().FirstOrDefault(d => d.code == code);
        }

        public List<Language> GetAllOptional()
        {
            return GetAllIsPublished().Where(d => !d.isDefault).ToList();
        }

        //public Language GetByTitle(string title)
        //{
        //    return db.Languages.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        //}


        public int GetMaxPriority()
        {
            return db.Languages.Any() ? db.Languages.Max(d => d.priority) : 0;
        }

        public void Add(Language entry)
        {
            db.Languages.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.Languages.Where(d => !d.isDeleted);
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