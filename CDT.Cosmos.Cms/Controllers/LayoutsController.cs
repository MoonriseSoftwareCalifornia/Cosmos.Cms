using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using HtmlAgilityPack;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Z.EntityFramework.Plus;

namespace CDT.Cosmos.Cms.Controllers
{
    /// <summary>
    /// Layouts controller
    /// </summary>
    [Authorize(Roles = "Administrators, Editors")]
    public class LayoutsController : BaseController
    {
        private readonly ArticleEditLogic _articleLogic;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<LayoutsController> _logger;
        private readonly Uri _blobPublicAbsoluteUrl;
        private readonly IViewRenderService _viewRenderService;



        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="userManager"></param>
        /// <param name="articleLogic"></param>
        /// <param name="syncContext"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="viewRenderService"></param>
        public LayoutsController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            SqlDbSyncContext syncContext,
            IOptions<CosmosConfig> options,
            ILogger<LayoutsController> logger,
            IViewRenderService viewRenderService) : base(dbContext, userManager, articleLogic, options)
        {
            if (options.Value.SiteSettings.AllowSetup ?? true)
            {
                throw new Exception("Permission denied. Website in setup mode.");
            }

            if (syncContext.IsConfigured())
                dbContext.LoadSyncContext(syncContext);

            _dbContext = dbContext;
            _articleLogic = articleLogic;
            _logger = logger;

            var htmlUtilities = new HtmlUtilities();

            if (htmlUtilities.IsAbsoluteUri(options.Value.SiteSettings.BlobPublicUrl))
            {
                _blobPublicAbsoluteUrl = new Uri(options.Value.SiteSettings.BlobPublicUrl);
            }
            else
            {
                _blobPublicAbsoluteUrl = new Uri(options.Value.SiteSettings.PublisherUrl.TrimEnd('/') + "/" + options.Value.SiteSettings.BlobPublicUrl.TrimStart('/'));
            }

            _viewRenderService = viewRenderService;
        }

        private bool LayoutExists(int id)
        {
            return _dbContext.Layouts.Any(e => e.Id == id);
        }

        /// <summary>
        /// Gets a list of layouts
        /// </summary>
        /// <param name="includeDefault">Default = false</param>
        /// <returns></returns>
        public async Task<IActionResult> GetLayoutList(bool includeDefault = false)
        {
            if (includeDefault)
            {
                return Json(await _dbContext.Layouts.OrderBy(o => o.LayoutName).Select(s => new { LayoutId = s.Id, s.LayoutName, s.Notes }).ToListAsync());
            }

            return Json(await _dbContext.Layouts.Where(w => w.IsDefault == false).OrderBy(o => o.LayoutName).Select(s => new { LayoutId = s.Id, s.LayoutName, s.Notes }).ToListAsync());
        }

        /// <summary>
        /// Gets the home page with the specified layout (may not be the default layout)
        /// </summary>
        /// <param name="id">Layout Id (default layout if null)</param>
        /// <returns>ViewResult with <see cref="ArticleViewModel"/></returns>
        private async Task<IActionResult> GetLayoutWithHomePage(int? id)
        {
            // Get the home page
            var model = await _articleLogic.GetByUrl("");

            // Specify layout if given.
            if (id.HasValue)
            {
                var layout = await _dbContext.Layouts.FirstOrDefaultAsync(i => i.Id == id.Value);
                model.Layout = new LayoutViewModel(layout);
            }

            // Make its editable
            model.Layout.HtmlHeader = model.Layout.HtmlHeader.Replace(" crx=\"", " contenteditable=\"", StringComparison.CurrentCultureIgnoreCase);
            model.Layout.FooterHtmlContent = model.Layout.FooterHtmlContent.Replace(" crx=\"", " contenteditable=\"", StringComparison.CurrentCultureIgnoreCase);

            return View(model);
        }

        /// <summary>
        /// After a layout change, refresh everything!
        /// </summary>
        /// <returns></returns>
        private async Task MakeGlobalChange()
        {
            _ = await base.UpdateTimeStamps();
            _ = await FlushCdn(_logger, new[] { "/*" });
        }

