﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Mikano_API.Helpers;
using System.Configuration;

namespace Mikano_API.Models
{
    #region Models
    [MetadataType(typeof(DirectorValidation))]
    public partial class Director
    {

    }

    public class DirectorValidation
    {

    }
    #endregion

    #region Methods
    public class DirectorRepository : SharedRepository
    {
        public dblinqDataContext db = new dblinqDataContext();

        public IQueryable<Director> GetAll()
        {
            return db.Directors.Where(d => !d.isDeleted && d.languageParentId == null).OrderByDescending(d => d.priority);
        }

        public IQueryable<Director> GetAllIsPublished()
        {
            return GetAll().Where(d => d.isPublished);
        }

        public Director GetNextEntry(Director entry)
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

        public Director GetById(int id)
        {
            return db.Directors.FirstOrDefault(d => !d.isDeleted && d.id == id);
        }

        //public Director GetByUrlTitle(string title)
        //{
        //    return db.Directors.FirstOrDefault(d => !d.isDeleted && d.title.Replace("<br/>", "-").Replace("<br>", "-").Replace("!", "").Replace(":", "-").Replace("'", "").Replace(" ", "-").Replace("&", "").Replace("?", "").Replace("/", "-").Replace("*", "-").Replace(".", "").Replace("--", "-").ToLower() == title.ToLower());
        //}

        //public Director GetByNativeTitle(string title)
        //{
        //    return db.Directors.FirstOrDefault(d => !d.isDeleted && d.title.ToLower() == title.ToLower());
        //}

        public int GetMaxPriority()
        {
            return db.Directors.Any() ? db.Directors.Max(d => d.priority) : 0;
        }

        public void Add(Director entry)
        {
            db.Directors.InsertOnSubmit(entry);
        }

        public string SortGrid(int newIndex, int oldIndex, int id)
        {
            var entry = GetById(id);
            var allData = db.Directors.Where(d => !d.isDeleted);
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
        //    db.Directors.DeleteOnSubmit(db.Directors.FirstOrDefault(d=>d.id == id));
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

    }
    #endregion
}