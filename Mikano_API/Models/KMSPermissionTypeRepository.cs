
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web;

namespace Mikano_API.Models
{
    #region Models

    #endregion

    #region Methods
    public class KMSPermissionTypeRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<KMSPermissionType> GetAllIsPublished()
        {
            return db.KMSPermissionTypes.Where(d => d.isPublished);
        }
    }
    #endregion
}