using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Controllers;
using CDT.Cosmos.Cms.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Z.EntityFramework.Plus;

namespace CDT.Cosmos.Cms.Data.Logic
{
    /// <summary>
    ///     Article Editor Logic
    /// </summary>
    /// <remarks>
    ///     Is derived from base class <see cref="ArticleLogic" />, adds on content editing functionality.
    /// </remarks>
    public class ArticleEditLogic : ArticleLogic
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="config"></param>
        /// <param name="syncContext"></param>
        public ArticleEditLogic(ApplicationDbContext dbContext,
            IOptions<CosmosConfig> config,
            SqlDbSyncContext syncContext) :
            base(dbContext,
                config, true)
        {
            if (syncContext.IsConfigured())
                DbContext.LoadSyncContext(syncContext);
        }

        /// <summary>
        ///     Database Context with Synchronize Context
        /// </summary>
        public new ApplicationDbContext DbContext => base.DbContext;

        /// <summary>
        ///     Determine if this service is configured
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsConfigured()
        {
            return await DbContext.IsConfigured();
        }

        #region VALLIDATION

        /// <summary>
        ///     Validate that the title is not already taken by another article.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        public async Task<bool> ValidateTitle(string title, int articleNumber)
        {
            if (title.ToLower() == "pub") return false;
            var article = await DbContext.Articles.FirstOrDefaultAsync(a =>
                    a.Title.ToLower() == title.Trim().ToLower() && // Is the title used already
                    a.StatusCode != (int)StatusCodeEnum.Deleted // and the page is active (active or is inactive)
            );

            if (article == null) return true;

            return article.ArticleNumber == articleNumber;
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        ///     Gets a template represented as an <see cref="ArticleViewModel" />.
        /// </summary>
        /// <param name="template"></param>
        /// <returns>ArticleViewModel</returns>
        private ArticleViewModel BuildTemplateViewModel(Template template)
        {
            return new()
            {
                Id = template.Id,
                ArticleNumber = template.Id,
                UrlPath = HttpUtility.UrlEncode(template.Title.Trim().Replace(" ", "_")),
                VersionNumber = 1,
                Published = DateTime.Now.ToUniversalTime(),
                Title = template.Title,
                Content = template.Content,
                Updated = DateTime.Now.ToUniversalTime(),
                HeaderJavaScript = string.Empty,
                FooterJavaScript = string.Empty,
                ReadWriteMode = true
            };
        }

        private async Task<int> GetNextVersionNumber(int articleNumber)
        {
            return await DbContext.Articles.Where(a => a.ArticleNumber == articleNumber)
                .MaxAsync(m => m.VersionNumber) + 1;
        }

        private async Task<int> GetNextArticleNumber()
        {
            if (await DbContext.Articles.AnyAsync())
                return await DbContext.Articles.MaxAsync(m => m.ArticleNumber) + 1;

            return 1;
        }

        private void HandleLogEntry(Article article, string note, string userId)
        {
            article.ArticleLogs ??= new List<ArticleLog>();
            article.ArticleLogs.Add(new ArticleLog
            {
                ArticleId = article.Id,
                IdentityUserId = userId,
                ActivityNotes = note,
                DateTimeStamp = DateTime.Now.ToUniversalTime()
            });
        }

        /// <summary>
        /// Make sure all content editble DIVs have a unique C/CMS ID (attribute 'data-ccms-ceid').
        /// </summary>
        /// <param name="content"></param>
        /// <returns>string</returns>
        /// <remarks>
        /// <para>
        /// The WYSIWYG editor is designed to only edit portions of an article content that are marked 
        /// with the attribute "contenteditable='true'".
        /// </para>
        /// <para>
        /// When an article is saved by the WYSIWYG editor only those portions within the DIV tags
        /// marked editable are saved.
        /// </para>
        /// <para>
        /// This allows editing of a web page with dynamic client-side functionality (JavaScript)
        /// like a map, chart, graph, etc. to be uneditable on a page while the text around it is.
        /// </para>
        /// </remarks>
        private string Ensure_ContentEditable_IsMarked(string content)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrWhiteSpace(content))
            {
                return content;
            }
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            var elements = htmlDoc.DocumentNode.SelectNodes("//*[@contenteditable]|//*[@crx]|//*[@data-ccms-ceid]");

            if (elements == null)
            {
                return content;
            }

            var count = 0;

