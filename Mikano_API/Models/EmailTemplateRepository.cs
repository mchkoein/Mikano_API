using Mikano_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mikano_API.Models
{


    public class EmailTemplateRepository : SharedRepository
    {

        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<EmailTemplate> GetAll()
        {
            var results = db.EmailTemplates.Where(d => !d.isDeleted);
            return results.OrderByDescending(d => d.priority);
        }

        public EmailTemplate GetNextEntry(EmailTemplate entry)
        {
            var allData = GetAll().ToList();
            var indexOfCurrentItem = allData.IndexOf(entry);

            EmailTemplate nextEntry = allData.Count >= indexOfCurrentItem + 1 ? null : allData[indexOfCurrentItem + 1];

            if (nextEntry == null)
            {
                nextEntry = allData.Where(d => d.id != entry.id).FirstOrDefault();
            }
            if (nextEntry == null)
            {
                nextEntry = entry;
            }
            return nextEntry;
        }

        public EmailTemplate GetById(int id)
        {
            return db.EmailTemplates.FirstOrDefault(d => d.id == id);
        }
        public void Delete(int id)
        {
            db.EmailTemplates.DeleteOnSubmit(db.EmailTemplates.FirstOrDefault(d => d.id == id));
        }
        public EmailTemplate GetByName(string name)
        {
            return db.EmailTemplates.FirstOrDefault(d => d.title == name); // changed title to question
        }


        public void Save()
        {
            db.SubmitChanges();
        }

        public void Add(EmailTemplate entry)
        {
            db.EmailTemplates.InsertOnSubmit(entry);
        }

        public int GetMaxPriority()
        {
            return db.EmailTemplates.Any() ? db.EmailTemplates.Max(d => d.priority) : 0;
        }

        public string SortGrid(int EmailTemplateIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.EmailTemplates.Where(d => !d.isDeleted);
            var steps = EmailTemplateIndex - oldIndex;
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



    }
}