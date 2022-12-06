using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Mikano_API.Models;
using System.Configuration;
using Microsoft.IdentityModel.Tokens;
using Mikano_API.Controllers;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Providers
{
    public class CustomTokenAuthenticationProvider : OAuthBearerAuthenticationProvider
    {

        public override Task RequestToken(OAuthRequestTokenContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            // try to find bearer token in a cookie 
            // (by default OAuthBearerAuthenticationHandler 
            // only checks Authorization header)
            string tokenCookie = context.OwinContext.Request.Query.Get("Authorization");
            if (!string.IsNullOrEmpty(tokenCookie))
                context.Token = tokenCookie;
            return Task.FromResult<object>(null);
        }

        public override Task ValidateIdentity(OAuthValidateIdentityContext context)
        {
            if (context.IsValidated && !context.Request.Path.ToString().ToLower().Contains("logout"))
            {
                var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

                var kSecurityStamp = context.Ticket.Identity.Claims.FirstOrDefault(d => d.Type == "kSecurityStamp");

                AdministratorRepository rpstry = new AdministratorRepository();
                var securityUser = rpstry.GetByUserNameGlobal(context.Ticket.Identity.Name);

                if (securityUser == null || string.IsNullOrEmpty(securityUser.KSecurityStamp) || kSecurityStamp == null || kSecurityStamp.Value != securityUser.KSecurityStamp)
                {
                    throw new SecurityTokenException("Invalid token");
                }
                if (securityUser.aspNetUsersActivationId != (int)UserActivation.Active)
                {
                    throw new SecurityTokenException("Invalid token");
                }
            }
            base.ValidateIdentity(context);
            return Task.FromResult<object>(null);
        }
    }
}