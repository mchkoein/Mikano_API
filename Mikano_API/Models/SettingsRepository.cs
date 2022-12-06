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

    public partial class SettingsFile
    {
        public string GetImageName
        {
            get
            {
                string name = this.imgSrc;
                if (name.Contains("~"))
                {
                    name = name.Split('~')[1];
                }

                var nameArray = name.Split('.');
                nameArray = nameArray.Take(nameArray.Count() - 1).ToArray();
                name = string.Join(" ", nameArray);
                return name.Replace("_", " ").Replace("-", " ");
            }
        }
    }
    [MetadataType(typeof(SettingValidation))]
    public partial class Setting
    {
        public string DSLCorporate { get; set; }
        public string DSLIndividual { get; set; }
        public string FOCorporate { get; set; }
        public string FOIndividual { get; set; }

        public IQueryable<SettingsFile> GetFilesByType(int typeId)
        {

            return this.SettingsFiles.Where(d => d.fileTypeId == typeId).AsQueryable();
        }

        public SettingsFile GetFirstFileByType(int typeId)
        {
            return this.GetFilesByType(typeId).FirstOrDefault();
        }

    }

    public class SettingValidation
    {

        // [Required]
        // public string adminEmailDSLApplication { get; set; }
        //[Required]
        // public string adminEmailDSLApplicationSignedForms { get; set; }
        // [Required]
        //public string adminEmailProductRequest { get; set; }
        // [Required]
        //public string adminEmailRefillWalletRequest { get; set; }
        //[Required]
        //public string adminEmailRequestChangeInfo { get; set; }
        //[Required]
        //public string adminEmailRequestSalesAgent { get; set; }
        //[Required]
        //public string emailAccountActivated { get; set; }
        //[Required]
        //public string emailAfterRefillByCreditCard { get; set; }
        //[Required]
        //public string emailContactUs { get; set; }
        //[Required]
        //public string emailResellerRefillTransaction { get; set; }
        //[Required]
        //public string emailResellerDeductionTransaction { get; set; }
        //[Required]
        //public string emailResetPassword { get; set; }
        //[Required]
        //public string emailToRefillWallet { get; set; }
        //[Required]
        //public string emailToResellerDSL { get; set; }
        //[Required]
        //public string emailToResellerUpdateInfo { get; set; }
        //[Required]
        //public string emailtoUserDSL { get; set; }
        //[Required]
        //public string emailWhenSellingProduct { get; set; }
    }
    #endregion

    #region Methods
    public class SettingsRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public Setting GetFirstOrDefault()
        {
            return db.Settings.FirstOrDefault();
        }

        public void DeleteRelatedImages(Setting entry)
        {
            db.SettingsFiles.DeleteAllOnSubmit(db.SettingsFiles.Where(d => d.settingsId == entry.id));
            Save();
        }

        public void Save()
        {
            db.SubmitChanges();
        }


    }
    #endregion
}