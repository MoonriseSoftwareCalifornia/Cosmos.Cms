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

        // Page import constants
        private const string COSMOS_HEAD_START = "<!-- BEGIN: page-specific head content (editable) -->";
        private const string COSMOS_HEAD_END = "<!-- END: page-specific head content (editable) -->";
        private const string COSMOS_BODY_START = "<!-- BEGIN: Page specific BODY content goes here (editable) -->";
        private const string COSMOS_BODY_END = "<!-- END: Page specific BODY content (editable) -->";
        private const string COSMOS_FOOTER_START = "<!-- BEGIN: Page specific *end* of BODY content goes here (editable) -->";
        private const string COSMOS_FOOTER_END = "<!-- END: Page specific *end* of BODY content  (editable) -->";

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
        public FileManagerController(IOptions<CosmosConfig> options,
            ILogger<FileManagerController> logger,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            IWebHostEnvironment hostEnvironment) : base(
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
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ImportPage(IEnumerable<IFormFile> files,
            string metaData, string path)
        {
            if (files == null || files.Any() == false || int.TryParse(path, out int Id) == false)
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

            try
            {

                var file = files.FirstOrDefault();
                using var memstream = new MemoryStream();
                await file.CopyToAsync(memstream);
                var html = Encoding.UTF8.GetString(memstream.ToArray());

                // Validate page can be parsed out
                var totalLength = html.Length;
                var cosmosHeadStart = html.IndexOf(COSMOS_HEAD_START) + COSMOS_HEAD_START.Length;
                var cosmosHeadEnd = html.IndexOf(COSMOS_HEAD_END);
                var cosmosBodyStart = html.IndexOf(COSMOS_BODY_START) + COSMOS_BODY_START.Length;
                var cosmosBodyEnd = html.IndexOf(COSMOS_BODY_END);
                var cosmosFooterStart = html.IndexOf(COSMOS_FOOTER_START) + COSMOS_FOOTER_START.Length;
                var cosmosFooterEnd = html.IndexOf(COSMOS_FOOTER_END);

                if (cosmosHeadStart == -1)
                {
                    ModelState.AddModelError("", $"Could not find {COSMOS_HEAD_START}");
                }
                if (cosmosHeadEnd == -1)
                {
                    ModelState.AddModelError("", $"Could not find  {COSMOS_HEAD_END}");
                }
                if (cosmosBodyStart == -1)
                {
                    ModelState.AddModelError("", $"Could not find  {COSMOS_BODY_START}");
                }
                if (cosmosBodyEnd == -1)
                {
                    ModelState.AddModelError("", $"Could not find  {COSMOS_BODY_END}");
                }
                if (cosmosFooterStart == -1)
                {
                    ModelState.AddModelError("", $"Could not find  {COSMOS_FOOTER_START}");
                }
                if (cosmosFooterEnd == -1)
                {
                    ModelState.AddModelError("", $"Could not find  {COSMOS_FOOTER_END}");
                }
                if (ModelState.IsValid)
                {
                    var article = await _articleLogic.Get(Id, EnumControllerName.Edit);

                    var pageHead = html.Substring(cosmosHeadStart, cosmosHeadEnd - cosmosHeadStart);
                    var pageBody = html.Substring(cosmosBodyStart, cosmosBodyEnd - cosmosBodyStart);
                    var pageFooter = html.Substring(cosmosFooterStart, cosmosFooterEnd - cosmosFooterStart);

                    article.HeaderJavaScript = pageHead;
                    article.Content = pageBody;
                    article.FooterJavaScript = pageFooter;

                    // Get the user's ID for logging.
                    var user = await _userManager.GetUserAsync(User);

                    await _articleLogic.UpdateOrInsert(article, user.Id);
                }
            }
            catch (Exception e)
            {
                var t = e; // Debugging
            }


            var fileBlob = new FileUploadResult
            {
                uploaded = fileMetaData.TotalChunks - 1 <= fileMetaData.ChunkIndex,
                fileUid = fileMetaData.UploadUid
            };

            return Json(fileBlob);
        }


        /// <summary>
        /// Opens the file manager without the toolbar.
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

        ///// <summary>
        ///// Edit code for a file
        ///// </summary>
        ///// <param name="path"></param>
        ///// <returns></returns>
        //public async Task<IActionResult> EditCode(string path)
        //{
        //    try
        //    {
        //        var extension = Path.GetExtension(path.ToLower());

        //        var filter = _options.Value.SiteSettings.AllowedFileTypes.Split(',');
        //        var editorField = new EditorField
        //        {
        //            FieldId = "Content",
        //            FieldName = Path.GetFileName(path)
        //        };

        //        if (!filter.Contains(extension)) return new UnsupportedMediaTypeResult();

        //        switch (extension)
        //        {
        //            case ".js":
        //                editorField.EditorMode = EditorMode.JavaScript;
        //                editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
        //                break;
        //            case ".css":
        //                editorField.EditorMode = EditorMode.Css;
        //                editorField.IconUrl = "/images/seti-ui/icons/css.svg";
        //                break;
        //            default:
        //                editorField.EditorMode = EditorMode.Html;
        //                editorField.IconUrl = "/images/seti-ui/icons/html.svg";
        //                break;
        //        }

        //        //
        //        // Get the blob now, so we can determine the type, or use this client as-is
        //        //
        //        //var properties = blob.GetProperties();

        //        // Open a stream
        //        await using var memoryStream = new MemoryStream();

        //        await using (var stream = await _storageContext.OpenBlobReadStreamAsync(path))
        //        {
        //            // Load into memory and release the blob stream right away
        //            await stream.CopyToAsync(memoryStream);
        //        }

        //        return View(new FileManagerEditCodeViewModel
        //        {
        //            Id = 1,
        //            Path = path,
        //            EditorTitle = Path.GetFileName(Path.GetFileName(path)),
        //            EditorFields = new List<EditorField>
        //            {
        //                editorField
        //            },
        //            Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
        //            EditingField = "Content",
        //            CustomButtons = new List<string>()
        //        });
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e.Message, e);
        //        throw;
        //    }
        //}
        ///// <summary>
        ///// Edit an image
        ///// </summary>
        ///// <param name="path"></param>
        ///// <returns></returns>
        //public async Task<IActionResult> EditImage(string path)
        //{
        //    var extension = Path.GetExtension(path.ToLower());

        //    var filter = new[] { ".png", ".jpg", ".gif", ".jpeg" };
        //    if (filter.Contains(extension))
        //    {
        //        EditorMode mode;
        //        switch (extension)
        //        {
        //            case ".js":
        //                mode = EditorMode.JavaScript;
        //                break;
        //            case ".css":
        //                mode = EditorMode.Css;
        //                break;
        //            default:
        //                mode = EditorMode.Html;
        //                break;
        //        }

        //        // Open a stream
        //        await using var memoryStream = new MemoryStream();

        //        await using (var stream = await _storageContext.OpenBlobReadStreamAsync(path))
        //        {
        //            // Load into memory and release the blob stream right away
        //            await stream.CopyToAsync(memoryStream);
        //        }

        //        return View(new FileManagerEditCodeViewModel
        //        {
        //            Id = 1,
        //            Path = path,
        //            EditorTitle = Path.GetFileName(Path.GetFileName(path)),
        //            EditorFields = new List<EditorField>
        //            {
        //                new()
        //                {
        //                    FieldId = "Content",
        //                    FieldName = "Html Content",
        //                    EditorMode = mode
        //                }
        //            },
        //            Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
        //            EditingField = "Content",
        //            CustomButtons = new List<string>()
        //        });
        //    }

        //    return new UnsupportedMediaTypeResult();
        //}

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
            if (files == null || files.Any() == false)
                return Json("");

            if (string.IsNullOrEmpty(path) || path.Trim('/') == "") return Unauthorized("Cannot upload here. Please select the 'pub' folder first, or subfolder below that, then try again.");
                        
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


        #endregion
    }
}