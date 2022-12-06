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
    [MetadataType(typeof(CorporatePageSectionValidation))]
    public partial class CorporatePageSection
    {
        public int templateId { get; set; }
        public List<int> RelatedEKomCategories { get; set; }
        public List<int> RelatedEKomCategories1 { get; set; }
        public List<int> RelatedEKomCollections { get; set; }
        public List<int> RelatedEKomProducts { get; set; }

        public string Images { get; set; }
        public string Videos { get; set; }
        public string Files { get; set; }

        public List<UploaderCorporatePageSectionMediaModel> MediaFields { get; set; }
    }

    public class UploaderCorporatePageSectionMediaModel
    {
        public string mediaSrc { get; set; }
        public string caption { get; set; }
        public string subCaption { get; set; }
        public string description { get; set; }
        public string link { get; set; }
        public string fieldName { get; set; }
        public string fileUploadId { get; set; }
    }

    public class CorporatePageSectionValidation
    {
        //public string title { get; set; }
    }
    #endregion

    #region Methods
    public class CorporatePageSectionRepository : SharedRepository
    {
        public dblinqDataContext db = new dblinqDataContext();

        //public IQueryable<CorporatePageSection> GetAll(int? parentId = 0)
        //{
        //    var results = db.CorporatePageSections.Where(d=> !d.isDeleted);
        //    if (parentId != 0)
        //    {
        //        results = results.Where(d => d.corporatePageId == parentId.Value && d.subCorporatePageId == null);
        //    }
        //    return results.OrderByDescending(d => d.priority);
        //}

        public IQueryable<CorporatePageSection> GetAll(int parentId)
        {
            var results = db.CorporatePageSections.Where(d => d.corporatePageId == parentId && !d.isDeleted && d.subCorporatePageId == null);
            return results.OrderByDescending(d => d.priority);
        }

        public IQueryable<CorporatePageSection> GetAllByCorporatePageSectionParentId(int parentId)
        {
            var results = db.CorporatePageSections.Where(d => d.subCorporatePageId == parentId && !d.isDeleted);
            return results.OrderByDescending(d => d.priority);
        }

        public IQueryable<CorporatePageSection> GetAllForSubCorporateSection(int parentId)
        {
            var results = db.CorporatePageSections.Where(d => d.subCorporatePageId == parentId && !d.isDeleted);
            return results.OrderByDescending(d => d.priority);
        }

        public CorporatePageSection GetById(int id)
        {
            return db.CorporatePageSections.FirstOrDefault(d => d.id == id && !d.isDeleted);
        }

        public CorporatePageSection GetByParentId(int id)
        {
            return db.CorporatePageSections.FirstOrDefault(d => d.CorporatePageSection1.id == id && !d.isDeleted);
        }
        public CorporatePageTemplate GetByTemplateId(int id)
        {
            return db.CorporatePageTemplates.FirstOrDefault(d => d.id == id && !d.isDeleted);
        }

        public int GetMaxPriority(int parentId)
        {
            return db.CorporatePageSections.Any(e => e.corporatePageId == parentId) ? db.CorporatePageSections.Where(e => e.corporatePageId == parentId).Max(d => d.priority) : 0;
        }

        public void Add(CorporatePageSection entry)
        {
            db.CorporatePageSections.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id, string specialParam, string specialParamValue)
        {
            var entry = GetById(id);
            var allData = GetAll(entry.corporatePageId);
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

        public string SortGridRepeaters(int newIndex, int oldIndex, int id, string specialParam, string specialParamValue)
        {
            var entry = GetById(id);
            var allData = GetAllByCorporatePageSectionParentId(entry.subCorporatePageId.Value);
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

        public void Delete(int id)
        {
            var entry = GetById(id);
            //Save();
            db.CorporatePageSections.DeleteOnSubmit(entry);
        }

        public void Save()
        {
            db.SubmitChanges();
        }

        #region Manage Related Entries (Categories, Banner Advertising etc.)







        #endregion

        #region Related Media 
        public void DeleteRelatedMedias(CorporatePageSection entry, int mediaType)
        {
            db.CorporatePageSectionMedias.DeleteAllOnSubmit(db.CorporatePageSectionMedias.Where(d => d.corporatePageSectionId == entry.id && d.mediaType == mediaType));
        }
        #endregion   

    }
    #endregion
}