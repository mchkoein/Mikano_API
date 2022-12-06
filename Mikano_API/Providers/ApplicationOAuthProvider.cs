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

namespace Mikano_API.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;

        public ApplicationOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            _publicClientId = publicClientId;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

            ApplicationUser user = await userManager.FindByNameAsync(context.UserName);
            if (user != null)
            {

                ApplicationUser validCredentials = await userManager.FindAsync(context.UserName, context.Password);

                // When a user is lockedout, this check is done to ensure that even if the credentials are valid
                // the user can not login until the lockout duration has passed
                if (await userManager.IsLockedOutAsync(user.Id))
                {
                    context.SetError("invalid_grant", string.Format("Your account has been locked out for {0} minutes due to multiple failed login attempts.", ConfigurationManager.AppSettings["DefaultAccountLockoutTimeSpan"].ToString()));
                    return;
                }
                // if user is subject to lockouts and the credentials are invalid
                // record the failure and check if user is lockedout and display message, otherwise,
                // display the number of attempts remaining before lockout
                else if (await userManager.GetLockoutEnabledAsync(user.Id) && validCredentials == null)
                {
                    // Record the failure which also may cause the user to be locked out
                    await userManager.AccessFailedAsync(user.Id);

                    string message;

                    if (await userManager.IsLockedOutAsync(user.Id))
                    {
                        message = string.Format("Your account has been locked out for {0} minutes due to multiple failed login attempts.", ConfigurationManager.AppSettings["DefaultAccountLockoutTimeSpan"].ToString());
                    }
                    else
                    {
                        int accessFailedCount = await userManager.GetAccessFailedCountAsync(user.Id);

                        int attemptsLeft =
                        Convert.ToInt32(
                        ConfigurationManager.AppSettings["MaxFailedAccessAttemptsBeforeLockout"].ToString()) -
                        accessFailedCount;

                        message = string.Format(
                        "Invalid credentials. You have {0} more attempt(s) before your account gets locked out.", attemptsLeft);

                    }
                    context.SetError("invalid_grant", message);
                    return;
                }
                else if (validCredentials == null)
                {
                    context.SetError("invalid_grant", "Invalid credentials. Please try again.");
                    return;
                }
                else
                {
                    // When token is verified correctly, clear the access failed count used for lockout
                    await userManager.ResetAccessFailedCountAsync(user.Id);
                }
            }
            else
            {
                // Allows cors for the /token endpoint this is different from webapi endpoints. 
                context.SetError("invalid_grant", "Invalid credentials. Please try again.");
                return;
            }



            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
               OAuthDefaults.AuthenticationType);
            ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
                CookieAuthenticationDefaults.AuthenticationType);

            AuthenticationProperties properties = CreateProperties(user.UserName);
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(properties, oAuthIdentity, cookiesIdentity);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            // Allows cors for the /token endpoint this is different from webapi endpoints. 
            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(string userName)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", userName }
            };
            return new AuthenticationProperties(data);
        }
    }
}