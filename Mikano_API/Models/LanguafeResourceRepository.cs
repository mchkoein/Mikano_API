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
    [MetadataType(typeof(LanguageResourceValidation))]
    public partial class LanguageResource
    {
    }

    public class LanguageResourceValidation
    {

    }
    #endregion

    #region Methods
    public class LanguageResourceRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<LanguageResource> GetAll()
        {
            return db.LanguageResources.Where(d => !d.isDeleted).OrderByDescending(d => d.dateCreated);
        }

        public IQueryable<LanguageResource> GetAllIsPublished()
        {
            return GetAll().Where(d => !d.isDeleted).OrderByDescending(d => d.dateCreated);
        }


        public LanguageResource GetNextEntry(LanguageResource entry)
        {
            var nextEntry = GetAll().Where(d => d.dateCreated < entry.dateCreated).FirstOrDefault();

            if (nextEntry == null)
            {
                nextEntry = GetAll().Where(d => d.dateCreated != entry.dateCreated).FirstOrDefault();
            }
            if (nextEntry == null)
            {
                nextEntry = entry;
            }
            return nextEntry;
        }

        public LanguageResource GetById(int id)
        {
            return db.LanguageResources.FirstOrDefault(d => d.id == id && !d.isDeleted);
        }

        public LanguageResource GetByTitle(string title)
        {
            return db.LanguageResources.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        }



        public void Add(LanguageResource entry)
        {
            db.LanguageResources.InsertOnSubmit(entry);
        }

        //public void Delete(int id)
        //{
        //    db.LanguageResources.DeleteOnSubmit(db.LanguageResources.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}