            foreach (var element in elements)
            {
                if (!element.Attributes.Contains("data-ccms-ceid"))
                {
                    element.Attributes.Add("data-ccms-ceid", Guid.NewGuid().ToString());
                    count++;
                }
                else
                {
                    if (string.IsNullOrEmpty(element.Attributes["data-ccms-ceid"].Value))
                    {
                        element.Attributes["data-ccms-ceid"].Value = Guid.NewGuid().ToString();
                    }
                }

                if (element.Attributes.Contains("contenteditable"))
                {
                    element.Attributes.Remove("contenteditable");
                }
                if (element.Attributes.Contains("crx"))
                {
                    element.Attributes.Remove("crx");
                }

            }

            // If we had to add at least one ID, then re-save the article.
            return htmlDoc.DocumentNode.OuterHtml;
        }

        private async Task<List<ArticleListItem>> PrivateGetArticleList(IQueryable<Article> query)
        {
            var data = await
                (from x in query
                 where x.StatusCode != (int)StatusCodeEnum.Deleted && x.Title.ToLower() != "redirect"
                 group x by x.ArticleNumber
                    into g
                 select new
                 {
                     ArticleNumber = g.Key,
                     Published = g.Max(i => i.Published)
                 }).ToListAsync();

            var model = new List<ArticleListItem>();

            foreach (var item in data)
            {
                Article article;

                if (item.Published.HasValue)
                    // If published, get the last published article, search by article number and published date and time.
                    article = await DbContext.Articles.Include(i => i.Team).Where(
                            f => f.ArticleNumber == item.ArticleNumber && f.Published.HasValue).OrderBy(o => o.Id)
                        .LastOrDefaultAsync();
                else
                    // If not published, get the last entity ID for the article.
                    article = await DbContext.Articles.Include(i => i.Team).Where(
                        f => f.ArticleNumber == item.ArticleNumber).OrderBy(o => o.Id).LastOrDefaultAsync();

                var entity = new ArticleListItem
                {
                    ArticleNumber = article.ArticleNumber,
                    Id = article.Id,
                    IsDefault = article.UrlPath == "root",
                    LastPublished = article.Published.HasValue ? article.Published.Value : null,
                    Title = article.Title,
                    Updated = article.Updated,
                    VersionNumber = article.VersionNumber,
                    Status = article.StatusCode == 0 ? "Active" : "Inactive",
                    UrlPath = article.UrlPath,
                    TeamName = article.Team == null ? "" : article.Team.TeamName
                };
                model.Add(entity);
            }

            return model.ToList();
        }

        private async Task ResetVersionExpirations(int articleNumber)
        {
            var list = await DbContext.Articles.Where(a => a.ArticleNumber == articleNumber).ToListAsync();

            foreach (var item in list)
                if (item.Expires.HasValue)
                    item.Expires = null;

            var published = list.Where(a => a.ArticleNumber == articleNumber && a.Published.HasValue)
                .OrderBy(o => o.VersionNumber).TakeLast(2).ToList();

            if (published.Count == 2) published[0].Expires = published[1].Published;

            await DbContext.SaveChangesAsync();
        }

        #endregion

        #region CREATE METHODS

        /// <summary>
        ///     Creates a new article, does NOT save it to the database before returning a copy for editing.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="templateId"></param>
        /// <returns>Unsaved article ready to edit and save</returns>
        /// <remarks>
        ///     <para>
        ///         Creates a new article, unsaved, ready to edit.  Uses <see cref="ArticleLogic.GetDefaultLayout" /> to get the
        ///         layout,
        ///         and builds the <see cref="ArticleViewModel" /> using method
        ///         <seealso cref="ArticleLogic.BuildArticleViewModel" />.
        ///     </para>
        ///     <para>
        ///         If a template ID is given, the contents of this article is loaded with content from the <see cref="Template" />
        ///         .
        ///     </para>
        /// </remarks>
        public async Task<ArticleViewModel> Create(string title, int? templateId = null)
        {
            //var layout = await GetDefaultLayout(false);
            var layout = await DbContext.Layouts.FirstOrDefaultAsync(f => f.IsDefault);
            if (layout != null)
                DbContext.Entry(layout).State = EntityState.Detached; // Prevents layout from being updated.

            var defaultTemplate = string.Empty;

            if (templateId.HasValue)
            {
                var template = await DbContext.Templates.FindAsync(templateId.Value);

                defaultTemplate = template?.Content;
            }

            if (string.IsNullOrEmpty(defaultTemplate))
                defaultTemplate = "<div class=\"container m-y-lg\">" +
                                  "<main class=\"main-primary\">" +
                                  "<div class=\"row\">" +
                                  "<div class=\"col-md-12\"><h1>Why Lorem Ipsum</h1><p>" +
                                  LoremIpsum.WhyLoremIpsum + "</p></div>" +
                                  "</div>" +
                                  "<div class=\"row\">" +
                                  "<div class=\"col-md-6\"><h2>Column 1</h2><p>" + LoremIpsum.SubSection1 +
                                  "</p></div>" +
                                  "<div class=\"col-md-6\"><h2>Column 2</h2><p>" + LoremIpsum.SubSection2 +
                                  "</p></div>" +
                                  "</div>" +
                                  "</main>" +
                                  "</div>";

            var article = new Article
            {
                ArticleNumber = 0,
                VersionNumber = 0,
                Title = title,
                Content = defaultTemplate,
                Updated = DateTime.Now.ToUniversalTime(),
                UrlPath = HttpUtility.UrlEncode(title.Replace(" ", "_")),
                ArticleLogs = new List<ArticleLog>(),
                LayoutId = layout?.Id
            };

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Makes an article the new home page.
        /// </summary>
        /// <param name="id">Article Id (row key)</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     The old home page has its URL changed from "root" to its normal path.  Also writes to the log
        ///     using <see cref="HandleLogEntry" />. Also flushes REDIS cache for the home page.
        /// </remarks>
        public async Task NewHomePage(int id, string userId)
        {
            //
            // Can't make a deleted file the new home page.
            //
            var newHome = await DbContext.Articles
                .Where(w => w.Id == id && w.StatusCode != (int)StatusCodeEnum.Deleted).ToListAsync();
            if (newHome == null) throw new Exception($"Article Id {id} not found.");
            var utcDateTimeNow = DateTime.Now.ToUniversalTime();
            if (newHome.All(a => a.Published != null && a.Published.Value <= utcDateTimeNow))
                throw new Exception("Article has not been published yet.");

            var currentHome = await DbContext.Articles.Where(w => w.UrlPath.ToLower() == "root").ToListAsync();

            var newUrl = HandleUrlEncodeTitle(currentHome.FirstOrDefault()?.Title);

            foreach (var article in currentHome) article.UrlPath = newUrl;

            await DbContext.SaveChangesAsync();

            foreach (var article in newHome) article.UrlPath = "root";

            await DbContext.SaveChangesAsync();

            var newHomeArticle = newHome.OrderByDescending(o => o.Id).FirstOrDefault(w => w.Published != null);

            if (newHomeArticle != null)
                HandleLogEntry(newHomeArticle, $"Article {newHomeArticle.ArticleNumber} is now the new home page.",
                    userId);
        }

        /// <summary>
        ///     This method puts an article into trash, and, all its versions.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>This method puts an article into trash. Use <see cref="RetrieveFromTrash" /> to restore an article. </para>
        ///     <para>WARNING: Make sure the menu MenuController.Index does not reference deleted files.</para>
        /// </remarks>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public async Task TrashArticle(int articleNumber)
        {
            var doomed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (doomed == null) throw new KeyNotFoundException($"Article number {articleNumber} not found.");

            if (doomed.Any(a => a.UrlPath.ToLower() == "root"))
                throw new NotSupportedException(
                    "Cannot trash the home page.  Replace home page with another, then send to trash.");
            foreach (var article in doomed) article.StatusCode = (int)StatusCodeEnum.Deleted;

            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Retrieves and article and all its versions from trash.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         Please be aware of the following:
        ///     </para>
        ///     <list type="bullet">
        ///         <item><see cref="Article.StatusCode" /> is set to <see cref="StatusCodeEnum.Active" />.</item>
        ///         <item><see cref="Article.Title" /> will be altered if a live article exists with the same title.</item>
        ///         <item>
        ///             If the title changed, the <see cref="Article.UrlPath" /> will be updated using
        ///             <see cref="ArticleLogic.HandleUrlEncodeTitle" />.
        ///         </item>
        ///         <item>The article and all its versions are set to unpublished (<see cref="Article.Published" /> set to null).</item>
        ///     </list>
        /// </remarks>
        public async Task RetrieveFromTrash(int articleNumber)
        {
            var redeemed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (redeemed == null) throw new KeyNotFoundException($"Article number {articleNumber} not found.");

            var title = redeemed.FirstOrDefault()?.Title.ToLower();

            // Avoid restoring an article that has a title that collides with a live article.
            if (await DbContext.Articles.AnyAsync(a =>
                a.Title.ToLower() == title && a.ArticleNumber != articleNumber &&
                a.StatusCode == (int)StatusCodeEnum.Deleted))
            {
                var newTitle = title + " (" + await DbContext.Articles.CountAsync() + ")";
                var url = HandleUrlEncodeTitle(newTitle);
                foreach (var article in redeemed)
                {
                    article.Title = newTitle;
                    article.UrlPath = url;
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }
            else
            {
                foreach (var article in redeemed)
                {
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }

            await DbContext.SaveChangesAsync();
        }

        #endregion

        #region SAVE ARTICLE METHODS

        /// <summary>
        ///     Updates an existing article, or inserts a new one.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <param name="teamId"></param>
        /// <remarks>
        ///     <para>
        ///         If the article number is '0', a new article is inserted.  If a version number is '0', then
        ///         a new version is created. Recreates <see cref="ArticleViewModel" /> using method
        ///         <see cref="ArticleLogic.BuildArticleViewModel" />.
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             Published articles will trigger the prior published article to have its Expired property set to this
        ///             article's published property.
        ///         </item>
        ///         <item>
        ///             Actions taken here by users are logged using <see cref="HandleLogEntry" />.
        ///         </item>
        ///         <item>
        ///             Title changes (and redirects) are handled by adding a new article with redirect info.
        ///         </item>
        ///         <item>
        ///             The <see cref="ArticleViewModel" /> that is returned, is rebuilt using
        ///             <see cref="ArticleLogic.BuildArticleViewModel" />.
        ///         </item>
        ///         <item>
        ///            <see cref="Article.Updated"/> property is automatically updated with current UTC date and time.
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <returns></returns>
        public async Task<ArticleUpdateResult> UpdateOrInsert(ArticleViewModel model, string userId, int? teamId = null)
        {
            var flushUrls = new List<string>();

            model.Content = Ensure_ContentEditable_IsMarked(model.Content);
			
			UpdateHeadBaseTag(model);
			
            Article article;
            
            //if (!string.IsNullOrEmpty(model.Content))
            //{
            //    //// When we save to the database, remove content editable attribute.
            //    model.Content = model.Content.Replace("contenteditable=", "crx=",
            //        StringComparison.CurrentCultureIgnoreCase);
            //}

            if (!await DbContext.Users.AnyAsync(a => a.Id == userId))
                throw new Exception($"User ID: {userId} not found!");

            //
            //  Validate that title is not already taken.
            //
            if (!await ValidateTitle(model.Title, model.ArticleNumber))
                throw new Exception($"Title '{model.Title}' already taken");

            var isRoot =
                await DbContext.Articles.AnyAsync(a => a.ArticleNumber == model.ArticleNumber && a.UrlPath == "root");


            // Enforce the default layout here
            var defaultLayout = await DbContext.Layouts.FirstOrDefaultAsync(l => l.IsDefault);

            // ************************************
            // DECISION: NEW ARTICLE OR EDITING EXISTING ARTICLE
            //
            if (model.ArticleNumber == 0)
            {
                //
                // CREATING NEW ARTICLE NOW
                // 
                // If the article number is 0, then this is a new article.
                // The save action will give this a new unique article number.
                //

                // If no other articles exist, then make this the new root or home page.
                isRoot = await DbContext.Articles.CountAsync() == 0;

                article = new Article
                {
                    ArticleNumber = await GetNextArticleNumber(),
                    VersionNumber = 1,
                    UrlPath = isRoot ? "root" : HandleUrlEncodeTitle(model.Title.Trim()),
                    ArticleLogs = new List<ArticleLog>(),
                    Updated = DateTime.Now.ToUniversalTime(),
                    RoleList = model.RoleList,
                    LayoutId = defaultLayout.Id
                };

                model.Published = isRoot ? DateTime.Now.ToUniversalTime() : model.Published?.ToUniversalTime();

                //model.UrlPath = article.UrlPath;

                //
                // Update base href (for Angular apps)
                //
                //UpdateHeadBaseTag(model, article);

                var articleCount = await DbContext.Articles.CountAsync();

                DbContext.Articles.Add(article); // Set in an "add" state.
                HandleLogEntry(article, $"New article {articleCount}", userId);
                HandleLogEntry(article, "New version 1", userId);

                if (article.Published.HasValue || isRoot)
                    HandleLogEntry(article, "Publish", userId);

                // If this is part of a team, add that here.
                if (teamId != null)
                {
                    var team = await DbContext.Teams.Include(i => i.Articles)
                        .FirstOrDefaultAsync(f => f.Id == teamId.Value);

                    team.Articles.Add(article);
                }

                //
                // Get rid of any old redirects
                //
                var oldRedirects = DbContext
                    .Articles
                    .Where(w =>
                        w.StatusCode == (int)StatusCodeEnum.Redirect &&
                        w.UrlPath == article.UrlPath
                    );

                DbContext.Articles.RemoveRange(oldRedirects);
            }
            else
            {
                //
                // EDITING EXISTING ARTICLE
                //
                // Validate that this article already exists.
                //
                if (!await DbContext.Articles.AnyAsync(a => a.ArticleNumber == model.ArticleNumber))
                    throw new Exception($"Article number: {model.ArticleNumber} not found!");

                //
                // Retrieve the article that we will be using.
                // This will either be used to create a new version (detached then added as new),
                // or updated in place.
                //
                article = await DbContext.Articles.FirstOrDefaultAsync(a => a.Id == model.Id);

                //
                // We are adding a new version.
                // DETACH and put into an ADD state.
                //
                if (model.VersionNumber == 0)
                {
                    //
                    // ADDING NEW ARTICLE VERSION
                    //
                    var versionNumber = await GetNextVersionNumber(model.ArticleNumber);

                    article = new Article
                    {
                        ArticleNumber = model.ArticleNumber, // This stays the same
                        VersionNumber = versionNumber,
                        UrlPath = model.UrlPath,
                        HeaderJavaScript = article.HeaderJavaScript,
                        FooterJavaScript = article.FooterJavaScript,
                        ArticleLogs = new List<ArticleLog>(),
                        LayoutId = article.LayoutId,
                        Title = article.Title, // Keep this from previous version, will handle title change below.
                        Updated = DateTime.Now.ToUniversalTime()
                    };

                    // Force the model into an unpublished state
                    model.Published = null;
                    //model.UrlPath = article.UrlPath;

                    DbContext.Articles.Add(article); // Put this entry in an add state

                    // Make sure this saves
                    await DbContext.SaveChangesAsync();

                    HandleLogEntry(article, "New version", userId);
                }
                else
                {
                    HandleLogEntry(article, "Edit existing", userId);
                }

                //
                // Is the title changing? If so handle redirect if this is NOT the root.
                //
                if (!isRoot && !string.Equals(article.Title, model.Title, StringComparison.CurrentCultureIgnoreCase))
                {
                    //
                    // ARTICLE TITLE IS CHANGING, SO CREATE A REDIRECT AND CHANGE ARTICLE TITLE AND URL
                    //
                    // And capture old URL to flush.
                    flushUrls.Add(model.UrlPath);

                    var oldTitle = article.Title;
                    var newTitle = model.Title;
                    var oldUrl = article.UrlPath;
                    var newUrl = HandleUrlEncodeTitle(model.Title);

                    // Update sub articles.
                    var subArticles = await GetAllSubArticles(model.UrlPath);

                    foreach (var subArticle in subArticles)
                    {
                        if (!subArticle.Title.Equals("redirect", StringComparison.CurrentCultureIgnoreCase))
                        {
                            subArticle.Title = UpdatePrefix(oldTitle, newTitle, subArticle.Title);
                        }
                        subArticle.UrlPath = UpdatePrefix(oldUrl, newUrl, subArticle.UrlPath);
                    }

                    DbContext.Articles.UpdateRange(subArticles);

                    // Remove any conflicting redirects
                    var conflictingRedirects = await DbContext.Articles.Where(a => a.Content == newUrl && a.Title.ToLower().Equals("redirect")).ToListAsync();

                    if (conflictingRedirects.Any())
                    {
                        DbContext.Articles.RemoveRange(conflictingRedirects);
                    }

                    //
                    // Update the path to reflect new title 
                    //
                    article.UrlPath = HandleUrlEncodeTitle(model.Title);
                    //model.UrlPath = article.UrlPath;

                    //
                    // Update base href
                    //
                    //UpdateHeadBaseTag(model, article);

                    // Add redirect here
                    await DbContext.Articles.AddAsync(new Article
                    {
                        Id = 0,
                        LayoutId = null,
                        ArticleNumber = 0,
                        StatusCode = (int)StatusCodeEnum.Redirect,
                        UrlPath = oldUrl, // Old URL
                        VersionNumber = 0,
                        Published = DateTime.Now.ToUniversalTime().AddDays(-1), // Make sure this sticks!
                        Title = "Redirect",
                        Content = newUrl, // New URL
                        Updated = DateTime.Now.ToUniversalTime(),
                        HeaderJavaScript = null,
                        FooterJavaScript = null,
                        Layout = null,
                        ArticleLogs = null,
                        MenuItems = null
                    });


                    HandleLogEntry(article, $"Redirect {model.UrlPath} to {article.UrlPath}", userId);

                    // We have to change the title and paths for all versions now.
                    var articlesToUpdate = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                        .ToListAsync();

                    foreach (var art in articlesToUpdate)
                    {

                        //
                        // Update base href (for Angular apps)
                        //
                        UpdateHeadBaseTag(article);

                        art.Title = model.Title;
                        art.Updated = DateTime.Now.ToUniversalTime();
                        art.UrlPath = article.UrlPath;
                    }

                    DbContext.Articles.UpdateRange(articlesToUpdate);
                    await DbContext.SaveChangesAsync();
                }

                //
                // Is the role list changing?
                //
                if (!string.Equals(article.RoleList, model.RoleList, StringComparison.CurrentCultureIgnoreCase))
                {
                    // get all prior article versions, changing security now.
                    var oldArticles = DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                        .ToListAsync().Result;

                    HandleLogEntry(article, $"Changing role access from '{article.RoleList}' to '{model.RoleList}'.",
                        userId);

                    //
                    // We have to change the title and paths for all versions now.
                    //
                    foreach (var oldArticle in oldArticles) oldArticle.RoleList = model.RoleList;
                }
            }

            //
            // Detect if the article is being published, expire the prior
            // published articles and add log entry
            //


            if (model.Published.HasValue)
                HandleLogEntry(article, model.Published.HasValue ? "Publish" : "Un-publish", userId);


            // If was NOT published before, but now is; or
            // if WAS published before, now is NOT; or
            // is now published, then indicate to flush caches.
            if (article.Published.HasValue != model.Published.HasValue
                || model.Published.HasValue)
                flushUrls.Add(model.UrlPath);

            article.Title = model.Title.Trim();

            if (model.Content == null || model.Content.Trim() == "")
            {
                article.Content = "";
            }
            else
            {
                //// When we save to the database, remove content editable attribute.
                article.Content = model.Content.Replace("contenteditable=", "crx=",
                    StringComparison.CurrentCultureIgnoreCase);
            }

            //
            // Make sure everything server-side is saved in UTC time.
            //
            if (model.Published.HasValue)
                article.Published = model.Published.Value.ToUniversalTime();
            else
                article.Published = null;
            article.Updated = DateTime.Now.ToUniversalTime();

            article.HeaderJavaScript = model.HeaderJavaScript;
            article.FooterJavaScript = model.FooterJavaScript;

            article.RoleList = model.RoleList;

            article.LayoutId = defaultLayout.Id;
            article.Layout = defaultLayout;

            // Save changes to database.
            await DbContext.SaveChangesAsync();

            // Resets the expiration dates, based on the last published article
            await ResetVersionExpirations(article.ArticleNumber);

            var result = new ArticleUpdateResult
            {
                Model = await BuildArticleViewModel(article, "en-US"),
                Urls = flushUrls.Distinct().ToList()
            };

            return result;
        }

        private string UpdatePrefix(string oldprefix, string newPrefix, string targetString)
        {
            var updated = newPrefix + targetString.TrimStart(oldprefix.ToArray());
            return updated;
        }

        /// <summary>
        /// Update head tag to match path 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// Angular uses the BASE tag within the HEAD to set relative path to article/app.
        /// If that tag is detected, it is updated automatically to match the current <see cref="Article.UrlPath"/>.
        /// </remarks>
        public void UpdateHeadBaseTag(ArticleViewModel model)
        {
            if (string.IsNullOrEmpty(model.HeaderJavaScript))
            {
                return;
            }

            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(model.HeaderJavaScript);

            // <meta name="ccms:framework" value="angular">
            var meta = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='ccms:framework']");

            // This only needs to be run if the framework is "Angular"
            if (meta != null && meta.Attributes["value"].Value.ToLower() != "angular")
            {
                return;
            }

            var element = htmlDoc.DocumentNode.SelectSingleNode("//base");

            if (element == null)
            {
                return;
            }

            var urlPath = $"/{model.UrlPath.ToLower().Trim('/')}/";

            var href = element.Attributes["href"];

            if (href == null)
            {
                element.Attributes.Add("href", urlPath);
            }
            else
            {
                href.Value = urlPath;
            }

            model.HeaderJavaScript = htmlDoc.DocumentNode.OuterHtml;
            model.UrlPath = urlPath;

            return;
        }

        /// <summary>
        /// Update head tag to match path 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// Angular uses the BASE tag within the HEAD to set relative path to article/app.
        /// If that tag is detected, it is updated automatically to match the current <see cref="Article.UrlPath"/>.
        /// </remarks>
        public void UpdateHeadBaseTag(Article model)
        {
            if (string.IsNullOrEmpty(model.HeaderJavaScript))
            {
                return;
            }

            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(model.HeaderJavaScript);

            var element = htmlDoc.DocumentNode.SelectSingleNode("//base");

            if (element == null)
            {
                return;
            }

            var urlPath = $"/{model.UrlPath.ToLower().Trim('/')}/";

            var href = element.Attributes["href"];

            if (href == null)
            {
                element.Attributes.Add("href", urlPath);
            }
            else
            {
                href.Value = urlPath;
            }

            model.HeaderJavaScript = htmlDoc.DocumentNode.OuterHtml;
            model.UrlPath = urlPath;

            return;
        }

        /// <summary>
        ///     Updates the date/time stamp for all published articles to current UTC time.
        /// </summary>
        /// <returns>Number of articles updated with new date/time</returns>
        /// <remarks>This action is used only for "publishing" entire websites.</remarks>
        public async Task<int> UpdateDateTimeStamps()
        {
            var articleIds = (await PrivateGetArticleList(DbContext.Articles.AsQueryable()))?.Select(s => s.Id)
                .ToList();
            if (articleIds == null || articleIds.Any() == false) return 0;

            // DateTime.Now uses DateTime.UtcNow internally and then applies localization.
            // In short, use ToUniversalTime() if you already have DateTime.Now and
            // to convert it to UTC, use DateTime.UtcNow if you just want to retrieve the
            // current time in UTC.
            var now = DateTime.Now.ToUniversalTime();
            var count = await DbContext.Articles.Where(a => articleIds.Contains(a.Id)).UpdateAsync(u => new Article
            {
                Updated = now
            });
            return count;
        }

        /// <summary>
        ///     Changes the status of an article by marking all versions with that status.
        /// </summary>
        /// <param name="articleNumber">Article to set status for</param>
        /// <param cref="StatusCodeEnum" name="code"></param>
        /// <param name="userId"></param>
        /// <exception cref="Exception">User ID or article number not found.</exception>
        /// <returns>Returns the number of versions for the given article where status was set</returns>
        public async Task<int> SetStatus(int articleNumber, StatusCodeEnum code, string userId)
        {
            if (!await DbContext.Users.AnyAsync(a => a.Id == userId))
                throw new Exception($"User ID: {userId} not found!");

            var versions = 
                await DbContext.Articles.Where(a => a.ArticleNumber == articleNumber).ToListAsync();
            
            if (!versions.Any())
                throw new Exception($"Article number: {articleNumber} not found!");

            foreach (var version in versions)
            {
                version.StatusCode = (int)code;
                version.ArticleLogs ??= new List<ArticleLog>();

                var statusText = code switch
                {
                    StatusCodeEnum.Deleted => "deleted",
                    StatusCodeEnum.Active => "active",
                    _ => "inactive"
                };

                version.ArticleLogs.Add(new ArticleLog
                {
                    ActivityNotes = $"Status changed to '{statusText}'.",
                    IdentityUserId = userId,
                    DateTimeStamp = DateTime.Now.ToUniversalTime()
                });
                
            }
            
            var count = await DbContext.SaveChangesAsync();
            return count;
        }

        #endregion

        #region GET METHODS ONLY FOR EDITOR

        /// <summary>
        ///     Gets a copy of the article ready for edit.
        /// </summary>
        /// <param name="articleNumber">Article Number</param>
        /// <param name="versionNumber">Version to edit</param>
        /// <returns>
        ///     <see cref="ArticleViewModel" />
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="ArticleLogic.BuildArticleViewModel" />
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(int articleNumber, int versionNumber)
        {
            var article = await DbContext.Articles.Include(l => l.Layout)
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber == articleNumber &&
                         a.VersionNumber == versionNumber &&
                         a.StatusCode != 2);

            if (article == null)
                throw new Exception($"Article number:{articleNumber}, Version:{versionNumber}, not found.");

            return await BuildArticleViewModel(article, "en-US");
        }


        /// <summary>
        ///     Gets an article by ID (row Key), or creates a new (unsaved) article if id is null.
        /// </summary>
        /// <param name="id">Row Id (or identity) number.  If null returns a new article.</param>
        /// <param name="controllerName"></param>
        /// <remarks>
        ///     <para>
        ///         For new articles, uses <see cref="Create" /> and the method
        ///         <see cref="ArticleLogic.BuildArticleViewModel" /> to
        ///         generate the <see cref="ArticleViewModel" /> .
        ///     </para>
        ///     <para>
        ///         Retrieves <see cref="Article" /> and builds an <see cref="ArticleViewModel" /> using the method
        ///         <see cref="ArticleLogic.BuildArticleViewModel" />,
        ///         or in the case of a template, uses method <see cref="BuildTemplateViewModel" />.
        ///     </para>
        /// </remarks>
        /// <returns>
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="ArticleLogic.BuildArticleViewModel" /> or <see cref="BuildTemplateViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(int? id, EnumControllerName controllerName)
        {
            if (controllerName == EnumControllerName.Template)
            {
                if (id == null)
                    throw new Exception("Template ID:null not found.");

                var idNo = id.Value;
                var template = await DbContext.Templates.FindAsync(idNo);

                if (template == null) throw new Exception($"Template ID:{id} not found.");
                return BuildTemplateViewModel(template);
            }

            //
            // This is used to create a "blank" page just so we have something to get started with.
            //
            if (id == null)
            {
                var count = await DbContext.Articles.CountAsync();
                return await Create("Page " + count);
            }

            var article = await DbContext.Articles
                .Include(l => l.Layout)
                .FirstOrDefaultAsync(a => a.Id == id && a.StatusCode != 2);

            article.Content = Ensure_ContentEditable_IsMarked(article.Content);

            if (article == null) throw new Exception($"Article ID:{id} not found.");
            return await BuildArticleViewModel(article, "en-US", false);
        }

        /// <summary>
        /// Gets the sub articles for a page
        /// </summary>
        /// <param name="urlPrefix">URL Prefix</param>
        /// <returns></returns>
        private async Task<List<Article>> GetAllSubArticles(string urlPrefix)
        {
            if (string.IsNullOrEmpty(urlPrefix) || string.IsNullOrWhiteSpace(urlPrefix) || urlPrefix.Equals("/"))
            {
                urlPrefix = "";
            }
            else
            {
                urlPrefix = System.Web.HttpUtility.UrlDecode(urlPrefix.ToLower().Replace("%20", "_").Replace(" ", "_")) + "/";
            }

            var query = DbContext.Articles
                .Where(a => a.Published <= DateTime.UtcNow && a.UrlPath.StartsWith(urlPrefix));

            return await query.ToListAsync();
        }

        #region LISTS

        /// <summary>
        ///     Gets the latest versions of articles (published or not).
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        ///     <para>Note: Cannot list articles that are trashed.</para>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleList(string userId, bool showDefaultSort = true)
        {
            var articles = DbContext.Articles.Include(i => i.Team)
                .Where(t => t.Team.Members.Any(m => m.UserId == userId));

            //= __dbContext.Teams.Include(i => i.Articles)
            //.Where(t => t.Members.Any(a => a.UserId == userId)).SelectMany(s => s.Articles);

            return await PrivateGetArticleList(articles);
        }

        /// <summary>
        ///     Gets the latest versions of articles (published or not).
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        ///     <para>Note: Cannot list articles that are trashed.</para>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleList(IQueryable<Article> query = null,
            bool showDefaultSort = true)
        {
            return await PrivateGetArticleList(query ?? DbContext.Articles.AsQueryable());
        }

        /// <summary>
        ///     Gets the latest versions of articles (published or not) for a specific team.
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        ///     <para>Note: Cannot list articles that are trashed.</para>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleList(int teamId)
        {
            var articles = DbContext.Teams.Include(i => i.Articles)
                .Where(t => t.Id == teamId).SelectMany(s => s.Articles);

            return await PrivateGetArticleList(articles);
        }

        /// <summary>
        /// Get a list of article redirects
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public async Task<List<RedirectItemViewModel>> GetArticleRedirects()
        {
            var redirectCode = (int)StatusCodeEnum.Redirect;
            var query = DbContext.Articles.OrderBy(o => o.UrlPath).Where(w => w.StatusCode == redirectCode);

            return await query.Select(s => new RedirectItemViewModel()
            {
                FromUrl = s.UrlPath,
                Id = s.Id,
                ToUrl = s.Content
            }).ToListAsync();
        }

        /// <summary>
        ///     Gets the latest versions of articles that are in the trash.
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleTrashList()
        {
            var data = await
                (from x in DbContext.Articles
                 where x.StatusCode == (int)StatusCodeEnum.Deleted
                 group x by x.ArticleNumber
                    into g
                 select new
                 {
                     ArticleNumber = g.Key,
                     VersionNumber = g.Max(i => i.VersionNumber),
                     LastPublished = g.Max(m => m.Published),
                     Status = g.Max(f => f.StatusCode)
                 }).ToListAsync();

            var model = new List<ArticleListItem>();

            foreach (var item in data)
            {
                var art = await DbContext.Articles.FirstOrDefaultAsync(
                    f => f.ArticleNumber == item.ArticleNumber && f.VersionNumber == item.VersionNumber
                );
                model.Add(new ArticleListItem
                {
                    ArticleNumber = art.ArticleNumber,
                    Id = art.Id,
                    LastPublished = item.LastPublished?.ToUniversalTime(),
                    Title = art.Title,
                    Updated = art.Updated.ToUniversalTime(),
                    VersionNumber = art.VersionNumber,
                    Status = art.StatusCode == 0 ? "Active" : "Inactive",
                    UrlPath = art.UrlPath
                });
            }

            return model;
        }

        #endregion

        #endregion
    }
}