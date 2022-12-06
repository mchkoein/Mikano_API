using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using System.Web.Http.Cors;
using Microsoft.AspNet.WebApi.Extensions.Compression.Server;
using System.Net.Http.Extensions.Compression.Core.Compressors;
using System.Net.Http;
using System.Net.Http.Extensions.Compression.Client;

namespace Mikano_API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            // Configure Web API to use only bearer token authentication.
            //config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            var serverCompressionHandler = new ServerCompressionHandler(4096, new GZipCompressor(), new DeflateCompressor());
            var clientCompressionHandler = new ClientCompressionHandler(4096, new GZipCompressor(), new DeflateCompressor());

            GlobalConfiguration.Configuration.MessageHandlers.Insert(0, serverCompressionHandler);
            var client = new HttpClient(clientCompressionHandler);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}",
                defaults: new { }
            );
        }
    }
}
