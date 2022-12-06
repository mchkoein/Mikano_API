using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Xml;

namespace Mikano_API.Models
{
    #region Model
    [MetadataType(typeof(KConfigValidation))]
    public partial class KConfig
    {
        public static string Get(string key)
        {
            try
            {
                return (new dblinqDataContext()).KConfigs.SingleOrDefault(d => d.key == key).value;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public int KConfigId { get; set; }

    }

    public class KConfigValidation
    {
        public string key { get; set; }

        public string value { get; set; }

        public string description { get; set; }

        public int categoryId { get; set; }

        public int id { get; set; }

        public DateTime dateCreated { get; set; }

        public DateTime dateModified { get; set; }

    }
    #endregion

    #region Methods
    public class KConfigRepository
    {
        public dblinqDataContext db = new dblinqDataContext();

        public IQueryable<KConfig> GetAll(int parentId)
        {
            var results = db.KConfigs.Where(d => d.categoryId == parentId);
            return results.OrderByDescending(d => d.key);
        }

        public KConfig GetById(int id)
        {
            return db.KConfigs.FirstOrDefault(d => d.id == id);
        }

        //public string GetByKey(string key, string categoryTitle)
        //{
        //	var entry = db.KConfigs.FirstOrDefault(d => d.key.ToLower() == key.ToLower() && !d.isDeleted  && d.KConfigCategory.title.ToLower() == categoryTitle.ToLower() && d.KConfigCategory.isPublished && !d.KConfigCategory.isDeleted);
        //	return entry == null ? "" : entry.value;
        //}

        public void Add(KConfig entry)
        {
            db.KConfigs.InsertOnSubmit(entry);
        }

        public void Delete(int id)
        {
            var entry = GetById(id);
            db.KConfigs.DeleteOnSubmit(entry);
        }

        public void Save()
        {
            db.SubmitChanges();
        }
    }
    #endregion
}