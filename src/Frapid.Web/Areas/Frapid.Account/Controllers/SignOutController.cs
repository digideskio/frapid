﻿using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using Frapid.Account.DAL;
using Frapid.Areas.Authorization;
using Frapid.Configuration;

namespace Frapid.Account.Controllers
{
    public class SignOutController : BaseAuthenticationController
    {
        [Route("account/sign-out")]
        [Route("account/log-out")]
        [RestrictAnonymous]
        public async Task<ActionResult> SignOutAsync()
        {
            if (this.AppUser != null)
            {
                await AccessTokens.RevokeAsync(this.Tenant, this.AppUser.ClientToken).ConfigureAwait(true);
            }

            FormsAuthentication.SignOut();
            return this.Redirect(this.GetReturnUrl());
        }

        private string GetReturnUrl()
        {
            string returnUrl = "/";
            var referrer = this.Request.UrlReferrer;
            if (referrer != null)
            {
                string domain = referrer.DnsSafeHost;
                bool wellKnown = TenantConvention.IsValidDomain(domain);

                if (wellKnown)
                {
                    returnUrl = referrer.ToString();
                }
            }
            return returnUrl;
        }
    }
}