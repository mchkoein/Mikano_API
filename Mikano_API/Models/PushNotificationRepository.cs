using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Mikano_API.Models
{

    [MetadataType(typeof(PushNotificationValidation))]
    public partial class PushNotification
    {
    }
    public class PushNotificationValidation
    {
        [Required]
        public string title { get; set; }
    }
    public class PushNotificationRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();
        public IQueryable<PushNotification> GetAll()
        {
            return db.PushNotifications.Where(d => !d.isDeleted);
        }

        public PushNotification GetById(int id)
        {
            return GetAll().FirstOrDefault(d => d.id == id);
        }

        public void Add(PushNotification entry)
        {
            db.PushNotifications.InsertOnSubmit(entry);
        }

        public PushNotification GetNextEntry(PushNotification entry)
        {
            var nextEntry = GetAll().Where(d => d.id < entry.id).FirstOrDefault();

            if (nextEntry == null)
            {
                nextEntry = GetAll().Where(d => d.id != entry.id).FirstOrDefault();
            }
            if (nextEntry == null)
            {
                nextEntry = entry;
            }
            return nextEntry;
        }

        public void Save()
        {
            db.SubmitChanges();
        }
    }
}