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
    [MetadataType(typeof(ImageResizeValidation))]
    public partial class ImageResize
    {
        public string FullSize
        {
            get
            {
                return this.width + "x" + this.height + "x" + (this.isInside ? "i" : "o");
            }
        }
    }

    public class ImageResizeValidation
    {
        [Required]
        public string width { get; set; }

        [Required]
        public string height { get; set; }

        [Range(0, 5)]
        public bool isInside { get; set; }

        [Required]
        public string section { get; set; }
    }
    #endregion

    #region Methods
    public class ImageResizeRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        public IQueryable<ImageResize> GetAll()
        {
            return db.ImageResizes.Where(d => !d.isDeleted).OrderByDescending(d => d.dateCreated);
        }

        public IQueryable<ImageResize> GetAllBySectionName(string sectionName)
        {
            return GetAll().Where(d => d.section.ToLower() == sectionName.ToLower());
        }

        //public IQueryable<EKomProductReview> GetAllIsPublished()
        //{
        //	return GetAll().Where(d => d.isPublished);
        //}

        public ImageResize GetById(int id)
        {
            return GetAll().FirstOrDefault(d => d.id == id);
        }


        public void Add(ImageResize entry)
        {
            db.ImageResizes.InsertOnSubmit(entry);
        }

        //public void Delete(int id)
        //{
        //    db.EKomProductReviews.DeleteOnSubmit(db.EKomProductReviews.FirstOrDefault(d => d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}