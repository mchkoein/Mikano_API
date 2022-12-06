using Mikano_API.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Web;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Models
{
    public class KMSLogRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        KConfigRepository kConfigRpstry = new KConfigRepository();


        public IQueryable<KMSLog> GetAll()
        {
            return db.KMSLogs.Where(d => !d.hasError).OrderByDescending(d => d.date);
        }

        public void AddLog(string aspNetUsersId, string adminName, string action, string assetType, string assetId, string assetTitle, string assets, string ipAddress, bool hasError = false, string subAction = null, int? subActionId = null)
        {
            ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
            if ((projectConfigKeys.enableTrackingErrors && hasError) || !hasError)
            {
                KMSLog entry = new KMSLog();
                entry.aspNetUsersId = aspNetUsersId;
                entry.adminName = adminName;
                entry.action = action;
                entry.assetType = assetType;
                entry.assetId = assetId;
                entry.assetTitle = assetTitle;
                if (action != KActions.read.ToString())
                {
                    entry.assets = assets;
                }
                entry.subAction = subAction;
                entry.subActionId = subActionId;
                entry.ipAddress = ipAddress;
                entry.hasError = hasError;
                entry.date = DateTime.Now;
                Add(entry);
                Save();
            }
        }

        public void Add(KMSLog entry)
        {
            db.KMSLogs.InsertOnSubmit(entry);
        }

        public void Save()
        {
            db.SubmitChanges();
        }


    }
}