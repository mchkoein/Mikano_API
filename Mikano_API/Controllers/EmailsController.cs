using System.Configuration;
using System.Net;
using System.Net.Security;
using System.Web.Mvc;
using Mikano_API.Helpers;
using Mikano_API.Models;
using static Mikano_API.Models.KMSEnums;
using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;

namespace Mikano_API.Controllers
{
    public class EmailsController : Controller
    {
        private AdministratorRepository accountRpstry = new AdministratorRepository();
        private ContactInfoRepository contactInfoRpstry = new ContactInfoRepository();
        //private ContactRepository contactRpstry = new ContactRepository();
        //private CareerApplicationRepository careerAppRpstry = new CareerApplicationRepository();
        private SocialMediaRepository socialmediasRpstry = new SocialMediaRepository();
        private CorporatePageRepository corporatePageRpstry = new CorporatePageRepository();
        private InquiryRepository inqRpstry = new InquiryRepository();

        internal ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

        //internal ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

        NameValueCollection ProjectKeysConfiguration = (NameValueCollection)ConfigurationManager.GetSection("ProjectKeysConfig");


        public ActionResult EmailWelcome(string id)
        {
            var model = accountRpstry.GetById(id);
            ViewData["fullName"] = model.firstName + " " + model.lastName;
            //ViewData["ContactInfo"] = contactInfoRpstry.GetFirst();
            ViewData["SocialMedias"] = socialmediasRpstry.GetAllIsPublished();
            return View(model);
        }

        public ActionResult EmailContact(int id)
        {
            var model = inqRpstry.GetById(id);
            //ViewData["ContactInfo"] = contactInfoRpstry.GetFirst();
            ViewData["SocialMedias"] = socialmediasRpstry.GetAllIsPublished();
            return View(model);
        }
        public ActionResult EmailProductInquiry(int id)
        {
            var model = inqRpstry.GetById(id);
            //ViewData["ContactInfo"] = contactInfoRpstry.GetFirst();
            ViewData["SocialMedias"] = socialmediasRpstry.GetAllIsPublished();
            return View(model);
        }
        public ActionResult EmailResetPassword(string id, string token)
        {
            ViewData["token"] = token;
            //ViewData["ContactInfo"] = contactInfoRpstry.GetFirst();
            ViewData["SocialMedias"] = socialmediasRpstry.GetAllIsPublished();
            return View(accountRpstry.GetById(id));
        }



    }


}