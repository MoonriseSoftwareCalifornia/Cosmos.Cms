using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using HtmlAgilityPack;
using Kendo.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Controllers
{
    /// <summary>
    /// Editor controller
    /// </summary>
    [Authorize(Roles = "Reviewers, Administrators, Editors, Authors, Team Members")]
    public class EditorController : BaseController
    {
        private readonly ArticleEditLogic _articleLogic;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<EditorController> _logger;
        private readonly IOptions<CosmosConfig> _options;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Uri _blobPublicAbsoluteUrl;
        private readonly IViewRenderService _viewRenderService;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext"></param>
        /// <param name="userManager"></param>
        /// <param name="articleLogic"></param>
        /// <param name="options"></param>
        /// <param name="syncContext"></param>
        /// <param name="viewRenderService"></param>
        public EditorController(ILogger<EditorController> logger,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            IOptions<CosmosConfig> options,
            SqlDbSyncContext syncContext,
            IViewRenderService viewRenderService
        ) :
             base(dbContext, userManager, articleLogic, options)
        {

            if (options.Value.SiteSettings.AllowSetup ?? true)
            {
                throw new Exception("Permission denied. Website in setup mode.");
            }
            if (syncContext.IsConfigured())
                dbContext.LoadSyncContext(syncContext);

            _logger = logger;
            //_blobEndpointUrl = options.Value.BlobServiceConfigs.BlobPublicUrl.TrimEnd('/') + "/pub/";
            _dbContext = dbContext;
            _options = options;
            _userManager = userManager;
            _articleLogic = articleLogic;

            var htmlUtilities = new HtmlUtilities();

            _blobPublicAbsoluteUrl = new Uri(htmlUtilities.IsAbsoluteUri(options.Value.SiteSettings.BlobPublicUrl) ? options.Value.SiteSettings.BlobPublicUrl : options.Value.SiteSettings.PublisherUrl);

            _viewRenderService = viewRenderService;
        }

        private TeamIdentityLogic GetTeamIdentityLogic()
        {
            return User.IsInRole("Team Members") ? new TeamIdentityLogic(_dbContext, User) : null;
        }

        /// <summary>
        ///     Disposes of resources for this controller.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        ///     Edit home page, shows list of pages.
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            ViewData["PublisherUrl"] = _options.Value.SiteSettings.PublisherUrl;
            ViewData["TeamLogic"] = GetTeamIdentityLogic();
            return View();
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Create()
        {
            var layout = await _dbContext.Layouts.FirstOrDefaultAsync(l => l.IsDefault);

            var templates = await _dbContext.Templates.Where(t => t.LayoutId == layout.Id).Select(s =>
                        new SelectListItem
                        {
                            Value = s.Id.ToString(),
                            Text = s.Title
                        }).ToListAsync();

            if (User.IsInRole("Team Members"))
            {
                var identityUser = await _userManager.GetUserAsync(User);

                var teams = await _dbContext
                    .Teams
                    .Where(a => a.Members.Any(x => x.UserId == identityUser.Id))
                    .ToListAsync();

                ViewData["Teams"] = teams.Select(c => new TeamViewModel
                {
                    Id = c.Id,
                    TeamDescription = c.TeamDescription,
                    TeamName = c.TeamName
                }).OrderBy(o => o.TeamName).ToList();

                return View(new CreatePageViewModel
                {
                    Id = 0,
                    Title = string.Empty,
                    TeamId = teams.FirstOrDefault()?.Id,
                    Templates = templates
                });
            }

            ViewData["Teams"] = null;
            return View(new CreatePageViewModel
            {
                Id = 0,
                Title = string.Empty,
                Templates = templates
            });
        }

        /// <summary>
        /// Gets template page information.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> GetTemplateInfo(int? Id)
        {
            if (Id == null)
                return Json("");

            var model = await _dbContext.Templates.FirstOrDefaultAsync(f => f.Id == Id.Value);

            return Json(model);
        }

        /// <summary>
        ///     Uses <see cref="ArticleEditLogic.Create(string, int?)" /> to create an <see cref="ArticleViewModel" /> that is
        ///     saved to
        ///     the database with <see cref="ArticleEditLogic.UpdateOrInsert" /> ready for editing.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        [HttpPost]
        public async Task<IActionResult> Create(CreatePageViewModel model)
        {
            if (model == null) return NotFound();

            if (User.IsInRole("Team Members") && model.TeamId == null)
            {
                ModelState.AddModelError("TeamId", "Choose a team for this page.");

                var identityUser = await _userManager.GetUserAsync(User);

                var teams = await _dbContext
                    .Teams
                    .Where(a => a.Members.Any(x => x.UserId == identityUser.Id))
                    .ToListAsync();

                ViewData["Teams"] = teams.Select(c => new TeamViewModel
                {
                    Id = c.Id,
                    TeamDescription = c.TeamDescription,
                    TeamName = c.TeamName
                }).ToList();
            }

            if (await _dbContext.Articles.AnyAsync(a =>
                a.StatusCode != 2
                && a.Title.Trim().ToLower() == model.Title.Trim().ToLower()))
                ModelState.AddModelError("Title", $"Title {model.Title} is already taken.");

            // Check for conflict with blob storage root path.
            var blobRootPath = "pub";

            if (!string.IsNullOrEmpty(blobRootPath))
                if (model.Title.ToLower() == blobRootPath.ToLower())
                    ModelState.AddModelError("Title",
                        $"Title {model.Title} conflicts with the file folder \"{blobRootPath}/\".");

            if (!ModelState.IsValid)
            {
                model.Templates = await _dbContext.Templates.Select(s =>
                    new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Title
                    }).ToListAsync();

                return View(model);
            }

            var article = await _articleLogic.Create(model.Title, model.TemplateId);
            var result =
                await _articleLogic.UpdateOrInsert(article, _userManager.GetUserId(User), model.TeamId);

            return RedirectToAction("Edit", new { result.Model.Id });
        }

        /// <summary>
        ///     Creates a new version for an article and redirects to editor.
        /// </summary>
        /// <param name="id">Article ID</param>
        /// <param name="entityId">Entity Id to use as new version</param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> CreateVersion(int id, int? entityId = null)
        {
            IQueryable<Article> query;

            //
            // Are we basing this on an existing entity?
            //
            if (entityId == null)
            {
                //
                // If here, we are not. Clone the new version from the last version.
                //
                // Find the last version here
                var maxVersion = await _dbContext.Articles.Where(a => a.ArticleNumber == id)
                    .MaxAsync(m => m.VersionNumber);

                //
                // Now find that version.
                //
                query = _dbContext.Articles.Where(f =>
                    f.ArticleNumber == id &&
                    f.VersionNumber == maxVersion);
            }
            else
            {
                //
                // We are here because the new versison is being based on a
                // specific older version, not the latest version.
                //
                //
                // Create a new version based on a specific version
                //
                query = _dbContext.Articles.Where(f =>
                    f.Id == entityId.Value);
            }

            var article = await query.FirstOrDefaultAsync();

            // var defaultLayout = await ArticleLogic.GetDefaultLayout("en-US");
            var model = new ArticleViewModel
            {
                Id = article.Id, // This is the article we are going to clone as a new version.
                StatusCode = StatusCodeEnum.Active,
                ArticleNumber = article.ArticleNumber,
                UrlPath = article.UrlPath,
                VersionNumber = 0,
                Published = null,
                Title = article.Title,
                Content = article.Content,
                Updated = DateTime.UtcNow,
                HeaderJavaScript = article.HeaderJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                ReadWriteMode = false,
                PreviewMode = false,
                EditModeOn = false,
                CacheKey = null,
                CacheDuration = 0
            };

            var userId = _userManager.GetUserId(User);

            var result = await _articleLogic.UpdateOrInsert(model, userId);

            return RedirectToAction("Edit", "Editor", new { result.Model.Id });
        }

        /// <summary>
        /// Create a duplicate page from a specified page.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Duplicate(int id)
        {
            var articleViewModel = await _articleLogic.Get(id, EnumControllerName.Edit);

            ViewData["Original"] = articleViewModel;

            if (articleViewModel == null)
            {
                return NotFound();
            }

            return View(new DuplicateViewModel()
            {
                Id = articleViewModel.Id,
                Published = articleViewModel.Published,
                Title = articleViewModel.Title
            });
        }

        /// <summary>
        /// Creates a duplicate page from a specified page and version.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Duplicate(DuplicateViewModel model)
        {
            string title = "";

            if (string.IsNullOrEmpty(model.ParentPageTitle))
            {
                title = model.Title;
            }
            else
            {
                title = $"{ model.ParentPageTitle.Trim('/')}/{ model.Title.Trim('/')} ";
            }

            if (_dbContext.Articles.Any(a => a.Title.ToLower() == title.ToLower()))
            {
                if (string.IsNullOrEmpty(model.ParentPageTitle))
                {
                    ModelState.AddModelError("Title", "Page title already taken.");
                }
                else
                {
                    ModelState.AddModelError("Title", "Sub-page title already taken.");
                }
            }



            var articleViewModel = await _articleLogic.Get(model.Id, EnumControllerName.Edit);

            if (ModelState.IsValid)
            {
                articleViewModel.ArticleNumber = 0;
                articleViewModel.Id = 0;
                articleViewModel.Published = model.Published;
                articleViewModel.Title = title;

                var identityUser = await _userManager.GetUserAsync(User);

                try
                {
                    var result = await _articleLogic.UpdateOrInsert(articleViewModel, identityUser.Id);

                    return RedirectToAction("Edit", new { Id = result.Model.Id });
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            ViewData["Original"] = articleViewModel;

            return View(model);
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(int id)
        {
            var page = await _articleLogic.Get(id, EnumControllerName.Edit);
            return View(new NewHomeViewModel
            {
                Id = page.Id,
                ArticleNumber = page.ArticleNumber,
                Title = page.Title,
                IsNewHomePage = false,
                UrlPath = page.UrlPath
            });
        }

        /// <summary>
        /// Make a web page the new home page
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(NewHomeViewModel model)
        {
            if (model == null) return NotFound();
            await _articleLogic.NewHomePage(model.Id, _userManager.GetUserId(User));

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Open trash
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public IActionResult Trash()
        {
            // TODO: Complete the trash bin UI
            return View();
        }

        /// <summary>
        ///     Publishes a website.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public IActionResult Publish()
        {
            return View();
        }

        /// <summary>
        /// Update date/time stamps on all published pages (articles).
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public override async Task<JsonResult> UpdateTimeStamps()
        {
            return await base.UpdateTimeStamps();
        }

        /// <summary>
        ///     Gets all the versions for an article
        /// </summary>
        /// <param name="id">Article number</param>
        /// <returns></returns>
        public async Task<IActionResult> Versions(int? id)
        {
            ViewData["EditModeOn"] = false;
            ViewData["MenuContent"] = _articleLogic.BuildMenu("en-US").Result;
            var article = await _dbContext.Articles.Where(a => a.ArticleNumber == id.Value)
                .Select(s => new { s.Title, s.Team }).FirstOrDefaultAsync();
            ViewData["ArticleTitle"] = article.Title;
            ViewData["TeamName"] = article.Team == null ? "" : article.Team.TeamName;

            if (id == null)
                return RedirectToAction("Index");

            ViewData["ArticleId"] = id.Value;

            ViewData["TeamLogic"] = GetTeamIdentityLogic();

            return View();
        }

        /// <summary>
        /// Open Cosmos CMS logs
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public IActionResult Logs()
        {
            return View();
        }

        /// <summary>
        /// Read logs
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Read_Logs([DataSourceRequest] DataSourceRequest request)
        {
            var data = await _dbContext.ArticleLogs
                .OrderByDescending(o => o.DateTimeStamp)
                .Include(i => i.IdentityUser)
                .Include(b => b.Article)
                .Select(s => new
                {
                    s.Id,
                    s.ActivityNotes,
                    s.DateTimeStamp,
                    s.Article.Title,
                    s.IdentityUser.Email
                }).ToListAsync();

            var result = await data.Select(s => new ArticleLogJsonModel
            {
                Id = s.Id,
                ActivityNotes = s.ActivityNotes,
                DateTimeStamp = s.DateTimeStamp.ToUniversalTime(),
                Title = s.Title,
                Email = s.Email
            }).ToDataSourceResultAsync(request);
            return Json(result);
        }



        #region SAVING CONTENT METHODS

        #endregion

        #region EDIT ARTICLE FUNCTIONS

        /// <summary>
        ///     Gets an article to edit by ID for the HTML (WYSIWYG) Editor.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                // Web browser may ask for favicon.ico, so if the ID is not a number, just skip the response.
                if (int.TryParse(id, out var pageId))
                {
                    ViewData["BlobEndpointUrl"] = _options.Value.SiteSettings.BlobPublicUrl;

                    //ViewData["Layouts"] = await GetLayoutListItems();
                    //
                    // Validate team member access.
                    //

                    ViewData["TeamLogic"] = GetTeamIdentityLogic();

                    return await BaseArticle_Get(pageId, EnumControllerName.Edit, true, true);
                }

                return NotFound();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                throw;
            }
        }


        /// <summary>
        /// Exports a layout with a blank page
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ExportPage(int? id)
        {
            ArticleViewModel article;
            if (id.HasValue)
            {
                article = await _articleLogic.Get(id.Value, EnumControllerName.Edit);
            }
            else
            {
                article = await _articleLogic.Create("Blank Page");
            }

            var htmlUtilities = new HtmlUtilities();

            article.Layout.Head = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.Head, _blobPublicAbsoluteUrl);
            article.Layout.HtmlHeader = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.HtmlHeader, _blobPublicAbsoluteUrl);
            article.Layout.FooterHtmlContent = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.FooterHtmlContent, _blobPublicAbsoluteUrl);

            article.HeaderJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeaderJavaScript, _blobPublicAbsoluteUrl);
            article.Content = htmlUtilities.RelativeToAbsoluteUrls(article.Content, _blobPublicAbsoluteUrl);
            article.FooterJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeaderJavaScript, _blobPublicAbsoluteUrl);

            var html = await _viewRenderService.RenderToStringAsync("~/Views/Editor/ExportPage.cshtml", article);

            var exportName = $"pageid-{article.ArticleNumber}-version-{article.VersionNumber}.html";

            var bytes = Encoding.UTF8.GetBytes(html);

            return File(bytes, "application/octet-stream", exportName);
        }

        /// <summary>
        ///     Saves an article via HTTP POST.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        ///     For published articles, flushes Redis and CDN
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> SaveHtml(ArticleViewModel model)
        {
            if (model == null) return NotFound();

            // If this person is in the team member role, check edit access.
            if (!await Ensure_TeamMemberEditAccess(model)) return Unauthorized();

            // Kendo editor uses BOM, so strip that out here just in case 
            // any find there way here.
            //
            // Strip Byte Order Marks (BOM)
            model.Content = StripBOM(model.Content);


            //
            // The HTML editor edits the title and Content fields.
            // Next two lines detect any HTML errors with each.
            // Errors are saved in ModelState.
            model.Title = BaseValidateHtml("Title", model.Title);
            model.Content = BaseValidateHtml("Content", model.Content);

            //
            // The WYSIWYG editor should only be allowed to edit content within the div tags
            // marked with the attribute contenteditable="true"
            //
            // First pull out the editable DIVs from the model just submitted.
            var modelHtmlDoc = new HtmlDocument();
            modelHtmlDoc.LoadHtml(model.Content);
            var modelEditableDivs = modelHtmlDoc.DocumentNode.SelectNodes("//*/div[@data-ccms-ceid]");


            // Next pull the original.
            var original = await _articleLogic.Get(model.Id, EnumControllerName.Edit);

            // No editable divs, then don't do anything.
            if (modelEditableDivs == null)
            {
                // Nothing should be edited because there are no editable DIVs.
                model.Content = original.Content;
            }
            else
            {
                var originalHtmlDoc = new HtmlDocument();
                originalHtmlDoc.LoadHtml(original.Content);
                var originalEditableDivs = originalHtmlDoc.DocumentNode.SelectNodes("//*/div[@data-ccms-ceid]");

                // The number of editable DIVs incoming should match the number in the original.
                if (modelEditableDivs.Count == originalEditableDivs.Count)
                {
                    foreach (var originaDiv in originalEditableDivs)
                    {
                        var id = originaDiv.Attributes["data-ccms-ceid"].Value;
                        var inputDiv = modelEditableDivs.FirstOrDefault(w => w.Attributes["data-ccms-ceid"].Value == id);
                        if (inputDiv != null)
                        {
                            originaDiv.InnerHtml = inputDiv.InnerHtml; // Modify the original
                        }
                        else
                        {
                            ModelState.AddModelError("Content", $"Could not match editable div '{id}' with original.");
                        }
                    }

                    // Now carry over what's beein updated to the original.
                    model.Content = originalHtmlDoc.DocumentNode.OuterHtml;
                }
                else
                {
                    ModelState.AddModelError("Content", "The number of editable sections in this page does not match the original. Cannot save.");
                }
            }


            // Make sure model state is valid
            if (ModelState.IsValid)
            {
                // Get the user's ID for logging.
                var user = await _userManager.GetUserAsync(User);

                //if (model.Published.HasValue)
                //{
                //    model.Content = MinifyHtml(model.Content);
                //    model.HeaderJavaScript = MinifyHtml(model.HeaderJavaScript);
                //    model.FooterJavaScript = MinifyHtml(model.FooterJavaScript);
                //}




                // START SAVE TO DATABASE ********
                //
                // Now save the changes to the database here.
                //
                var result = await _articleLogic.UpdateOrInsert(model, user.Id);
                //
                // END  SAVE TO  DATABASE ********


                model = result.Model;

                // Re-enable editable sections.
                //model.Content = model.Content.Replace("crx=", "contenteditable=",
                //    StringComparison.CurrentCultureIgnoreCase);

                //
                // Flush Redis and CDN if required 
                // New: Delay CDN 10 seconds to allow for local memory cache(s) to drain
                //
                if (result.Urls.Any() &&
                    (_options.Value?.CdnConfig.AzureCdnConfig.ClientId != null) || _options.Value?.CdnConfig.AkamaiContextConfig.AccessToken != null)
                {

                    // Now get all the languages that were flushed
                    // TODO: This needs to be improved.
                    // Not sure this is the best way to do this.
                    var paths = new List<string>();

                    paths.Add($"/{model.UrlPath}");
                    // Add the primary path
                    foreach (var url in result.Urls)
                    {
                        paths.Add($"/{url.TrimStart('/')}");
                    }

                    var json = await FlushCdn(paths.OrderBy(s => s).Distinct().ToArray());
                    var cdnResult = (CdnPurgeViewModel)json.Value;
                }

            }

            var errors = ModelState.Values
                .Where(w => w.ValidationState == ModelValidationState.Invalid)
                .ToList();

            var data = new SaveResultJsonModel
            {
                IsValid = ModelState.IsValid,
                ErrorCount = ModelState.ErrorCount,
                HasReachedMaxErrors = ModelState.HasReachedMaxErrors,
                ValidationState = ModelState.ValidationState,
                Model = model,
                Errors = errors
            };

            return Json(data);
        }

        /// <summary>
        ///     Ensures a team member has edit access to an article
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<bool> Ensure_TeamMemberEditAccess(ArticleViewModel model)
        {
            if (User.IsInRole("Team Members"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teamMember = await _dbContext.TeamMembers
                    .Where(t => t.UserId == user.Id &&
                                // ReSharper disable once AccessToModifiedClosure
                                t.Team.Articles.Any(a => a.Id == model.Id))
                    .FirstOrDefaultAsync();

                if (teamMember == null ||
                    model.Published.HasValue && teamMember.TeamRole != (int)TeamRoleEnum.Editor)
                    return false;
            }
            else
            {
                if (model.Published.HasValue && User.IsInRole("Authors"))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Flush CDN (if present).
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<JsonResult> FlushCdn(string[] paths = null)
        {
            return await FlushCdn(_logger, paths);
        }

        /// <summary>
        /// Edit web page code with Monaco editor.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> EditCode(int id)
        {
            var article = await _articleLogic.Get(id, EnumControllerName.Edit);
            if (article == null) return NotFound();

            // Validate security for authors before going further
            if (User.IsInRole("Team Members"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teamMember = await _dbContext.TeamMembers
                    .Where(t => t.UserId == user.Id &&
                                t.Team.Articles.Any(a => a.Id == id))
                    .FirstOrDefaultAsync();

                if (teamMember == null || article.Published.HasValue &&
                    teamMember.TeamRole != (int)TeamRoleEnum.Editor)
                    return Unauthorized();
            }
            else
            {
                if (article.Published.HasValue && User.IsInRole("Authors"))
                    return Unauthorized();
            }

            ViewData["Version"] = article.VersionNumber;

            ViewData["TeamLogic"] = GetTeamIdentityLogic();

            // Re-enable editable sections.
            //article.Content = article.Content.Replace("crx=", "contenteditable=",
            //    StringComparison.CurrentCultureIgnoreCase);

            return View(new EditCodePostModel
            {
                Id = article.Id,
                ArticleNumber = article.ArticleNumber,
                Title = article.Title,
                Published = article.Published,
                RoleList = article.RoleList,
                EditorTitle = article.Title,
                EditorFields = new[]
                {
                    new EditorField
                    {
                        FieldId = "HeaderJavaScript",
                        FieldName = "Header Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <head> tag."
                    },
                    new EditorField
                    {
                        FieldId = "Content",
                        FieldName = "Html Content",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear in the <body>."
                    },
                    new EditorField
                    {
                        FieldId = "FooterJavaScript",
                        FieldName = "Footer Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <body> tag."
                    }
                },
                HeaderJavaScript = article.HeaderJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                Content = article.Content,
                EditingField = "HeaderJavaScript",
                CustomButtons = new[] { "Preview", "Html", "Import" }
            });
        }

        /// <summary>
        ///     Saves the code and html of the page.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This method saves page code to the database.  <see cref="EditCodePostModel.Content" /> is validated using method
        ///     <see cref="BaseController.BaseValidateHtml" />.
        ///     HTML formatting errors that could not be automatically fixed are logged with
        ///     <see cref="ControllerBase.ModelState" /> and
        ///     the code is not saved in the database.
        /// </remarks>
        /// <exception cref="NotFoundResult"></exception>
        /// <exception cref="UnauthorizedResult"></exception>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> EditCode(EditCodePostModel model)
        {
            if (model == null) return NotFound();

            var articleViewModel = await _articleLogic.Get(model.Id, EnumControllerName.Edit);

            if (articleViewModel == null) return NotFound();

            // Validate security for authors before going further
            if (User.IsInRole("Team Members"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teamMember = await _dbContext.TeamMembers
                    .Where(t => t.UserId == user.Id &&
                                // ReSharper disable once AccessToModifiedClosure
                                t.Team.Articles.Any(a => a.Id == articleViewModel.Id))
                    .FirstOrDefaultAsync();

                if (teamMember == null || articleViewModel.Published.HasValue &&
                    teamMember.TeamRole != (int)TeamRoleEnum.Editor)
                    return Unauthorized();
            }
            else
            {
                if (articleViewModel.Published.HasValue && User.IsInRole("Authors"))
                    return Unauthorized();
            }

            // Strip Byte Order Marks (BOM)
            model.Content = StripBOM(model.Content);
            model.HeaderJavaScript = StripBOM(model.HeaderJavaScript);
            model.FooterJavaScript = StripBOM(model.FooterJavaScript);

            // Validate HTML
            model.Content = BaseValidateHtml("Content", model.Content);

            // Save title, published date/time, roles
            articleViewModel.Published = model.Published;
            articleViewModel.Title = BaseValidateHtml("Title", model.Title);
            articleViewModel.RoleList = model.RoleList;

            // Check for validation errors...
            if (ModelState.IsValid)
                try
                {
                    articleViewModel.Content = model.Content;

                    if (string.IsNullOrEmpty(model.HeaderJavaScript) ||
                    string.IsNullOrWhiteSpace(model.HeaderJavaScript))
                        articleViewModel.HeaderJavaScript = string.Empty;
                    else
                        articleViewModel.HeaderJavaScript = model.HeaderJavaScript.Trim();

                    if (string.IsNullOrEmpty(model.FooterJavaScript) ||
                        string.IsNullOrWhiteSpace(model.FooterJavaScript))
                        articleViewModel.FooterJavaScript = string.Empty;
                    else
                        articleViewModel.FooterJavaScript = model.FooterJavaScript.Trim();
                    // If no HTML errors were thrown, save here.

                    // Get the user's ID for logging.
                    var user = await _userManager.GetUserAsync(User);

                    //
                    // SAVE HERE!
                    //
                    var result = await _articleLogic.UpdateOrInsert(articleViewModel, user.Id);

                    //
                    // Pull back out of the database, so user can see exactly what was saved.
                    //
                    var article = await _dbContext.Articles.FirstOrDefaultAsync(f => f.Id == model.Id);
                    if (article == null) throw new Exception("Could not retrieve saved code!");


                }
                catch (Exception e)
                {
                    var provider = new EmptyModelMetadataProvider();
                    ModelState.AddModelError("Save", e, provider.GetMetadataForType(typeof(string)));
                }

            //// Now, prior to sending model back, re-enable the content editable attribute.
            //if (!string.IsNullOrEmpty(article.Content))
            //{
            //    article.Content = article.Content.Replace(" crx=\"", " contenteditable=\"",
            //        StringComparison.CurrentCultureIgnoreCase);
            //}

            // ReSharper disable once PossibleNullReferenceException
            ViewData["Version"] = articleViewModel.VersionNumber;

            var jsonModel = new SaveCodeResultJsonModel
            {
                ErrorCount = ModelState.ErrorCount,
                IsValid = ModelState.IsValid
            };
            jsonModel.Errors.AddRange(ModelState.Values
                .Where(w => w.ValidationState == ModelValidationState.Invalid)
                .ToList());
            jsonModel.ValidationState = ModelState.ValidationState;

            DateTime? publishedDateTime = null;
            if (articleViewModel.Published.HasValue)
            {
                publishedDateTime = articleViewModel.Published.Value.ToUniversalTime();
            }

            return Json(jsonModel);
        }

        /// <summary>
        /// Preload the website (useful if CDN configured).
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Administrators")]
        public IActionResult Preload()
        {
            return View(new PreloadViewModel());
        }

        /// <summary>
        /// Execute preload.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="primaryOnly"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators")]
        public async Task<IActionResult> Preload(PreloadViewModel model, bool primaryOnly = false)
        {
            var activeCode = (int)StatusCodeEnum.Active;
            var query = _dbContext.Articles.Where(p => p.Published != null && p.StatusCode == activeCode);
            var articleList = await _articleLogic.GetArticleList(query);
            var publicUrl = _options.Value.SiteSettings.PublisherUrl.TrimEnd('/');

            model.PageCount = 0;

            var client = new HttpClient();

            // Get a list of editors that are outside the current cloud.
            var otherEditors = _options.Value.EditorUrls.Where(w => w.CloudName.Equals(_options.Value.PrimaryCloud, StringComparison.CurrentCultureIgnoreCase) == false).ToList();

            model.EditorCount++;

            //
            // If we are preloading CDN
            if (model.PreloadCdn)
            {
                foreach (var article in articleList)
                {
                    try
                    {
                        var response = await client.GetAsync($"{publicUrl}/{(article.UrlPath == "root" ? "" : article.UrlPath)}");
                        response.EnsureSuccessStatusCode();
                        _ = await response.Content.ReadAsStringAsync();
                        //model.PageCount++;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message, e);
                    }
                }
            }


            return View(model);
        }

        #endregion

        #region Data Services

        /// <summary>
        /// Check to see if a page title is already taken.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CheckTitle(int articleNumber, string title)
        {
            var result = await _articleLogic.ValidateTitle(title, articleNumber);

            if (result) return Json(true);

            return Json($"Title '{title}' is already taken.");
        }

        /// <summary>
        /// Gets a list of articles (web pages)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetArticleList()
        {
            List<ArticleListItem> list;

            if (User.IsInRole("Team Members"))
            {
                var identityUser = await _userManager.GetUserAsync(User);
                ViewData["TeamsLookup"] = await _dbContext.Teams
                    .Where(a => a.Members.Any(x => x.UserId == identityUser.Id))
                    .Select(s => new TeamViewModel
                    {
                        Id = s.Id,
                        TeamDescription = s.TeamDescription,
                        TeamName = s.TeamName
                    }).OrderBy(o => o.TeamName)
                    .ToListAsync();

                var userId = await _userManager.GetUserIdAsync(await _userManager.GetUserAsync(User));

                var query = _dbContext.Articles
                    .Include(i => i.Team)
                    .Where(w => w.Team.Members.Any(a => a.UserId == userId));

                list = await _articleLogic.GetArticleList(query, true);
            }
            else
            {
                list = await _articleLogic.GetArticleList(query: null, true);

                ViewData["TeamsLookup"] = null;
            }
            return Json(list.OrderBy(o => o.Title).ToList());
        }

        /// <summary>
        ///     Get list of articles
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Note: This method cannot retrieve articles that are in the trash.
        /// </remarks>
        public async Task<IActionResult> Get_Articles([DataSourceRequest] DataSourceRequest request)
        {
            List<ArticleListItem> list;

            var defaultSort = request.Sorts?.Any() == false && request.Filters?.Any() == false;

            if (User.IsInRole("Team Members"))
            {
                var identityUser = await _userManager.GetUserAsync(User);
                ViewData["TeamsLookup"] = await _dbContext.Teams
                    .Where(a => a.Members.Any(x => x.UserId == identityUser.Id))
                    .Select(s => new TeamViewModel
                    {
                        Id = s.Id,
                        TeamDescription = s.TeamDescription,
                        TeamName = s.TeamName
                    }).OrderBy(o => o.TeamName)
                    .ToListAsync();

                var userId = await _userManager.GetUserIdAsync(await _userManager.GetUserAsync(User));

                var query = _dbContext.Articles
                    .Include(i => i.Team)
                    .Where(w => w.Team.Members.Any(a => a.UserId == userId));

                list = await _articleLogic.GetArticleList(query, defaultSort);
            }
            else
            {
                list = await _articleLogic.GetArticleList(query: null, defaultSort);

                ViewData["TeamsLookup"] = null;
            }

            if (defaultSort)
            {
                if (request.Sorts == null) request.Sorts = new List<SortDescriptor>();
                if (request.Page < 2)
                    request.Sorts.Add(new SortDescriptor
                    {
                        Member = "IsDefault",
                        SortDirection = ListSortDirection.Descending
                    });
                request.Sorts.Add(new SortDescriptor
                {
                    Member = "Title",
                    SortDirection = ListSortDirection.Ascending
                });
            }
            else if (request.Page < 2)
            {
                if (request.Sorts == null) request.Sorts = new List<SortDescriptor>();
                request.Sorts.Insert(0, new SortDescriptor
                {
                    Member = "IsDefault",
                    SortDirection = ListSortDirection.Descending
                });
            }

            return Json(await list.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Gets a list of articles (pages) on this website.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <remarks>Returns published and non-published links</remarks>
        public async Task<IActionResult> List_Articles(string text)
        {
            IQueryable<Article> query = _dbContext.Articles
            .OrderBy(o => o.Title)
            .Where(w => w.StatusCode == (int)StatusCodeEnum.Active || w.StatusCode == (int)StatusCodeEnum.Inactive);

            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(x => x.Title.ToLower().Contains(text.ToLower()));
            }

            var model = await query.Select(s => new
            {
                s.Title,
                s.UrlPath
            }).Distinct().Take(10).ToListAsync();

            return Json(model);
        }

        /// <summary>
        /// Sends an article (or page) to trash bin.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Trash_Article([DataSourceRequest] DataSourceRequest request,
            ArticleListItem model)
        {
            if (model != null) await _articleLogic.TrashArticle(model.ArticleNumber);
            return Json(await new[] { model }.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        ///     Get list of articles that are in the trash bin.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> Get_TrashedArticles([DataSourceRequest] DataSourceRequest request)
        {
            var list = await _articleLogic.GetArticleTrashList();
            return Json(await list.ToDataSourceResultAsync(request));
        }

        /// <summary>
        ///     Get all the versions of an article by article number.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Get_Versions([DataSourceRequest] DataSourceRequest request, int id)
        {
            var data = await _dbContext.Articles.OrderByDescending(o => o.VersionNumber)
                .Where(a => a.ArticleNumber == id).Select(s => new
                {
                    s.Id,
                    s.Published,
                    s.Title,
                    s.Updated,
                    s.VersionNumber,
                    s.Expires,
                    UsesHtmlEditor = s.Content.ToLower().Contains(" editable=") || s.Content.ToLower().Contains(" data-ccms-ceid=")
                }).ToListAsync();

            //
            // Testing has detected a rare occurance of a duplicate version number.
            // Compensating for this error, is to first detect duplicate numbers,
            // and if one is detected, recorder the version numbers.
            //
            var check = (from a in data
                         group a by a.VersionNumber
                into g
                         select new
                         {
                             Version = g.Key,
                             Count = g.Count()
                         }).ToList();

            if (check.Any(c => c.Count > 1))
            {
                //
                // Duplicate versions detected. Fix now.
                //
                var ids = data.Select(s => s.Id).ToArray();
                var entities = await _dbContext.Articles.Where(a => ids.Contains(a.Id)).OrderBy(o => o.Id)
                    .ToListAsync();
                // Reorder version numbers.
                for (var i = 0; i < entities.Count; i++) entities[i].VersionNumber = i + 1;
                await _dbContext.SaveChangesAsync();
                //
                // Reload the version list.
                //
                data = await _dbContext.Articles.OrderByDescending(o => o.VersionNumber)
                    .Where(a => a.ArticleNumber == id).Select(s => new
                    {
                        s.Id,
                        s.Published,
                        s.Title,
                        s.Updated,
                        s.VersionNumber,
                        s.Expires,
                        UsesHtmlEditor = s.Content.ToLower().Contains(" editable=") || s.Content.ToLower().Contains(" data-ccms-ceid=")
                    }).ToListAsync();
            }

            var model = data.Select(x =>
                new ArticleVersionInfo
                {
                    Id = x.Id,
                    VersionNumber = x.VersionNumber,
                    Title = x.Title,
                    Updated = x.Updated.ToUniversalTime(),
                    Published = x.Published?.ToUniversalTime(),
                    Expires = x.Expires?.ToUniversalTime(),
                    UsesHtmlEditor = x.UsesHtmlEditor
                });

            return Json(await model.ToDataSourceResultAsync(request));
        }

        /// <summary>
        ///     Gets a role list, and allows for filtering
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get_RoleList(string text)
        {
            var query = _dbContext.Roles.Select(s => new RoleItemViewModel
            {
                Id = s.Id,
                RoleName = s.Name,
                RoleNormalizedName = s.NormalizedName
            });

            if (!string.IsNullOrEmpty(text)) query = query.Where(w => w.RoleName.StartsWith(text));

            return Json(await query.OrderBy(r => r.RoleName).ToListAsync());
        }

        #region REDIRECT MANAGEMENT

        /// <summary>
        /// Redirect manager page
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public IActionResult Redirects()
        {
            return View();
        }

        /// <summary>
        /// Removes given redirects
        /// </summary>
        /// <param name="request"></param>
        /// <param name="redirects"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<ActionResult> Create_Redirects([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "models")] IEnumerable<RedirectItemViewModel> redirects)
        {
            if (redirects.Any())
            {
                foreach (var redirect in redirects)
                {
                    // Add redirect here
                    _dbContext.Articles.Add(new Article
                    {
                        Id = 0,
                        LayoutId = null,
                        ArticleNumber = 0,
                        StatusCode = (int)StatusCodeEnum.Redirect,
                        UrlPath = redirect.FromUrl, // Old URL
                        VersionNumber = 0,
                        Published = DateTime.Now.ToUniversalTime().AddDays(-1), // Make sure this sticks!
                        Title = "Redirect",
                        Content = redirect.ToUrl, // New URL
                        Updated = DateTime.Now.ToUniversalTime(),
                        HeaderJavaScript = null,
                        FooterJavaScript = null,
                        Layout = null,
                        ArticleLogs = null,
                        MenuItems = null
                    });
                }
                await _dbContext.SaveChangesAsync();
            }

            return Json(redirects.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Removes given redirects
        /// </summary>
        /// <param name="request"></param>
        /// <param name="redirects"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<ActionResult> Delete_Redirects([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "models")] IEnumerable<RedirectItemViewModel> redirects)
        {
            if (redirects.Any())
            {
                var ids = redirects.Select(s => s.Id).ToArray();
                _dbContext.Articles.RemoveRange(await _dbContext.Articles.Where(w => ids.Contains(w.Id)).ToListAsync());
                await _dbContext.SaveChangesAsync();
            }

            return Json(redirects.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Gets a list of redirects
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Read_Redirects([DataSourceRequest] DataSourceRequest request)
        {
            var list = await _articleLogic.GetArticleRedirects();
            return Json(await list.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Removes given redirects
        /// </summary>
        /// <param name="request"></param>
        /// <param name="redirects"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<ActionResult> Update_Redirects([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "models")] IEnumerable<RedirectItemViewModel> redirects)
        {
            if (redirects.Any())
            {
                foreach (var redirect in redirects)
                {
                    var article = await _dbContext.Articles.FindAsync(redirect.Id);
                    if (article != null)
                    {
                        article.UrlPath = redirect.FromUrl;
                        article.Content = redirect.ToUrl;
                    }
                }
                await _dbContext.SaveChangesAsync();
            }

            return Json(redirects.ToDataSourceResult(request, ModelState));
        }


        #endregion

        #endregion

        /// <summary>
        /// Recieves an encrypted signal from another editor to do something.
        /// </summary>
        /// <param name="data">Encrypted arguments</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public string Signal(string data)
        {
            var result = new SignalResult();

            try
            {
                var args = DecryptString(data).Split('|');

                switch (args[0])
                {
                    case "VERIFY":
                        result.JsonValue = JsonConvert.SerializeObject(new SignalVerifyResult { Echo = args[1], Stamp = DateTime.UtcNow });
                        break;
                    default:
                        throw new Exception($"Signal {args[0]} not supported.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                result.Exceptions.Add(e);
            }

            result.HasErrors = result.Exceptions.Any();

            var json = JsonConvert.SerializeObject(result);

            return EncryptString(json);
        }

    }
}