using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Mikano_API.Helpers;
using static Mikano_API.Models.KMSEnums;
using System.Configuration;
using System.Data.SqlClient;

namespace Mikano_API.Models
{
    #region Models
    [MetadataType(typeof(DivisionValidation))]
    public partial class Division
    {
        public int Level
        {
            get
            {
                try
                {
                    return 1 + (null != parentId ? this.Division1.Level : 0);
                }
                catch (Exception e)
                {
                    return 1;
                }
            }
        }

        public string TitleWithPreviouses
        {
            get
            {
                try
                {
                    var level = this.Level;
                    if (level == 1)
                    {
                        return this.title;
                    }
                    else
                    {
                        if (level == 2)
                        {
                            return this.Division1.title + " » " + this.title;
                        }
                        else
                        {
                            if (level == 3)
                            {
                                return this.Division1.Division1.title + " » " + this.Division1.title + " » " + this.title;
                            }
                            else
                            {
                                if (level == 4)
                                {
                                    return this.Division1.Division1.Division1.title + " » " + this.Division1.Division1.title + " » " + this.Division1.title + " » " + this.title;
                                }
                                else
                                {
                                    return this.title;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    return this.title;
                }
            }
        }

        public string GetTitleByLevel(int level = 1)
        {
            try
            {
                if (level == 1)
                {
                    return this.title;
                }
                else
                {
                    if (level == 2 && this.Division1 != null)
                    {
                        return this.Division1.title;
                    }
                    else
                    {
                        if (level == 3 && this.Division1 != null && this.Division1.Division1 != null)
                        {
                            return this.Division1.Division1.title;
                        }
                        else
                        {
                            if (level == 4 && this.Division1 != null && this.Division1.Division1 != null && this.Division1.Division1.Division1 != null)
                            {
                                return this.Division1.Division1.Division1.title;
                            }
                            else
                            {
                                return "";
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }

    public class DivisionValidation
    {
        [Required]
        public string title { get; set; }
    }
    #endregion

    #region Methods
    public class DivisionRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<Division> GetAllCategories()
        {
            return db.Divisions.Where(d => d.isPublished && !d.isDeleted && d.languageParentId == null);
        }

        public IQueryable<Division> GetCategories()
        {
            return db.Divisions.Where(d => !d.isDeleted && !d.languageParentId.HasValue);
        }

        public IQueryable<Division> GetAll(int? parentId = null)
        {
            var results = db.Divisions.Where(d => !d.isDeleted && d.languageParentId == null);
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

        public IQueryable<Division> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public IQueryable<Division> GetSubCategories()
        {
            return db.Divisions.Where(d => !d.isDeleted && d.parentId.HasValue && d.languageParentId == null).OrderBy(d => d.title);
        }

        public IQueryable<Division> GetPublishedSubCategories()
        {
            return db.Divisions.Where(d => !d.isDeleted && d.isPublished && d.parentId.HasValue && d.languageParentId == null).OrderBy(d => d.title);
        }
        public IQueryable<Division> GetSubCategoriesByParent(int id)
        {
            return db.Divisions.Where(d => !d.isDeleted && d.parentId == id && d.languageParentId == null).OrderBy(d => d.title);
        }

        public Division GetNextEntry(Division entry)
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

        public Division GetById(int id)
        {
            return db.Divisions.FirstOrDefault(d => !d.isDeleted && d.id == id);
        }

        public Division GetByIdIsPublished(int id)
        {
            return db.Divisions.FirstOrDefault(d => d.isPublished && !d.isDeleted && d.id == id);
        }

        public Division GetByTitleForImport(string title)
        {
            return db.Divisions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }

        public Division GetByTitleAndParentTitle(string title, string parentTitle, string parent1Title, string parent2Title)
        {
            var entries = db.Divisions.Where(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
            if (!string.IsNullOrEmpty(parentTitle))
            {
                entries = entries.Where(d => d.Division1.title.ToLower() == parentTitle.ToLower());
            }
            if (!string.IsNullOrEmpty(parent1Title))
            {
                entries = entries.Where(d => d.Division1.Division1.title.ToLower() == parent1Title.ToLower());
            }
            if (!string.IsNullOrEmpty(parent2Title))
            {
                entries = entries.Where(d => d.Division1.Division1.Division1.title.ToLower() == parent2Title.ToLower());
            }

            return entries.FirstOrDefault();
        }

        public Division GetParentByTitle(string title)
        {
            return db.Divisions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower() && !d.parentId.HasValue);
        }

        public Division GetByTitle(string title)
        {
            return db.Divisions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }

        public Division GetByTitleAndParentId(string title, int parentId)
        {
            return db.Divisions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower() && d.parentId == parentId);
        }

        public Division GetChildByTitle(string title)
        {
            return db.Divisions.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower() && d.parentId.HasValue);
        }

        public IQueryable<Division> GetByListOfTitle(List<string> titles, int? parentId = null)
        {
            var test = db.Divisions.Select(d => d.title);
            var entries = db.Divisions.Where(d => titles.Contains(d.title) && d.isPublished);
            if (parentId.HasValue)
            {
                entries = entries.Where(d => d.parentId == parentId);
            }
            return entries;
        }

        public IQueryable<Division> GetAllSubcats()
        {
            var subcats = db.Divisions.Where(d => d.Level == 2 && d.isPublished);

            return subcats;
        }

        public bool CheckIfSubcatExist(List<string> subCats)
        {
            var subcats = db.Divisions.Where(d => subCats.Contains(d.title) && d.isPublished);

            if (subcats.Count() >= 1)
            {
                return true;
            }
            return false;
        }

        public bool CheckExistance(int level)
        {
            return db.Divisions.Any(d => d.Level == level);
        }

        public int GetMaxPriority(int? parentId)
        {
            var results = db.Divisions.AsQueryable();
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

        public int GetMaxPriorityForCategory(int id)
        {
            Division categoryEntry = GetById(id);
            return categoryEntry != null && categoryEntry.Divisions.Any() ? categoryEntry.Divisions.Max(d => d.priority) : 0;
        }

        public void Add(Division entry)
        {
            db.Divisions.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.Divisions.Where(d => !d.isDeleted);

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

        //public void Delete(int id)
        //{
        //    db.EKomCategories.DeleteOnSubmit(db.EKomCategories.FirstOrDefault(d=>d.id == id));
        //}

        public void UpdateSubDeletedRecords()
        {
            //db.ExecuteCommand("update EKomCategory set isDeleted = 1 where parentid in (select id from EKomCategory where isdeleted=1)");

            #region Execute Command
            string connection = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection sqlConnection = new SqlConnection(connection);
            string query = "UPDATE Division SET isDeleted = 1 WHERE parentId IN (SELECT id FROM Division WHERE isDeleted = 1)";
            SqlCommand cmd = new SqlCommand(query, sqlConnection);
            sqlConnection.Open();
            cmd.ExecuteNonQuery();
            sqlConnection.Close();
            #endregion
        }

        public void Save()
        {
            db.SubmitChanges();
        }

    }
    #endregion
}