        /// <summary>
        /// Gets a list of layouts
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            if (!await _dbContext.Layouts.AnyAsync())
            {
                _dbContext.Layouts.AddRange(LayoutDefaults.GetStarterLayouts());
                await _dbContext.SaveChangesAsync();
            }
            return View();
        }

        /// <summary>
        /// Page returns a list of community layouts.
        /// </summary>
        /// <returns></returns>
        public IActionResult CommunityLayouts()
        {
            return View();
        }

        /// <summary>
        /// Create a new layout
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Create()
        {
            var layout = new Layout();
            layout.IsDefault = false;
            layout.LayoutName = "New Layout " + await _dbContext.Layouts.CountAsync();
            layout.Notes = "New layout created. Please customize using code editor.";
            _dbContext.Layouts.Add(layout);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("EditCode", new { layout.Id });
        }

        /// <summary>
        /// Edit a layout by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int? id)
        {
            return await GetLayoutWithHomePage(id);
        }

        /// <summary>
        /// Edit the page header and footer of a layout.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="header"></param>
        /// <param name="footer"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Edit([Bind("id,header,footer")] int id, string header, string footer)
        {
            var layout = await _dbContext.Layouts.FirstOrDefaultAsync(i => i.Id == id);

            // Make editable
            //header = header.Replace(" contenteditable=\"", " crx=\"", StringComparison.CurrentCultureIgnoreCase);
            //footer = footer.Replace(" contenteditable=\"", " crx=\"", StringComparison.CurrentCultureIgnoreCase);

            layout.HtmlHeader = header;
            layout.FooterHtmlContent = footer;

            await _dbContext.SaveChangesAsync();

            return await GetLayoutWithHomePage(id);
        }

        /// <summary>
        /// Gets a layout to edit it's notes
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditNotes(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            var model = await _dbContext.Layouts.Select(s => new LayoutIndexViewModel
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes
            }).FirstOrDefaultAsync(f => f.Id == id.Value);

            if (model == null) return NotFound();

            return View(model);
        }

        /// <summary>
        /// Edit layout notes
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> EditNotes([Bind(include: "Id,IsDefault,LayoutName,Notes")] LayoutIndexViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model != null)
            {
                var layout = await _dbContext.Layouts.FindAsync(model.Id);
                layout.LayoutName = model.LayoutName;
                var contentHtmlDocument = new HtmlDocument();
                contentHtmlDocument.LoadHtml(HttpUtility.HtmlDecode(model.Notes));
                if (contentHtmlDocument.ParseErrors.Any())
                    foreach (var error in contentHtmlDocument.ParseErrors)
                        ModelState.AddModelError("Notes", error.Reason);

                var remove = "<div style=\"display:none;\"></div>";
                layout.Notes = contentHtmlDocument.ParsedText.Replace(remove, "").Trim();
                //layout.IsDefault = model.IsDefault;
                if (model.IsDefault)
                {
                    var layouts = await _dbContext.Layouts.Where(w => w.Id != model.Id).ToListAsync();
                    foreach (var layout1 in layouts) layout1.IsDefault = false;
                }

                await _dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edit code for a layout
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditCode(int? id)
        {
            if (id == null) return NotFound();

            var layout = await _dbContext.Layouts.FindAsync(id);
            if (layout == null) return NotFound();

            var model = new LayoutCodeViewModel
            {
                Id = layout.Id,
                EditorTitle = layout.LayoutName,
                EditorFields = new List<EditorField>
                {
                    new()
                    {
                        FieldId = "Head",
                        FieldName = "Head",
                        EditorMode = EditorMode.Html,
                        ToolTip = "Layout content to appear in the HEAD of every page."
                    },
                    new()
                    {
                        FieldId = "HtmlHeader",
                        FieldName = "Header Content",
                        EditorMode = EditorMode.Html,
                        ToolTip = "Layout body header content to appear on every page."
                    },
                    new()
                    {
                        FieldId = "BodyHtmlAttributes",
                        FieldName = "Body Attributes",
                        EditorMode = EditorMode.Html,
                        ToolTip = "Body tag attributes such as class or styles."
                    },
                    new()
                    {
                        FieldId = "FooterHtmlContent",
                        FieldName = "Footer Content",
                        EditorMode = EditorMode.Html,
                        ToolTip = "Layout footer content to appear at the bottom of the body on every page."
                    }
                },
                CustomButtons = new List<string> { "Preview", "Layouts" },
                Head = layout.Head,
                HtmlHeader = layout.HtmlHeader,
                BodyHtmlAttributes = layout.BodyHtmlAttributes,
                FooterHtmlContent = layout.FooterHtmlContent,
                EditingField = "Head"
            };
            return View(model);
        }

        /// <summary>
        ///     Saves the code and html of the page.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="layout"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         This method saves page code to the database. The following properties are validated with method
        ///         <see cref="BaseController.BaseValidateHtml" />:
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.Head" />
        ///         </item>
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.HtmlHeader" />
        ///         </item>
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.FooterHtmlContent" />
        ///         </item>
        ///     </list>
        ///     <para>
        ///         HTML formatting errors that could not be automatically fixed by <see cref="BaseController.BaseValidateHtml" />
        ///         are logged with <see cref="ControllerBase.ModelState" />.
        ///     </para>
        /// </remarks>
        /// <exception cref="NotFoundResult"></exception>
        /// <exception cref="UnauthorizedResult"></exception>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCode(int id, LayoutCodeViewModel layout)
        {
            if (id != layout.Id) return NotFound();

            if (ModelState.IsValid)
                try
                {
                    // Strip out BOM
                    layout.Head = StripBOM(layout.Head);
                    layout.HtmlHeader = StripBOM(layout.HtmlHeader);
                    layout.FooterHtmlContent = StripBOM(layout.FooterHtmlContent);
                    layout.BodyHtmlAttributes = StripBOM(layout.BodyHtmlAttributes);

                    //
                    // This layout now is the default, make sure the others are set to "false."

                    var entity = await _dbContext.Layouts.FindAsync(layout.Id);
                    entity.FooterHtmlContent =
                        BaseValidateHtml("FooterHtmlContent", layout.FooterHtmlContent);
                    entity.Head = BaseValidateHtml("Head", layout.Head);
                    entity.HtmlHeader = BaseValidateHtml("HtmlHeader", layout.HtmlHeader);
                    entity.BodyHtmlAttributes = layout.BodyHtmlAttributes;

                    // Check validation again after validation of HTML
                    if (entity.IsDefault)
                    {
                        await _dbContext.SaveChangesAsync();

                        // Make sure everything is refreshed.
                        await MakeGlobalChange();
                    }
                    else
                    {
                        await _dbContext.SaveChangesAsync();
                    }

                    var jsonModel = new SaveCodeResultJsonModel
                    {
                        ErrorCount = ModelState.ErrorCount,
                        IsValid = ModelState.IsValid
                    };
                    jsonModel.Errors.AddRange(ModelState.Values
                        .Where(w => w.ValidationState == ModelValidationState.Invalid)
                        .ToList());
                    jsonModel.ValidationState = ModelState.ValidationState;

                    return Json(jsonModel);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LayoutExists(layout.Id)) return NotFound();
                    throw;
                }

            return View(layout);
        }

        /// <summary>
        /// Preview 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Preview(int id)
        {
            var layout = await _dbContext.Layouts.FindAsync(id);
            if (layout == null) return NotFound();

            var model = await _articleLogic.Create("Layout Preview");
            model.Layout = new LayoutViewModel(layout);
            model.EditModeOn = false;
            model.ReadWriteMode = false;
            model.PreviewMode = true;

            return View("~/Views/Home/Preview.cshtml", model);
        }

        /// <summary>
        /// Preview how a layout will look in edit mode.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditPreview(int id)
        {
            var layout = await _dbContext.Layouts.FindAsync(id);
            var model = await _articleLogic.Create("Layout Preview");
            model.Layout = new LayoutViewModel(layout);
            model.EditModeOn = true;
            model.ReadWriteMode = true;
            model.PreviewMode = true;
            return View("~/Views/Home/Index.cshtml", model);
        }

        /// <summary>
        /// Exports a layout with a blank page
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ExportLayout(int? id)
        {
            var article = await _articleLogic.Create("Blank Page");

            var view = "~/Views/Layouts/ExportLayout.cshtml";
            var exportName = $"layoutid-{article.Layout.Id }.html";

            if (id.HasValue)
            {
                var layout = await _dbContext.Layouts.FindAsync(id.Value);
                article.Layout = new LayoutViewModel(layout);
            }
            else
            {
                view = "~/Views/Layouts/ExportBlank.cshtml";
                exportName = "blank-layout.html";
            }

            var htmlUtilities = new HtmlUtilities();

            article.Layout.Head = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.Head, _blobPublicAbsoluteUrl);
            article.Layout.HtmlHeader = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.HtmlHeader, _blobPublicAbsoluteUrl);
            article.Layout.FooterHtmlContent = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.FooterHtmlContent, _blobPublicAbsoluteUrl);

            article.HeaderJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeaderJavaScript, _blobPublicAbsoluteUrl);
            article.Content = htmlUtilities.RelativeToAbsoluteUrls(article.Content, _blobPublicAbsoluteUrl);
            article.FooterJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeaderJavaScript, _blobPublicAbsoluteUrl);

            var html = await _viewRenderService.RenderToStringAsync(view, article);



            var bytes = Encoding.UTF8.GetBytes(html);

            return File(bytes, "application/octet-stream", exportName);
        }

        /// <summary>
        /// Set a layout as the default layout.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> SetLayoutAsDefault(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            var model = await _dbContext.Layouts.Select(s => new LayoutIndexViewModel
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes
            }).FirstOrDefaultAsync(f => f.Id == id.Value);

            if (model == null) return NotFound();

            return View(model);
        }

        /// <summary>
        ///     Sets a layout as the default layout
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SetLayoutAsDefault([Bind(include: "Id,IsDefault,LayoutName,Notes")] LayoutIndexViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model == null) return RedirectToAction("Index");

            var layout = await _dbContext.Layouts.FindAsync(model.Id);
            layout.IsDefault = model.IsDefault;
            if (model.IsDefault)
            {
                await _dbContext.SaveChangesAsync();
                await _dbContext.Layouts.Where(w => w.Id != model.Id)
                    .UpdateAsync(u => new Layout
                    {
                        IsDefault = false
                    });
                int[] validCodes =
                {
                    (int) StatusCodeEnum.Active,
                    (int) StatusCodeEnum.Inactive
                };

                await _dbContext.Articles.Where(w => validCodes.Contains(w.StatusCode))
                    .UpdateAsync(u => new Article
                    {
                        LayoutId = layout.Id
                    });

                // Make sure everything is refreshed.
                await MakeGlobalChange();

                return RedirectToAction("Publish", "Editor");
            }

            return RedirectToAction("Index", "Layouts");
        }

        /// <summary>
        ///     Gets a list of layouts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Layouts([DataSourceRequest] DataSourceRequest request)
        {
            var model = _dbContext.Layouts.Select(s => new LayoutIndexViewModel
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes
            }).OrderByDescending(o => o.IsDefault).ThenBy(t => t.LayoutName);

            return Json(await model.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Reads the community layouts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_CommunityLayouts([DataSourceRequest] DataSourceRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var utilities = new LayoutUtilities();

            return Json(await utilities.CommunityCatalog.LayoutCatalog.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Gets the template pages for a community layout.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_LayoutTemplatePages([DataSourceRequest] DataSourceRequest request, string id)
        {
            if (request == null)
            {
                return null;
            }

            var utilities = new LayoutUtilities();

            var model = await utilities.GetPageTemplates(id);

            return Json(await model.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Upload a view
        /// </summary>
        /// <returns></returns>
        /// <remarks>You can upload a new layout, or a file to replace a layout NOT set as default.</remarks>
        public IActionResult Upload(int? id)
        {
            if (id.HasValue)
            {
                var layout = _dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == id.Value && f.IsDefault == false);
            }
            return View(new LayoutFileUploadViewModel());
        }

        /// <summary>
        /// Upload a layout
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        /// <remarks>You can upload a new layout, or a file to replace a layout NOT set as default.</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, int? id, string name, string description)
        {
            if (string.IsNullOrEmpty(name))
            {
                ModelState.AddModelError("Name", "Layer name required.");
            }
            if (file == null)
            {
                ModelState.AddModelError("File", "Please select a file.");
            }

            if (id.HasValue == false)
            {
                id = 0;
            }

            var layout = await _dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == id.Value);

            if (layout != null && layout.IsDefault)
            {
                ModelState.AddModelError("Id", "Cannot upload and replace the default layout.");
            }

            if (ModelState.IsValid)
            {

                if (layout != null && layout.IsDefault)
                {
                    ModelState.AddModelError("", "Cannot upload a layout to replace the 'default' layout.");
                    return View();
                }

                using var memstream = new MemoryStream();
                await file.CopyToAsync(memstream);
                var html = Encoding.UTF8.GetString(memstream.ToArray());

                // Load the HTML document.
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(html);

                var headNode = htmlDoc.DocumentNode.SelectSingleNode("//head");
                var headHtml = headNode.InnerHtml.Trim();

                // -----------------------------------------------------
                // Remove Cosmos Head Injection
                var cosmosHeadStart = headHtml.IndexOf(LayoutImportConstants.COSMOS_HEAD_START) + LayoutImportConstants.COSMOS_HEAD_START.Length;
                var cosmosHeadEnd = headHtml.IndexOf(LayoutImportConstants.COSMOS_HEAD_END);

                if (cosmosHeadStart == -1)
                {
                    ModelState.AddModelError("", $"Could not find {HttpUtility.HtmlEncode(LayoutImportConstants.COSMOS_HEAD_START)}");
                }
                if (cosmosHeadEnd == -1)
                {
                    ModelState.AddModelError("", $"Could not find  {HttpUtility.HtmlEncode(LayoutImportConstants.COSMOS_HEAD_END)}");
                }

                // Capture the layout head content
                if (ModelState.IsValid)
                {
                    headNode.InnerHtml = headHtml.Substring(cosmosHeadStart, cosmosHeadEnd - cosmosHeadStart);

                    var utilities = new LayoutUtilities();
                    if (layout == null)
                    {
                        layout = utilities.ParseHtml(htmlDoc.DocumentNode.OuterHtml);

                        layout.IsDefault = false;
                        layout.LayoutName = name;
                        layout.Notes = description;

                        _dbContext.Layouts.Add(layout);
                    }
                    else
                    {
                        var uploadedLayout = utilities.ParseHtml(htmlDoc.DocumentNode.OuterHtml);

                        layout.BodyHtmlAttributes = uploadedLayout.BodyHtmlAttributes;
                        layout.FooterHtmlContent = uploadedLayout.FooterHtmlContent;
                        layout.Head = uploadedLayout.Head;
                        layout.HtmlHeader = uploadedLayout.HtmlHeader;

                        layout.IsDefault = false;
                        layout.LayoutName = name;
                        layout.Notes = description;
                    }


                    //await _dbContext.SaveChangesAsync();

                    return RedirectToAction("EditCode", new { layout.Id });
                }
            }

            return View(new LayoutFileUploadViewModel() { Description = description, File = file, Id = (id == 0 || id == null) ? null : id.Value, Name = name });
        }

        /// <summary>
        ///     Updates a layout
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Update_Layout([DataSourceRequest] DataSourceRequest request,
            LayoutIndexViewModel model)
        {
            var entity = await _dbContext.Layouts.FindAsync(model.Id);
            entity.IsDefault = model.IsDefault;
            entity.LayoutName = model.LayoutName;
            entity.Notes = model.Notes;
            await _dbContext.SaveChangesAsync();

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        ///     Removes a layout and its associated template pages.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>Cannot remove the default layout.</remarks>
        [HttpPost]
        public async Task<IActionResult> Destroy_Layout([DataSourceRequest] DataSourceRequest request,
            LayoutIndexViewModel model)
        {
            if (model != null)
            {
                var entity = await _dbContext.Layouts.FindAsync(model.Id);

                if (!entity.IsDefault)
                {
                    // also remove pages that go with this layout.
                    var pages = await _dbContext.Templates.Where(t => t.LayoutId == model.Id).ToListAsync();
                    _dbContext.Templates.RemoveRange(pages);
                    _dbContext.Layouts.Remove(entity);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    ModelState.AddModelError("Id", "Cannot delete the default layout.");
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Gets a community layout
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> ImportCommunityLayout(string id)
        {
            try
            {
                if (_dbContext.Layouts.Any(c => c.CommunityLayoutId == id))
                {
                    throw new Exception("Layout already loaded.");
                }

                var utilities = new LayoutUtilities();
                var layout = await utilities.GetCommunityLayout(id, false);
                var communityPages = await utilities.GetCommunityTemplatePages(id);
                layout.IsDefault = (await _dbContext.Layouts.AnyAsync(a => a.IsDefault)) == false;
                _dbContext.Layouts.Add(layout);
                await _dbContext.SaveChangesAsync();

                if (communityPages != null && communityPages.Any())
                {
                    var pages = communityPages.Select(p => new Template()
                    {
                        CommunityLayoutId = id,
                        Content = p.Content,
                        Description = p.Description,
                        LayoutId = layout.Id,
                        Title = p.Title
                    });

                    _dbContext.Templates.AddRange(pages);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Id", ex.Message);
            }

            return RedirectToAction("Index");
        }

    }
}