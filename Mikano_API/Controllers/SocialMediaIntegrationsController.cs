using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Mikano_API.Models;
using Newtonsoft.Json;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/socialmediaintegrations")]
    public class SocialMediaIntegrationsController : SharedController<SocketHub>
    {
        // GET: SocialMediaIntegrations
        private SettingsRepository settingsRpstry = new SettingsRepository();

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetInstagramLatestFeed(int postsTake = 12)
        {

            InstagramModel entries = null;
            var settingsEntry = settingsRpstry.GetFirstOrDefault();
            try
            {
                using (var webClient = new WebClient())
                {
                    var results = webClient.DownloadString("https://api.instagram.com/v1/users/self/media/recent?scope=public_content&access_token=" + settingsEntry.instagramAccessToken);

                    entries = JsonConvert.DeserializeObject<InstagramModel>(results);

                    var entriesResults = entries == null ? null : entries.data.Where(d => d.type == "image").Take(postsTake);

                    return Request.CreateResponse(HttpStatusCode.OK, entriesResults);
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = e.Message });
            }

        }
    }
}