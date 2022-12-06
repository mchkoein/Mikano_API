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
using System.Text;
using Mikano_API.Controllers;
using static Mikano_API.Models.KMSEnums;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Owin;
using Facebook;
using System.Web.Http.Cors;
using System.Collections.Specialized;
using System.Net;
using Mikano_API.Helpers;

namespace Mikano_API.Providers
{
    [EnableCors("*", "*", "*")]
    public class CustomOAuthProvider : OAuthAuthorizationServerProvider
    {
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

            ApplicationUser user = await userManager.FindByNameAsync(context.UserName);
            AdministratorRepository rpstry = new AdministratorRepository();
            if (user != null)
            {
                var originalUser = rpstry.GetByIdGlobal(user.Id);
                if (originalUser.LockoutEndDateUtc != null && originalUser.LockoutEndDateUtc.Value > DateTime.Now)
                {
                    context.SetError("Invalid User", "Invalid credentials. Please try again.");
                    return;
                }

                ApplicationUser validCredentials = null;
                if (context.Request.Headers.Any(d => d.Key == "fbaccessToken"))
                {
                    string fbaccessToken = context.Request.Headers.Any(d => d.Key == "fbaccessToken") && context.Request.Headers.FirstOrDefault(d => d.Key == "fbaccessToken").Value.FirstOrDefault() != null && context.Request.Headers.FirstOrDefault(d => d.Key == "fbaccessToken").Value.FirstOrDefault() != "null" ? context.Request.Headers.FirstOrDefault(d => d.Key == "fbaccessToken").Value.FirstOrDefault() : null;
                    ExternalCustomLoginModel externalUserObj = new MembershipHelper().FacebookExternalLogin(fbaccessToken);
                    if (externalUserObj != null && (externalUserObj.email.ToLower() == context.UserName.ToLower() || originalUser.providerUserId == externalUserObj.id))
                    {
                        validCredentials = await userManager.FindByEmailAsync(originalUser.UserName);
                    }
                }
                else if (context.Request.Headers.Any(d => d.Key == "googleaccessToken"))
                {
                    string googleaccessToken = context.Request.Headers.Any(d => d.Key == "googleaccessToken") && context.Request.Headers.FirstOrDefault(d => d.Key == "googleaccessToken").Value.FirstOrDefault() != null && context.Request.Headers.FirstOrDefault(d => d.Key == "googleaccessToken").Value.FirstOrDefault() != "null" ? context.Request.Headers.FirstOrDefault(d => d.Key == "googleaccessToken").Value.FirstOrDefault() : null;

                    GoogleAccountModel externalUserObj = new MembershipHelper().GoogleExternalLogin(googleaccessToken);

                    if (externalUserObj != null && (externalUserObj.email.ToLower() == context.UserName.ToLower() || originalUser.googleAccessToken == externalUserObj.sub))
                    {
                        validCredentials = await userManager.FindByEmailAsync(originalUser.UserName);
                    }
                }
                else
                {
                    validCredentials = await userManager.FindAsync(context.UserName, context.Password);
                }
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



            #region generate random KSecurityStampfor only for first login
            var securityUser = rpstry.GetByIdGlobal(user.Id);

            if (string.IsNullOrEmpty(securityUser.KSecurityStamp))
            {
                securityUser.KSecurityStamp = Guid.NewGuid().ToString();
                securityUser.lastLoginDate = DateTime.Now;
                rpstry.Save();
            }
            #endregion


            var oAuthIdentity = await user.GenerateUserIdentityAsync(userManager, "JWT");
            oAuthIdentity.AddClaim(new Claim("kSecurityStamp", securityUser.KSecurityStamp));
            var ticket = new AuthenticationTicket(oAuthIdentity, null);

            KMSLogRepository logRpstry = new KMSLogRepository();
            logRpstry.AddLog(user.Id, user.UserName, "login", "account", "account", "account", CollectRequestData(context.Request, null), GetIpAddress(context.Request), false);
            context.Validated(ticket);
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



        internal string CollectRequestData(IOwinRequest request, object postData)
        {
            string result = "{";
            if (request.Headers != null && request.Headers.Any())
            {
                result += "Headers:" + JsonConvert.SerializeObject(request.Headers) + ",";
            }
            if (request.Query != null && request.Query.Any())
            {
                result += "Query:" + JsonConvert.SerializeObject(request.Query) + ",";
            }
            if (postData != null)
            {
                var objectType = postData.GetType();
                var properties = objectType.GetProperties().Where(d => d.PropertyType.IsSerializable);
                Dictionary<string, object> newData = new Dictionary<string, object>();
                foreach (var prop in properties)
                {
                    newData.Add(prop.Name, prop.GetValue(postData, null));
                }

                result += "Post Data:" + JsonConvert.SerializeObject(newData) + ",";
            }


            if (request.Uri != null)
            {
                result += "Uri:" + JsonConvert.SerializeObject(request.Uri) + ",";
            }

            return result + "}";
        }

        internal string GetIpAddress(IOwinRequest request)
        {
            return request.RemoteIpAddress;
        }



        internal string CreatePassword(int length, bool isOnlyNumeric = false)
        {
            string valid = isOnlyNumeric ? "1234567890" : "abcdefghijklmnopqrst1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

    }
}