using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using Mikano_API.Models;
using System.Configuration;

namespace Mikano_API.Models
{
    public class CountryRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<Country> GetAll()
        {
            return db.Countries.Where(d => !d.isDeleted && d.languageParentId == null).OrderBy(d => d.CountryName);
        }

        public Country GetById(int? id)
        {
            return GetAll().FirstOrDefault(d => d.id == id);
        }

        public Country GetByName(string name)
        {
            return db.Countries.SingleOrDefault(d => d.CountryName.ToLower() == name.ToLower());
        }

        public Country GetByCountryId(int id)
        {
            return db.Countries.SingleOrDefault(d => d.id == id);
        }

        public void Add(Country entry)
        {
            db.Countries.InsertOnSubmit(entry);
        }

        public void Save()
        {
            db.SubmitChanges();
        }

    }
}