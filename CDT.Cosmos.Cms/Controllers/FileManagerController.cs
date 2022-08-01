using Azure.Storage.Blobs.Specialized;
using CDT.Cosmos.BlobService;
using CDT.Cosmos.BlobService.Models;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CDT.Cosmos.Cms.Controllers
{

    /// <summary>
    /// File manager controller
    /// </summary>
    [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
    public class FileManagerController : BaseController
    {
        // Private fields
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ArticleEditLogic _articleLogic;
        private readonly Uri _blobPublicAbsoluteUrl;
        private readonly IViewRenderService _viewRenderService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="dbContext"></param>
        /// <param name="storageContext"></param>
        /// <param name="userManager"></param>
        /// <param name="articleLogic"></param>
        /// <param name="hostEnvironment"></param>
        /// <param name="viewRenderService"></param>
        public FileManagerController(IOptions<CosmosConfig> options,
            ILogger<FileManagerController> logger,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            IWebHostEnvironment hostEnvironment,
            IViewRenderService viewRenderService) : base(
            dbContext,
            userManager,
            articleLogic,
            options
        )
        {
            if (options.Value.SiteSettings.AllowSetup ?? true)
            {
                throw new Exception("Permission denied. Website in setup mode.");
            }
            _options = options;
            _logger = logger;
            _storageContext = storageContext;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
            _articleLogic = articleLogic;

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

        /// <summary>
        /// Index method
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            _storageContext.CreateFolder("/pub");
            ViewData["BlobEndpointUrl"] = GetBlobRootUrl();
            return View();
        }

        /// <summary>
        /// Imports a page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public IActionResult ImportPage(int? id)
        {
            if (id.HasValue)
            {
                ViewData["ArticleId"] = id.Value;
                return View();
            }
            return NotFound();
        }

        /// <summary>
        /// Import a view
        /// </summary>
        /// <param name="files"></param>
        /// <param name="metaData"></param>
        /// <param name="id">Article ID</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ImportPage(IEnumerable<IFormFile> files,
            string metaData, string id)
        {
            if (files == null || files.Any() == false || int.TryParse(id, out int Id) == false)
            {
                return null;
            }

            if (string.IsNullOrEmpty(metaData)) return Unauthorized("metaData cannot be null or empty.");

            //
            // Get information about the chunk we are on.
            //
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(metaData));

            var serializer = new JsonSerializer();
            FileUploadMetaData fileMetaData;
            using (var streamReader = new StreamReader(ms))
            {
                fileMetaData =
                    (FileUploadMetaData)serializer.Deserialize(streamReader, typeof(FileUploadMetaData));
            }

            if (fileMetaData == null) throw new Exception("Could not read the file's metadata");

            var uploadResult = new PageImportResult
            {
                uploaded = fileMetaData.TotalChunks - 1 <= fileMetaData.ChunkIndex,
                fileUid = fileMetaData.UploadUid
            };

            try
            {

                if (ModelState.IsValid)
                {
                    var article = await _articleLogic.Get(Id, EnumControllerName.Edit);

                    var originalHtml = await _articleLogic.ExportArticle(article, _blobPublicAbsoluteUrl, _viewRenderService);
                    var originalHtmlDoc = new HtmlAgilityPack.HtmlDocument();
                    originalHtmlDoc.LoadHtml(originalHtml);

                    var file = files.FirstOrDefault();
                    using var memstream = new MemoryStream();
                    await file.CopyToAsync(memstream);
                    var html = Encoding.UTF8.GetString(memstream.ToArray());

                    // Load the HTML document.
                    var newHtmlDoc = new HtmlAgilityPack.HtmlDocument();
                    newHtmlDoc.LoadHtml(html);

                    var originalHeadNode = originalHtmlDoc.DocumentNode.SelectSingleNode("//head");
                    var originalBodyNode = originalHtmlDoc.DocumentNode.SelectSingleNode("//body");

                    var layoutHeadNodes =
                        SelectNodesBetweenComments(originalHeadNode, PageImportConstants.COSMOS_HEAD_START, PageImportConstants.COSMOS_HEAD_END);
                    var layoutHeadScriptsNodes =
                        SelectNodesBetweenComments(originalHeadNode, PageImportConstants.COSMOS_HEAD_SCRIPTS_START, PageImportConstants.COSMOS_HEAD_SCRIPTS_END);
                    var layoutBodyHeaderNodes =
                        SelectNodesBetweenComments(originalBodyNode, PageImportConstants.COSMOS_BODY_HEADER_START, PageImportConstants.COSMOS_BODY_HEADER_END);
                    var layoutBodyFooterNodes =
                        SelectNodesBetweenComments(originalBodyNode, PageImportConstants.COSMOS_BODY_FOOTER_START, PageImportConstants.COSMOS_BODY_FOOTER_END);
                    var layoutBodyGoogleTranslateNodes =
                        SelectNodesBetweenComments(originalBodyNode, PageImportConstants.COSMOS_GOOGLE_TRANSLATE_START, PageImportConstants.COSMOS_GOOGLE_TRANSLATE_END);
                    var layoutBodyEndScriptsNodes =
                        SelectNodesBetweenComments(originalBodyNode, PageImportConstants.COSMOS_BODY_END_SCRIPTS_START, PageImportConstants.COSMOS_BODY_END_SCRIPTS_END);


                    // NOTES
                    // https://stackoverflow.com/questions/3844208/html-agility-pack-find-comment-node?msclkid=b885cfabc88011ecbf75531a66703f70
                    // https://html-agility-pack.net/knowledge-base/7275301/htmlagilitypack-select-nodes-between-comments?msclkid=b88685c7c88011ecbe703bfac7781d3c


                    var newHeadNode = newHtmlDoc.DocumentNode.SelectSingleNode("//head");
                    var newBodyNode = newHtmlDoc.DocumentNode.SelectSingleNode("//body");

                    // Now remove layout elements for the HEAD node
                    RemoveNodes(ref newHeadNode, layoutHeadNodes);
                    RemoveNodes(ref newHeadNode, layoutHeadScriptsNodes);

                    // Now remove layout elements for the BODY - Except layout footer
                    RemoveNodes(ref newBodyNode, layoutBodyHeaderNodes);
                    RemoveNodes(ref newBodyNode, layoutBodyGoogleTranslateNodes);
                    RemoveNodes(ref newBodyNode, layoutBodyEndScriptsNodes);

                    // Now capture nodes above and below footer within body
                    var exclude = new[] { HtmlAgilityPack.HtmlNodeType.Comment, HtmlAgilityPack.HtmlNodeType.Text };

                    var footerStartIndex = GetChildNodeIndex(newBodyNode, layoutBodyFooterNodes.FirstOrDefault(f => exclude.Contains(f.NodeType) == false));
                    var footerEndIndex = GetChildNodeIndex(newBodyNode, layoutBodyFooterNodes.LastOrDefault(f => exclude.Contains(f.NodeType) == false));

                    // Clean up the head inject
                    var headHtml = new StringBuilder();
                    foreach (var node in newHeadNode.ChildNodes)
                    {
                        if (node.NodeType != HtmlAgilityPack.HtmlNodeType.Comment &&
                           node.NodeType != HtmlAgilityPack.HtmlNodeType.Text)
                        {
                            headHtml.AppendLine(node.OuterHtml);
                        }
                    }

                    // Retrieve HTML above footer
                    var bodyHtmlAboveFooter = new StringBuilder();
                    for (int i = 0; i < footerStartIndex; i++)
                    {
                        if (newBodyNode.ChildNodes[i].NodeType != HtmlAgilityPack.HtmlNodeType.Comment &&
                            newBodyNode.ChildNodes[i].NodeType != HtmlAgilityPack.HtmlNodeType.Text)
                        {
                            bodyHtmlAboveFooter.AppendLine(newBodyNode.ChildNodes[i].OuterHtml);
                        }
                    }

                    // Retrieve HTML below footer
                    var bodyHtmlBelowFooter = new StringBuilder();
                    for (int i = footerEndIndex + 1; i < newBodyNode.ChildNodes.Count; i++)
                    {
                        if (newBodyNode.ChildNodes[i].NodeType != HtmlAgilityPack.HtmlNodeType.Comment &&
                               newBodyNode.ChildNodes[i].NodeType != HtmlAgilityPack.HtmlNodeType.Text)
                        {
                            bodyHtmlBelowFooter.AppendLine(newBodyNode.ChildNodes[i].OuterHtml);
                        }
                    }

                    var trims = new char[] { ' ', '\n', '\r' };

                    article.HeadJavaScript = headHtml.ToString().Trim(trims);
                    article.Content = bodyHtmlAboveFooter.ToString().Trim(trims);
                    article.FooterJavaScript = bodyHtmlBelowFooter.ToString().Trim(trims);

                    // Get the user's ID for logging.
                    var user = await _userManager.GetUserAsync(User);

                    await _articleLogic.UpdateOrInsert(article, user.Id);
                }
                else
                {
                    uploadResult.Errors = SerializeErrors(ModelState);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("file", e.Message);
                _logger.LogError("Web page import failed.", e);
                uploadResult.Errors = SerializeErrors(ModelState);
            }


            return Json(uploadResult);
        }

        private int GetChildNodeIndex(HtmlAgilityPack.HtmlNode parent, HtmlAgilityPack.HtmlNode child)
        {
            var target = parent.ChildNodes.FirstOrDefault(f => NodesAreEqual(f, child));
            if (target == null)
            {
                return -1;
            }
            var index = parent.ChildNodes.IndexOf(target);
            return index;
        }

        /// <summary>
        /// Removes nodes from a parent node by XPath.
        /// </summary>
        /// <param name="originalNode"></param>
        /// <param name="nodesToRemove"></param>
        private void RemoveNodes(ref HtmlAgilityPack.HtmlNode originalNode, IEnumerable<HtmlAgilityPack.HtmlNode> nodesToRemove)
        {
            foreach (var node in nodesToRemove)
            {
                var doomed = originalNode.ChildNodes.FirstOrDefault(w => NodesAreEqual(w, node));
                if (doomed != null)
                {
                    doomed.Remove();
                }
            }
        }

        /// <summary>
        /// Determines if nodes are equal
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <returns></returns>
        /// <remarks>Compares node name, node type, and attributes.</remarks>
        private bool NodesAreEqual(HtmlAgilityPack.HtmlNode node1, HtmlAgilityPack.HtmlNode node2)
        {
            if (node1.Name == node2.Name && node1.NodeType == node2.NodeType)
            {
                var attributeNames1 = node1.Attributes.Select(s => new
                {
                    Name = s.Name.ToLower(),
                    Value = s.Value
                }).OrderBy(o => o.Name).ToList();

                var attributeNames2 = node2.Attributes.Select(s => new
                {
                    Name = s.Name.ToLower(),
                    Value = s.Value
                }).OrderBy(o => o.Name).ToList();

                var firstNotInSecond = attributeNames1.Except(attributeNames2).ToList();
                var secondNotInFirst = attributeNames2.Except(attributeNames1).ToList();

                return firstNotInSecond.Count == 0 && secondNotInFirst.Count == 0;
            }
            return false;
        }

        /// <summary>
        /// Selects nodes between HTML comments
        /// </summary>
        /// <param name="originalNode"></param>
        /// <param name="startComment"></param>
        /// <param name="endComment"></param>
        /// <returns></returns>
        private IEnumerable<HtmlAgilityPack.HtmlNode> SelectNodesBetweenComments(HtmlAgilityPack.HtmlNode originalNode, string startComment, string endComment)
        {
            var nodes = new List<HtmlAgilityPack.HtmlNode>();

            startComment = startComment.Replace("<!--", "").Replace("-->", "").Trim();
            endComment = endComment.Replace("<!--", "").Replace("-->", "").Trim();

            var startNode = originalNode.SelectSingleNode($"//comment()[contains(., '{startComment}')]");
            var endNode = originalNode.SelectSingleNode($"//comment()[contains(., '{endComment}')]");

            if (startNode != null && endNode != null)
            {
                int startNodeIndex = startNode.ParentNode.ChildNodes.IndexOf(startNode);
                int endNodeIndex = endNode.ParentNode.ChildNodes.IndexOf(endNode);

                for (int i = startNodeIndex; i < endNodeIndex + 1; i++)
                {
                    nodes.Add(originalNode.ChildNodes[i]);
                }
            }
            else if (startNode != null && endNode == null)
            {
                throw new Exception($"End comment: '{endComment}' not found.");
            }
            else if (startNode == null && endNode != null)
            {
                throw new Exception($"Start comment: '{startComment}' not found.");
            }

            return nodes;
        }

        /// <summary>
        /// Opens the file manager without the tool bar.
        /// </summary>
        /// <param name="id">option id</param>
        /// <returns></returns>
        /// <remarks>
        /// This is suitable for opening the file manager as a popup.
        /// </remarks>
        public IActionResult Popup(string id)
        {
            _storageContext.CreateFolder("/pub");
            ViewData["BlobEndpointUrl"] = GetBlobRootUrl();
            ViewData["Popup"] = true;
            ViewData["option"] = id;
            return View("index");
        }

        #region PRIVATE FIELDS AND METHODS

        private readonly ILogger<FileManagerController> _logger;
        private readonly StorageContext _storageContext;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IOptions<CosmosConfig> _options;

        #endregion

        #region HELPER METHODS

        /// <summary>
        ///     Makes sure all root folders exist.
        /// </summary>
        /// <returns></returns>
        public void EnsureRootFoldersExist()
        {
            //await _storageContext.CreateFolderAsync("/");

            _storageContext.CreateFolder("/pub");
        }

        /// <summary>
        ///     Encodes a URL
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <remarks>
        ///     For more information, see
        ///     <a
        ///         href="https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata#blob-names">
        ///         documentation
        ///     </a>
        ///     .
        /// </remarks>
        public string UrlEncode(string path)
        {
            var parts = ParsePath(path);
            var urlEncodedParts = new List<string>();
            foreach (var part in parts) urlEncodedParts.Add(HttpUtility.UrlEncode(part.Replace(" ", "-")));

            return TrimPathPart(string.Join('/', urlEncodedParts));
        }

        #endregion

        #region FILE MANAGER FUNCTIONS

        /// <summary>
        ///     Creates a new entry, using relative path-ing, and normalizes entry name to lower case.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="entry"></param>
        /// <returns><see cref="JsonResult" />(<see cref="BlobService.FileManagerEntry" />)</returns>
        public async Task<ActionResult> Create(string target, BlobService.FileManagerEntry entry)
        {
            target = target == null ? "" : target.ToLower();
            entry.Path = target;
            entry.Name = UrlEncode(entry.Name.ToLower());
            entry.Extension = entry.Extension?.ToLower();

            if (!entry.Path.StartsWith("pub", StringComparison.CurrentCultureIgnoreCase))
            {
                return Unauthorized("New folders can't be created here using this tool. Please select the 'pub' folder and try again.");
            }

            // Check for duplicate entries
            var existingEntries = await _storageContext.GetFolderContents(target);

            if (existingEntries != null && existingEntries.Any())
            {
                var results = existingEntries.FirstOrDefault(f => f.Name.Equals(entry.Name));

                if (results != null)
                {
                    //var i = 1;
                    var originalName = entry.Name;
                    for (var i = 0; i < existingEntries.Count; i++)
                    {
                        entry.Name = originalName + "-" + (i + 1);
                        if (!existingEntries.Any(f => f.Name.Equals(entry.Name))) break;
                        i++;
                    }
                }
            }

            var fullPath = string.Join('/', ParsePath(entry.Path, entry.Name));
            fullPath = UrlEncode(fullPath);

            var fileManagerEntry = _storageContext.CreateFolder(fullPath);

            return Json(fileManagerEntry);
        }

        /// <summary>
        ///     Deletes a folder, normalizes entry to lower case.
        /// </summary>
        /// <param name="entry">Item to delete using relative path</param>
        /// <returns></returns>
        public async Task<ActionResult> Destroy(BlobService.FileManagerEntry entry)
        {
            var path = entry.Path.ToLower();

            if (entry.IsDirectory)
            {
                if (path == "pub")
                    return Unauthorized($"Cannot delete folder {path}.");
                await _storageContext.DeleteFolderAsync(path);
            }
            else
            {
                _storageContext.DeleteFile(path);
            }

            return Json(new object[0]);
        }

        /// <summary>
        ///     Read files for a given path, retuning <see cref="AppendBlobClient" />, not <see cref="BlockBlobClient" />.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fileType"></param>
        /// <returns>List of items found at target search, relative</returns>
        [HttpPost]
        public async Task<IActionResult> Read(string target, string fileType)
        {
            target = string.IsNullOrEmpty(target) ? "" : HttpUtility.UrlDecode(target);

            //
            // GET FULL OR ABSOLUTE PATH
            //
            var model = await _storageContext.GetFolderContents(target);

            //
            // OPTIONAL FILTER
            //
            if (!string.IsNullOrEmpty(fileType))
            {
                string[] fileExtensions = null;

                switch (fileType)
                {
                    case "f":
                        fileExtensions = AllowedFileExtensions
                            .GetFilterForViews(AllowedFileExtensions.ExtensionCollectionType.FileUploads).Split(',')
                            .Select(s => s.Trim().ToLower()).ToArray();
                        break;
                    case "i":
                        fileExtensions = AllowedFileExtensions
                            .GetFilterForViews(AllowedFileExtensions.ExtensionCollectionType.ImageUploads).Split(',')
                            .Select(s => s.Trim().ToLower()).ToArray();
                        break;
                }

                var filteredModel = new List<BlobService.FileManagerEntry>();

                foreach (var item in model)
                {
                    if (item.IsDirectory)
                    {
                        filteredModel.Add(item);
                    }
                    else if (fileExtensions.Contains(item.Extension.ToLower().TrimStart('.')))
                    {
                        filteredModel.Add(item);
                    }
                }

                return Json(filteredModel);
            }


            return Json(model);
        }

        /// <summary>
        /// File browser used by Kendo file manager.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IActionResult> ImageBrowserRead(string path)
        {
            return await FileBrowserRead(path, "i");
        }

        /// <summary>
        ///     File browser read used by Kendo editor
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileType"></param>
        /// <returns></returns>
        public async Task<IActionResult> FileBrowserRead(string path, string fileType = "f")
        {
            path = string.IsNullOrEmpty(path) ? "" : HttpUtility.UrlDecode(path);

            var model = await _storageContext.GetFolderContents(path);

            string[] fileExtensions = null;

            switch (fileType)
            {
                case "f":
                    fileExtensions = AllowedFileExtensions
                        .GetFilterForViews(AllowedFileExtensions.ExtensionCollectionType.FileUploads).Split(',')
                        .Select(s => s.Trim().ToLower()).ToArray();
                    break;
                case "i":
                    fileExtensions = AllowedFileExtensions
                        .GetFilterForViews(AllowedFileExtensions.ExtensionCollectionType.ImageUploads).Split(',')
                        .Select(s => s.Trim().ToLower()).ToArray();
                    break;
            }

            var jsonModel = new List<FileBrowserEntry>();

            foreach (var entry in model)
                if (entry.IsDirectory || fileExtensions == null)
                    jsonModel.Add(new FileBrowserEntry
                    {
                        EntryType = entry.IsDirectory ? FileBrowserEntryType.Directory : FileBrowserEntryType.File,
                        Name = $"{entry.Name}",
                        Size = entry.Size
                    });
                else if (fileExtensions.Contains(entry.Extension.TrimStart('.')))
                    jsonModel.Add(new FileBrowserEntry
                    {
                        EntryType = FileBrowserEntryType.File,
                        Name = $"{entry.Name}.{entry.Extension.TrimStart('.')}",
                        Size = entry.Size
                    });

            return Json(jsonModel.Select(s => new KendoFileBrowserEntry(s)).ToList());
        }

        /// <summary>
        /// Create an image thumbnail
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "path" })]
        public async Task<IActionResult> CreateThumbnail(string path)
        {
            path = string.IsNullOrEmpty(path) ? "" : HttpUtility.UrlDecode(path);

            //var searchPath = GetAbsolutePath(path);

            try
            {
                await using var fileStream = await _storageContext.OpenBlobReadStreamAsync(path);
                // 80 x 80
                var desiredSize = new ImageSizeModel();

                const string contentType = "image/png";

                var thumbnailCreator = new ThumbnailCreator();

                return File(thumbnailCreator.Create(fileStream, desiredSize, contentType), contentType);
            }
            catch
            {
                var filePath = Path.Combine(_hostEnvironment.WebRootPath, "images\\ImageIcon.png");
                await using var fileStream = System.IO.File.OpenRead(filePath);
                // 80 x 80
                var desiredSize = new ImageSizeModel();

                const string contentType = "image/png";

                var thumbnailCreator = new ThumbnailCreator();

                return File(thumbnailCreator.Create(fileStream, desiredSize, contentType), contentType);
            }
        }

        /// <summary>
        ///     Updates the name an entry with a given entry, normalize names to lower case.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns>An empty <see cref="ContentResult" />.</returns>
        /// <exception cref="Exception">Forbidden</exception>
        [HttpPost]
        public async Task<ActionResult> Update(BlobService.FileManagerEntry entry)
        {
            entry.Path = entry.Path?.ToLower();
            entry.Name = entry.Name?.ToLower();
            entry.Extension = entry.Extension?.ToLower();

            if (entry.Path == "pub")
                return Unauthorized($"Cannot rename folder {entry.Path}.");

            //var source = GetAbsolutePath(entry.Path);
            string newName;
            if (!string.IsNullOrEmpty(entry.Path) && entry.Path.Contains("/"))
            {
                var pathParts = entry.Path.Split("/");
                // For the following line, see example 3 here: https://stackoverflow.com/questions/3634099/c-sharp-string-array-replace-last-element
                pathParts[^1] = entry.Name!;
                newName = string.Join("/", pathParts);
            }
            else
            {
                newName = entry.Name;
            }

            if (!entry.IsDirectory)
            {
                if (!string.IsNullOrEmpty(entry.Extension) && !newName.ToLower().EndsWith(entry.Extension.ToLower()))
                {
                    newName = $"{newName}.{entry.Extension.TrimStart('.')}";
                }
            }

            // Encode using our own rules
            newName = TrimPathPart(UrlEncode(newName));


            if (entry.Path == "pub")
                throw new UnauthorizedAccessException($"Cannot rename folder {entry.Path}.");

            await _storageContext.RenameAsync(entry.Path, newName);

            // File manager is expecting the file name to come back without an extension.
            entry.Name = Path.GetFileNameWithoutExtension(newName);
            entry.Path = GetRelativePath(newName);
            entry.Extension = entry.IsDirectory || string.IsNullOrEmpty(entry.Extension) ? "" : entry.Extension;

            // Example: {"Name":"Wow","Size":0,"Path":"Wow","Extension":"","IsDirectory":true,"HasDirectories":false,"Created":"2020-10-30T18:14:16.0772789+00:00","CreatedUtc":"2020-10-30T18:14:16.0772789Z","Modified":"2020-10-30T18:14:16.0772789+00:00","ModifiedUtc":"2020-10-30T18:14:16.0772789Z"}
            return Json(entry);
        }

        #endregion

        #region UTILITY FUNCTIONS

        /// <summary>
        ///     Converts the full path from a blob, to a relative one useful for the file manager.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public string GetRelativePath(params string[] fullPath)
        {
            var rootPath = "";

            var absolutePath = string.Join('/', ParsePath(fullPath));

            if (absolutePath.ToLower().StartsWith(rootPath.ToLower()))
            {
                if (rootPath.Length == absolutePath.Length) return "";
                return TrimPathPart(absolutePath.Substring(rootPath.Length));
            }

            return TrimPathPart(absolutePath);
        }

        /// <summary>
        ///     Gets the public URL of the blob.
        /// </summary>
        /// <returns></returns>
        public string GetBlobRootUrl()
        {
            return $"{_options.Value.SiteSettings.BlobPublicUrl.TrimEnd('/')}/";
        }

        /// <summary>
        ///     Parses out a path into a string array.
        /// </summary>
        /// <param name="pathParts"></param>
        /// <returns></returns>
        public string[] ParsePath(params string[] pathParts)
        {
            if (pathParts == null) return new string[] { };

            var paths = new List<string>();

            foreach (var part in pathParts)
                if (!string.IsNullOrEmpty(part))
                {
                    var split = part.Split("/");
                    foreach (var p in split)
                        if (!string.IsNullOrEmpty(p))
                        {
                            var path = TrimPathPart(p);
                            if (!string.IsNullOrEmpty(path)) paths.Add(path);
                        }
                }

            return paths.ToArray();
        }

        /// <summary>
        ///     Trims leading and trailing slashes and white space from a path part.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public string TrimPathPart(string part)
        {
            if (string.IsNullOrEmpty(part))
                return "";

            return part.Trim('/').Trim('\\').Trim();
        }

        #endregion

        #region EDIT (CODE | IMAGE) FUNCTIONS

        /// <summary>
        /// Edit code for a file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditCode(string path)
        {
            try
            {
                var extension = Path.GetExtension(path.ToLower());

                var filter = _options.Value.SiteSettings.AllowedFileTypes.Split(',');
                var editorField = new EditorField
                {
                    FieldId = "Content",
                    FieldName = Path.GetFileName(path)
                };

                if (!filter.Contains(extension)) return new UnsupportedMediaTypeResult();

                switch (extension)
                {
                    case ".js":
                        editorField.EditorMode = EditorMode.JavaScript;
                        editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                        break;
                    case ".css":
                        editorField.EditorMode = EditorMode.Css;
                        editorField.IconUrl = "/images/seti-ui/icons/css.svg";
                        break;
                    default:
                        editorField.EditorMode = EditorMode.Html;
                        editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                        break;
                }

                //
                // Get the blob now, so we can determine the type, or use this client as-is
                //
                //var properties = blob.GetProperties();

                // Open a stream
                await using var memoryStream = new MemoryStream();

                await using (var stream = await _storageContext.OpenBlobReadStreamAsync(path))
                {
                    // Load into memory and release the blob stream right away
                    await stream.CopyToAsync(memoryStream);
                }

                return View(new FileManagerEditCodeViewModel
                {
                    Id = 1,
                    Path = path,
                    EditorTitle = Path.GetFileName(Path.GetFileName(path)),
                    EditorFields = new List<EditorField>
                    {
                        editorField
                    },
                    Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
                    EditingField = "Content",
                    CustomButtons = new List<string>()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// Save the file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCode(FileManagerEditCodeViewModel model)
        {

            var extension = Path.GetExtension(model.Path.ToLower());

            var filter = _options.Value.SiteSettings.AllowedFileTypes.Split(',');
            var editorField = new EditorField
            {
                FieldId = "Content",
                FieldName = Path.GetFileName(model.Path)
            };

            if (!filter.Contains(extension)) return new UnsupportedMediaTypeResult();

            var contentType = string.Empty;

            switch (extension)
            {
                case ".js":
                    editorField.EditorMode = EditorMode.JavaScript;
                    editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                    contentType = "application/javascript";
                    break;
                case ".css":
                    editorField.EditorMode = EditorMode.Css;
                    editorField.IconUrl = "/images/seti-ui/icons/css.svg";
                    contentType = "text/css";
                    break;
                case ".htm":
                case ".html":
                    editorField.EditorMode = EditorMode.Html;
                    editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                    contentType = "text/html";
                    break;
                default:
                    editorField.EditorMode = EditorMode.Html;
                    editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                    contentType = "text/plain";
                    break;
            }

            // Save the blob now

            var bytes = Encoding.Default.GetBytes(model.Content);

            using var memoryStream = new MemoryStream(bytes, false);
            
            var formFile = new FormFile(memoryStream, 0, memoryStream.Length, Path.GetFileNameWithoutExtension(model.Path), Path.GetFileName(model.Path));

            var metaData = new FileUploadMetaData
            {
                ChunkIndex = 0,
                ContentType = contentType,
                FileName = Path.GetFileName(model.Path),
                RelativePath = Path.GetFileName(model.Path),
                TotalFileSize = memoryStream.Length,
                UploadUid = Guid.NewGuid().ToString(),
                TotalChunks = 1
            };

            var uploadPath = model.Path.TrimEnd(metaData.FileName.ToArray()).TrimEnd('/');

            var result = (JsonResult) await Upload(new IFormFile[] { formFile }, JsonConvert.SerializeObject(metaData), uploadPath);

            var resultMode = (FileUploadResult)result.Value;

            var jsonModel = new SaveCodeResultJsonModel
            {
                ErrorCount = ModelState.ErrorCount,
                IsValid = ModelState.IsValid
            };

            if (!resultMode.uploaded)
            {
                ModelState.AddModelError("", $"Error saving {Path.GetFileName(model.Path)}");
            }

            jsonModel.Errors.AddRange(ModelState.Values
                .Where(w => w.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                .ToList());
            jsonModel.ValidationState = ModelState.ValidationState;

            return Json(jsonModel);
        }

        /// <summary>
        /// Edit an image
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditImage(string path)
        {
            var extension = Path.GetExtension(path.ToLower());

            var filter = new[] { ".png", ".jpg", ".gif", ".jpeg" };
            if (filter.Contains(extension))
            {
                EditorMode mode;
                switch (extension)
                {
                    case ".js":
                        mode = EditorMode.JavaScript;
                        break;
                    case ".css":
                        mode = EditorMode.Css;
                        break;
                    default:
                        mode = EditorMode.Html;
                        break;
                }

                // Open a stream
                await using var memoryStream = new MemoryStream();

                await using (var stream = await _storageContext.OpenBlobReadStreamAsync(path))
                {
                    // Load into memory and release the blob stream right away
                    await stream.CopyToAsync(memoryStream);
                }

                return View(new FileManagerEditCodeViewModel
                {
                    Id = 1,
                    Path = path,
                    EditorTitle = Path.GetFileName(Path.GetFileName(path)),
                    EditorFields = new List<EditorField>
                    {
                        new()
                        {
                            FieldId = "Content",
                            FieldName = "Html Content",
                            EditorMode = mode
                        }
                    },
                    Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
                    EditingField = "Content",
                    CustomButtons = new List<string>()
                });
            }

            return new UnsupportedMediaTypeResult();
        }

        #endregion

        #region UPLOADER FUNCTIONS

        /// <summary>
        ///     Removes a file
        /// </summary>
        /// <param name="fileNames"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public ActionResult Remove(string[] fileNames, string path)
        {
            // Return an empty string to signify success
            return Content("");
        }

        /// <summary>
        ///     Used to directories, with files processed one chunk at a time, and normalizes the blob name to lower case.
        /// </summary>
        /// <param name="folders"></param>
        /// <param name="metaData"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpPost]
        [RequestSizeLimit(
            6291456)] // AWS S3 multi part upload requires 5 MB parts--no more, no less so pad the upload size by a MB just in case
        public async Task<ActionResult> UploadDirectory(IEnumerable<IFormFile> folders,
            string metaData, string path)
        {
            return await Upload(folders, metaData, path);
        }
        /// <summary>
        ///     Used to upload files, one chunk at a time, and normalizes the blob name to lower case.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="metaData"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpPost]
        [RequestSizeLimit(
            6291456)] // AWS S3 multi part upload requires 5 MB parts--no more, no less so pad the upload size by a MB just in case
        public async Task<ActionResult> Upload(IEnumerable<IFormFile> files,
            string metaData, string path)
        {
            try
            {
                if (files == null || files.Any() == false)
                    return Json("");

                if (string.IsNullOrEmpty(path) || path.Trim('/') == "") return Unauthorized("Cannot upload here. Please select the 'pub' folder first, or sub-folder below that, then try again.");

                //
                // Get information about the chunk we are on.
                //
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(metaData));

                var serializer = new JsonSerializer();
                FileUploadMetaData fileMetaData;
                using (var streamReader = new StreamReader(ms))
                {
                    fileMetaData =
                        (FileUploadMetaData)serializer.Deserialize(streamReader, typeof(FileUploadMetaData));
                }

                if (fileMetaData == null) throw new Exception("Could not read the file's metadata");

                var file = files.FirstOrDefault();

                if (file == null) throw new Exception("No file found to upload.");

                var blobName = UrlEncode(fileMetaData.FileName.ToLower());

                fileMetaData.FileName = blobName.ToLower();
                fileMetaData.RelativePath = (path.TrimEnd('/') + "/" + fileMetaData.RelativePath).ToLower();

                // Make sure full folder path exists
                var parts = fileMetaData.RelativePath.ToLower().Trim('/').Split('/');
                var part = "";
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (i == 0 && parts[i] != "pub")
                    {
                        throw new Exception("Must upload folders and files under /pub directory.");
                    }

                    part = $"{part}/{parts[i].ToLower()}";
                    if (part != "/pub")
                    {
                        var folder = part.Trim('/');
                        _storageContext.CreateFolder(folder);
                    }
                }

                await using (var stream = file.OpenReadStream())
                {
                    await using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        _storageContext.AppendBlob(memoryStream, fileMetaData);
                    }
                }

                var fileBlob = new FileUploadResult
                {
                    uploaded = fileMetaData.TotalChunks - 1 <= fileMetaData.ChunkIndex,
                    fileUid = fileMetaData.UploadUid
                };
                return Json(fileBlob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw ex;
            }
        }

        #endregion
    }

    /// <summary>
    /// Page import constants
    /// </summary>
    public static class PageImportConstants
    {
        /// <summary>
        /// Marks the start of the head injection
        /// </summary>
        public const string COSMOS_HEAD_START = "<!--  BEGIN: Cosmos Layout HEAD content inject (not editable). -->";
        /// <summary>
        /// Marks the end of the head injection
        /// </summary>
        public const string COSMOS_HEAD_END = "<!--  END: Cosmos HEAD inject (not editable). -->";
        /// <summary>
        /// Marks the beginning of the optional head script injection
        /// </summary>
        public const string COSMOS_HEAD_SCRIPTS_START = "<!-- BEGIN: Optional Cosmos script section injected (not editable). -->";
        /// <summary>
        /// Marks the end of the optional head script injection
        /// </summary>
        public const string COSMOS_HEAD_SCRIPTS_END = "<!-- END: Optional Cosmos script section injected  (not editable). -->";
        /// <summary>
        /// Marks the beginning of the header injection
        /// </summary>
        public const string COSMOS_BODY_HEADER_START = "<!-- BEGIN: Cosmos Layout BODY HEADER content (not editable) -->";
        /// <summary>
        /// Marks the end of the header injection
        /// </summary>
        public const string COSMOS_BODY_HEADER_END = "<!-- END: Cosmos Layout BODY HEADER content (not editable) -->";
        /// <summary>
        /// Marks the start of the footer injection
        /// </summary>
        public const string COSMOS_BODY_FOOTER_START = "<!-- BEGIN: Cosmos Layout BODY FOOTER (not editable) -->";
        /// <summary>
        /// Marks the end of the footer injection
        /// </summary>
        public const string COSMOS_BODY_FOOTER_END = "<!-- END: Cosmos Layout BODY FOOTER (not editable) -->";
        /// <summary>
        /// Marks the start of Google Translate injection
        /// </summary>
        public const string COSMOS_GOOGLE_TRANSLATE_START = "<!-- BEGIN: Google Translate v3 (not editable) -->";
        /// <summary>
        /// Marks the endo of Google Translate injection
        /// </summary>
        public const string COSMOS_GOOGLE_TRANSLATE_END = "<!-- END: Google Translate v3 (not editable) -->";
        /// <summary>
        /// Marks the start of the end-of-body script injection
        /// </summary>
        public const string COSMOS_BODY_END_SCRIPTS_START = "<!-- BEGIN: Optional Cosmos script section injected (not editable). -->";
        /// <summary>
        /// Marks the end of the end-of-body script injection
        /// </summary>
        public const string COSMOS_BODY_END_SCRIPTS_END = "<!-- END: Optional Cosmos script section (not editable). -->";
    }

    /// <summary>
    /// Layout import marker constants
    /// </summary>
    public static class LayoutImportConstants
    {
        /// <summary>
        /// Marks the start of the head injection
        /// </summary>
        public const string COSMOS_HEAD_START = "<!--  BEGIN: Cosmos Layout HEAD content. -->";
        /// <summary>
        /// Marks the end of the head injection
        /// </summary>
        public const string COSMOS_HEAD_END = "<!--  END: Cosmos Layout HEAD content. -->";
        /// <summary>
        /// Marks the beginning of the header injection
        /// </summary>
        public const string COSMOS_BODY_HEADER_START = "<!-- BEGIN: Cosmos Layout BODY HEADER content -->";
        /// <summary>
        /// Marks the end of the header injection
        /// </summary>
        public const string COSMOS_BODY_HEADER_END = "<!-- END: Cosmos Layout BODY HEADER content -->";
        /// <summary>
        /// Marks the start of the footer injection
        /// </summary>
        public const string COSMOS_BODY_FOOTER_START = "<!-- BEGIN: Cosmos Layout BODY FOOTER content -->";
        /// <summary>
        /// Marks the end of the footer injection
        /// </summary>
        public const string COSMOS_BODY_FOOTER_END = "<!-- END: Cosmos Layout BODY FOOTER content -->";
    }
}