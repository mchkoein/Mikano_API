using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;
using System.Web.Http;
using Mikano_API.Providers;
using Microsoft.AspNet.SignalR.Hubs;

[assembly: OwinStartup(typeof(Mikano_API.Startup))]
namespace Mikano_API
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
