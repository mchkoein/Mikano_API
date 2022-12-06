using System.Web.Http;
using Mikano_API.Models;
using System.Linq;
using System.Net.Http;
using System.Net;
using System;
using Mikano_API;
using System.Web.Http.ModelBinding;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using static Mikano_API.Models.KMSEnums;
using System.Web;
using System.Net.Http.Formatting;
using System.Configuration;

namespace Mikano_API.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("api/metas")]
    public class MetasController : SharedController<SocketHub>
    {

        [HttpGet]
        public HttpResponseMessage GetPageTitle(int defaultPageId, int currentPageId, bool? isProduct = false, bool? isArticle = false)
        {

            CorporatePageRepository corporatePageRpstry = new CorporatePageRepository();

            try
            {

                CorporatePage defaultPage = corporatePageRpstry.GetById(defaultPageId);
                string currentPageTitle = null;

                currentPageTitle = corporatePageRpstry.GetById(currentPageId).customPageTitle;

                var pageTitle = string.IsNullOrEmpty(currentPageTitle) ? defaultPage.customPageTitle : currentPageTitle;

                return Request.CreateResponse(HttpStatusCode.OK, pageTitle);
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "");
            }
        }

    }
}