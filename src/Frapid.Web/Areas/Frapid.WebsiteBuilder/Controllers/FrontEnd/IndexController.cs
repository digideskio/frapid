﻿using System.Threading.Tasks;
using System.Web.Mvc;
using Frapid.ApplicationState.Cache;
using Frapid.Areas.Caching;
using Frapid.Areas.CSRF;
using Frapid.Configuration;
using Frapid.WebsiteBuilder.Models;
using Frapid.WebsiteBuilder.Plugins;
using Frapid.WebsiteBuilder.ViewModels;
using Npgsql;
using Serilog;

namespace Frapid.WebsiteBuilder.Controllers.FrontEnd
{
    [AntiForgery]
    public class IndexController: WebsiteBuilderController
    {
        [Route("hit")]
        [Route("site/{categoryAlias}/{alias}/hit")]
        [HttpPost]
        public async Task<ActionResult> CounterAsync(string categoryAlias = "", string alias = "")
        {
            await ContentModel.AddHitAsync(AppUsers.GetTenant(), categoryAlias, alias);
            return this.Ok();
        }

        [Route("")]
        [Route("site/{categoryAlias}/{alias}")]
        [FrapidOutputCache(ProfileName = "Content")]
        public async Task<ActionResult> IndexAsync(string categoryAlias = "", string alias = "", bool isPost = false, FormCollection form = null)
        {
            try
            {
                Log.Verbose($"Prepping \"{this.CurrentPageUrl}\".");


                var model = await this.GetContentsAsync(categoryAlias, alias, isPost, form);

                if(model == null)
                {
                    Log.Error($"Could not serve the url \"{this.CurrentPageUrl}\" because the model was null.");
                    return this.View(GetLayoutPath() + "404.cshtml");
                }

                Log.Verbose($"Parsing custom content extensions for \"{this.CurrentPageUrl}\".");
                model.Contents = await ContentExtensions.ParseHtmlAsync(this.Tenant, model.Contents);

                Log.Verbose($"Parsing custom form extensions for \"{this.CurrentPageUrl}\".");
                model.Contents = await FormsExtension.ParseHtmlAsync(model.Contents, isPost, form);

                model.Contents = HitHelper.Add(model.Contents);

                return this.View(this.GetRazorView<AreaRegistration>("Index/Index.cshtml"), model);
            }
            catch(NpgsqlException ex)
            {
                Log.Error
                    (
                     "An exception was encountered while trying to get content. More info:\nCategory alias: {categoryAlias}, alias: {alias}, is post: {isPost}, form: {form}. Exception\n{ex}.",
                     categoryAlias,
                     alias,
                     isPost,
                     form,
                     ex);
                return new HttpNotFoundResult();
            }
        }


        private async Task<Content> GetContentsAsync(string categoryAlias, string alias, bool isPost = false, FormCollection form = null)
        {
            string tenant = TenantConvention.GetTenant(this.CurrentDomain);
            var model = await ContentModel.GetContentAsync(tenant, categoryAlias, alias);

            if(model == null)
            {
                return null;
            }

            bool isHomepage = string.IsNullOrWhiteSpace(categoryAlias) && string.IsNullOrWhiteSpace(alias);

            string path = GetLayoutPath();
            string layout = isHomepage ? this.GetHomepageLayout() : this.GetLayout();


            model.LayoutPath = path;
            model.Layout = layout;

            return model;
        }

        [Route("site/{categoryAlias}/{alias}")]
        [HttpPost]
        public Task<ActionResult> PostAsync(string categoryAlias, string alias, FormCollection form)
        {
            Log.Verbose($"Got a post request on \"{this.CurrentPageUrl}\". Post Data:\n\n {form}");
            return this.IndexAsync(categoryAlias, alias, true, form);
        }

        [Route("")]
        [HttpPost]
        public Task<ActionResult> PostAsync(FormCollection form)
        {
            return this.PostAsync(string.Empty, string.Empty, form);
        }
    }
}