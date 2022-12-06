using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Configuration;
using Mikano_API.Models;
using System.Net;
using System.Text;
//using System.Web.Script.Serialization;
using System.IO;

namespace Mikano_API.Models
{

    public class NewsletterUserCategoryRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public void Add(NewsletterUserCategory entry)
        {
            db.NewsletterUserCategories.InsertOnSubmit(entry);
        }

        public IQueryable<NewsletterUserCategory> GetAll()
        {
            return db.NewsletterUserCategories;
        }

        public void Delete(NewsletterUserCategory entry)
        {
            db.NewsletterUserCategories.DeleteOnSubmit(entry);
        }

        public void Save()
        {
            db.SubmitChanges();
        }

    }
}