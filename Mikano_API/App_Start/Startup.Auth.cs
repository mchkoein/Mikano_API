using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin;
using Mikano_API.Providers;
using Mikano_API.Models;
using Microsoft.Owin.Security.DataHandler.Encoder;
using System.Configuration;

using Microsoft.Owin.Security;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Jwt;
using System.Collections.Specialized;
using Mikano_API.Helpers;

namespace Mikano_API
{
    public partial class Startup
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        //internal ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();
        internal ProjectKeysModel projectConfigKeys = new ProjectKeysHelper().GetKeys();

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {

            NameValueCollection ProjectKeysConfiguration = (NameValueCollection)ConfigurationManager.GetSection("ProjectKeysConfig");

            // Configure the db context and user manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Configure the application for OAuth based flow
            PublicClientId = "self";
            // Enable the application to use bearer tokens to authenticate users
            //app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
                //TokenEndpointPath = new PathString("/Token"),
                //Provider = new ApplicationOAuthProvider(PublicClientId),
                //AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
                //AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
                //// In production mode set AllowInsecureHttp = false
                //// false measns HTTPS is allowed only
                //AllowInsecureHttp = true

                //For Dev enviroment only (on production should be AllowInsecureHttp = false)
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(1000),
                Provider = new CustomOAuthProvider(),
                AccessTokenFormat = new CustomJwtFormat(projectConfigKeys.apiUrl),
                //projectConfigKeys.apiUrl
                //ConfigurationManager.AppSettings["koeinUrl"]
                RefreshTokenProvider = new CustomRefreshTokenProvider()
            });


            var issuer = projectConfigKeys.apiUrl;// ConfigurationManager.AppSettings["koeinUrl"];// projectConfigKeys.apiUrl;
            var audienceId = ConfigurationManager.AppSettings["AudienceId"];
            var secret = TextEncodings.Base64Url.Decode(ConfigurationManager.AppSettings["AudienceSecret"]);

            // Api controllers with an [Authorize] attribute will be validated with JWT
            app.UseJwtBearerAuthentication(
                   new JwtBearerAuthenticationOptions
                   {
                       AuthenticationMode = AuthenticationMode.Active,
                       Provider = new CustomTokenAuthenticationProvider(),
                       AllowedAudiences = new[] { audienceId },
                       IssuerSecurityTokenProviders = new IIssuerSecurityTokenProvider[]
                       {
                    new SymmetricKeyIssuerSecurityTokenProvider(issuer, secret)
                       }
                   });



            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //    consumerKey: "",
            //    consumerSecret: "");

            //app.UseFacebookAuthentication(
            //    appId: "",
            //    appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }
    }
}
