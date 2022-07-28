using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Website.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Website.Controllers
{
    public class HomeController : Controller
    {

        private readonly IOptions<CosmosConfig> _cosmosOptions;
        private readonly ILogger _logger;
        private readonly IDistributedCache _distributedCache;
        private readonly SqlConnectionStringBuilder _connectionStringBuilder;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ApplicationDbContext dbContext, ILogger<HomeController> logger,
            IOptions<CosmosConfig> cosmosOptions,
            IDistributedCache distributedCache)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cosmosOptions = cosmosOptions;
            _distributedCache = distributedCache;

            var primaryCloud = cosmosOptions.Value.PrimaryCloud;
            var primary = cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.CloudName.Equals(primaryCloud, StringComparison.CurrentCultureIgnoreCase));

            _connectionStringBuilder = new SqlConnectionStringBuilder(primary.ToString());
            var secondary = cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.CloudName.Equals(primaryCloud, StringComparison.CurrentCultureIgnoreCase) == false);

            if (secondary != null)
            {
                _connectionStringBuilder.FailoverPartner = secondary.Hostname;
            }
        }

        /// <summary>
        ///     Index method of the home controller, main entry point for web pages.
        /// </summary>
        /// <remarks>Cosmos handles routes, not normal MVC routing.</remarks>
        /// <returns></returns>
        public async Task<IActionResult> Index(string id)
        {
            // We do this so Cosmos can handle heirarchical page paths
            HttpContext.Request.Query.TryGetValue("lang", out var lang);
            return await GetArticleViewModelAsync(HttpContext.Request.Path, lang, false, true);
        }

        /// <summary>
        /// Gets the children of a given page path.
        /// </summary>
        /// <param name="id">UrlPath</param>
        /// <returns></returns>
        [EnableCors("AllCors")]
        public async Task<IActionResult> GetTOC(string id, bool orderByPub = false, int pageSize = 10, int pageNo = 0)
        {
            var articleLogic = new ArticleLogic(_dbContext, _cosmosOptions);
            var result = await articleLogic.GetTOC(id, pageNo, pageSize, orderByPub);
            return Json(result);
        }

        /// <summary>
        /// Gets the page as a JSON result
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Json([FromQuery] string id, string lang = "en")
        {
            return await GetArticleViewModelAsync(id, lang, true, false);
        }

        /// <summary>
        /// Internal method used to produce either a ViewResult or JsonResult
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lang"></param>
        /// <param name="returnJson"></param>
        /// <param name="includeLayout"></param>
        /// <returns></returns>
        internal async Task<IActionResult> GetArticleViewModelAsync(string urlPath, string lang = "en", bool returnJson = false,
            bool includeLayout = false)
        {

            // Make sure this is UrlEncoded, because this is the way it is stored in DB.
            if (!string.IsNullOrEmpty(urlPath)) urlPath = ArticleLogic.HandleUrlEncodeTitle(urlPath);


            urlPath = urlPath?.Trim().ToLower();
            if (string.IsNullOrEmpty(urlPath) || urlPath.Trim() == "/")
                urlPath = "root";

            var articleLogic = new ArticleLogic(_dbContext, _cosmosOptions, false);

            var articleViewModel = await articleLogic.GetByUrl(urlPath, lang);

            if (articleViewModel == null)
            {
                //
                // Create your own not found page for a graceful page for users.
                //
                articleViewModel = await articleLogic.GetByUrl("not_found", lang);

                HttpContext.Response.StatusCode = 404;

            }

            if (articleViewModel == null) return NotFound();

            if (returnJson)
            {
                if (!includeLayout) articleViewModel.Layout = null;
                return Json(articleViewModel);
            }

            return GetActionResult(articleViewModel, new string[] { });

        }

        private IActionResult GetActionResult(ArticleViewModel model, string[] headerDiagnostics)
        {

            if (model.StatusCode == StatusCodeEnum.Redirect) return Redirect(model.Content);

            SetResponseCachingHeaders(model.Updated.ToUniversalTime(), model.CacheDuration, model.Expires);

            // Determine if Google Translate v3 is configured so the javascript support will be added
            ViewData["UseGoogleTranslate"] =
                string.IsNullOrEmpty(_cosmosOptions?.Value?.GoogleCloudAuthConfig?.ClientId) == false;

            return View(model);
        }

        public virtual IActionResult CheckHealth()
        {
            return base.Ok();
        }

        /// <summary>
        ///     Gets a list of languages supported for translation
        /// </summary>
        /// <param name="lang">language code</param>
        /// <returns></returns>
        public virtual async Task<JsonResult> GetSupportedLanguages(string lang = "en-US")
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseSqlServer(_connectionStringBuilder.ConnectionString);
            using var dbContext = new ApplicationDbContext(builder.Options);

            var articleLogic = new ArticleLogic(dbContext, _cosmosOptions);

            SetResponseCachingHeaders(DateTime.UtcNow, 60);

            var result = await articleLogic.GetSupportedLanguages(lang);

            return Json(result.Languages.Select(s => new LangItemViewModel
            {
                DisplayName = s.DisplayName,
                LanguageCode = s.LanguageCode
            }).ToList());
        }

        /// <summary>
        ///     Set response object headers
        /// </summary>
        /// <param name="lastModifiedUtc">When the cache object was last modified in UTC</param>
        /// <param name="cacheDurationSeconds">How many seconds until cache expires.</param>
        private void SetResponseCachingHeaders(DateTime lastModifiedUtc, int cacheDurationSeconds,
            DateTime? expires = null)
        {
            // All CDNs look to the Cache-Control header.
            // See this article and video from Akamai:
            // https://developer.akamai.com/blog/2017/03/28/what-you-need-know-about-caching-part-1
            // And this from Microsoft:
            //https://docs.microsoft.com/en-us/azure/cdn/cdn-manage-expiration-of-cloud-service-content#setting-cache-control-headers-programmatically
            if (expires.HasValue)
            {
                var expiresUtc = expires.Value.ToUniversalTime();
                var nowUtc = DateTime.UtcNow;

                if (nowUtc.CompareTo(expiresUtc) < 0)
                {
                    cacheDurationSeconds = Math.Min(Convert.ToInt32(expiresUtc.Subtract(nowUtc).TotalSeconds), cacheDurationSeconds);
                }

                Response.Headers[HeaderNames.Expires] = expiresUtc.ToString("R");
                Response.Headers[HeaderNames.CacheControl] = $"public,max-age={cacheDurationSeconds}";
            }
            else
            {
                Response.Headers[HeaderNames.CacheControl] = $"public,max-age={cacheDurationSeconds}";
            }

            // Azure CDN Standard/Premium from Verizon supports ETag by default, while
            // Azure CDN Standard from Microsoft and Azure CDN Standard from Akamai do not.
            Response.Headers[HeaderNames.ETag] = lastModifiedUtc.Ticks.ToString();

            // Akamai, Microsoft and Verizon CDN all support watching last modified for changes.
            Response.Headers[HeaderNames.LastModified] = lastModifiedUtc.ToUniversalTime().ToString("R");

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Error/{statusCode}")]
        public IActionResult HandleErrorCode(int statusCode)
        {
            var statusCodeData = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry the page you requested could not be found";
                    ViewBag.RouteOfException = statusCodeData.OriginalPath;
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Sorry something went wrong on the server";
                    ViewBag.RouteOfException = statusCodeData.OriginalPath;
                    break;
            }

            return View("Error");
        }
    }
}