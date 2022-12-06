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
    [MetadataType(typeof(NewsCommunicationValidation))]
    public partial class NewsCommunication
    {
        public string Images { get; set; }
        public string Videos { get; set; }
        public string Files { get; set; }

        public List<UploaderNewsCommunicationMediaModel> MediaFields { get; set; }
    }

    public class UploaderNewsCommunicationMediaModel
    {
        public string mediaSrc { get; set; }
        public string caption { get; set; }
        public string subCaption { get; set; }
        public string description { get; set; }
        public string link { get; set; }
        public string fieldName { get; set; }
        public string fileUploadId { get; set; }
    }

    public class NewsCommunicationValidation
    {
        [Required]
        public string title { get; set; }

    }
    #endregion

    #region Methods
    public class NewsCommunicationRepository : SharedRepository
    {
        private dblinqDataContext db = new dblinqDataContext();

        //public dblinqDataContext db = new dblinqDataContext();

        public IQueryable<NewsCommunication> GetAll()
        {
            return db.NewsCommunications.Where(d => !d.isDeleted && d.languageParentId == null).OrderByDescending(d => d.priority);
        }

        public IQueryable<NewsCommunication> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public NewsCommunication GetNextEntry(NewsCommunication entry)
        {
            var nextEntry = GetAll().Where(d => d.priority < entry.priority).FirstOrDefault();

            if (nextEntry == null)
            {
                nextEntry = GetAll().Where(d => d.priority != entry.priority).FirstOrDefault();
            }
            if (nextEntry == null)
            {
                nextEntry = entry;
            }
            return nextEntry;
        }

        public NewsCommunication GetById(int id)
        {
            return db.NewsCommunications.FirstOrDefault(d => !d.isDeleted && d.id == id);
        }

        public NewsCommunication GetFeatured()
        {
            return db.NewsCommunications.FirstOrDefault(d => !d.isDeleted && d.isFeatured);
        }
        public NewsCommunication GetFeatured1()
        {
            return db.NewsCommunications.FirstOrDefault(d => !d.isDeleted && d.isFeatured1);
        }
        public NewsCommunication GetFeatured2()
        {
            return db.NewsCommunications.FirstOrDefault(d => !d.isDeleted && d.isFeatured2);
        }
        //public NewsCommunication GetByUrlTitle(string title)
        //{
        //    return db.NewsCommunications.FirstOrDefault(d => !d.isDeleted && d.title.Replace("<br/>", "-").Replace("<br>", "-").Replace("!", "").Replace(":", "-").Replace("'", "").Replace(" ", "-").Replace("&", "").Replace("?", "").Replace("/", "-").Replace("*", "-").Replace(".", "").Replace("--", "-").ToLower() == title.ToLower());
        //}

        //public NewsCommunication GetByNativeTitle(string title)
        //{
        //    return db.NewsCommunications.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        //}

        public int GetMaxPriority()
        {
            return db.NewsCommunications.Any() ? db.NewsCommunications.Max(d => d.priority) : 0;
        }

        public void Add(NewsCommunication entry)
        {
            db.NewsCommunications.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.NewsCommunications.Where(d => !d.isDeleted);
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

        //public void Delete(int id)
        //{
        //    db.NewsCommunications.DeleteOnSubmit(db.NewsCommunications.FirstOrDefault(d=>d.id == id));
        //}

        public void Save()
        {
            db.SubmitChanges();
        }

        #region Navigation Through Entries
        public NavigationModel GetNavigationInformation(int id)
        {
            var navigation = new NavigationModel();

            var allEntries = GetAll().Select(d => d.id).ToList();
            var indexOfCurrentItem = allEntries.IndexOf(id);

            //Evaluate [canGoToFirst]
            if (indexOfCurrentItem != 0 && allEntries.FirstOrDefault() != id)
            {
                navigation.canGoToFirst = true;
            }
            //Evaluate [canGoToPrev]
            if (indexOfCurrentItem != 0)
            {
                navigation.canGoToPrev = true;
            }
            //Evaluate [canGoToNext]
            if (allEntries.Count != indexOfCurrentItem + 1)
            {
                navigation.canGoToNext = true;
            }
            //Evaluate [canGoToLast]
            if (allEntries.Count != indexOfCurrentItem + 1 && allEntries[indexOfCurrentItem + 1] != id)
            {
                navigation.canGoToLast = true;
            }

            return navigation;

        }

        public int GetTargetedEntry(int id, string navigation)
        {
            var allEntries = GetAll().Select(d => d.id).ToList();

            var indexOfCurrentItem = allEntries.IndexOf(id);

            int targetedEntryId = 0;

            switch (navigation)
            {
                case "first":
                    targetedEntryId = allEntries.FirstOrDefault();
                    break;

                case "prev":
                    targetedEntryId = indexOfCurrentItem == 0 ? -1 : allEntries[indexOfCurrentItem - 1];

                    if (targetedEntryId == -1)
                    {
                        targetedEntryId = id;
                    }

                    break;

                case "next":
                    targetedEntryId = allEntries.Count == indexOfCurrentItem + 1 ? -1 : allEntries[indexOfCurrentItem + 1];

                    if (targetedEntryId == -1)
                    {
                        targetedEntryId = id;
                    }

                    break;

                case "last":
                    targetedEntryId = allEntries.Count == indexOfCurrentItem + 1 ? -1 : allEntries[allEntries.Count - 1];

                    if (targetedEntryId == -1)
                    {
                        targetedEntryId = id;
                    }
                    break;

                default:
                    targetedEntryId = id;
                    break;
            }

            return targetedEntryId;
        }
        #endregion

        #region Related Media 
        public void DeleteRelatedMedias(NewsCommunication entry, int mediaType)
        {
            db.NewsCommunicationMedias.DeleteAllOnSubmit(db.NewsCommunicationMedias.Where(d => d.newsCommunicationId == entry.id && d.mediaType == mediaType));
        }
        #endregion

    }
    #endregion
}