using Amazon;
using Amazon.S3;
using Azure.Storage.Blobs;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Common.Services.Configurations.Storage;
using CDT.Cosmos.Cms.Data;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using CDT.Cosmos.Cms.Services.Secrets;
using Kendo.Mvc.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Controllers
{
    /// <summary>
    /// Controller used for Cosmos setup.
    /// </summary>
    [Authorize(Roles = "Administrators")]
    public class SetupController : Controller
    {
        private readonly ILogger<SetupController> _logger;
        private readonly IOptions<CosmosConfig> _options;
        private readonly CosmosConfigStatus _cosmosStatus;
        private readonly SecretsClient _secretsClient;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        /// <param name="cosmosStatus"></param>
        /// <param name="secretsClient"></param>
        public SetupController(ILogger<SetupController> logger,
            IOptions<CosmosConfig> options,
            CosmosConfigStatus cosmosStatus,
            SecretsClient secretsClient
        )
        {
            _logger = logger;
            _options = options;
            _cosmosStatus = cosmosStatus;
            _secretsClient = secretsClient;
        }

        private bool CanUseSecretClient()
        {
            return CanUseConfigWizard() && _options.Value.SiteSettings.AllowConfigEdit;
        }

        private bool CanUseConfigWizard()
        {
            if (User.Identity.IsAuthenticated)
            {
                var connection = _options.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);
                using var dbContext = GetDbContext(connection.ToString());
                using var userManager = GetUserManager(dbContext);
                using var roleManager = GetRoleManager(dbContext);
                var user = userManager.GetUserAsync(User).Result;
                var authorized = userManager.IsInRoleAsync(user, "Administrators").Result;
                return authorized;
            }

            return _options.Value.SiteSettings.AllowSetup ?? false && _options.Value.SiteSettings.AllowConfigEdit;
        }

        /// <summary>
        ///     Loads JSON strings into configuration model
        /// </summary>
        /// <param name="model"></param>
        private void LoadJson(ConfigureIndexViewModel model)
        {
            #region LOAD BLOB CONNECTIONS

            model.StorageConfig = new StorageConfig();

            if (!string.IsNullOrEmpty(model.AwsS3ConnectionsJson))
            {
                var data = JsonConvert.DeserializeObject<AmazonStorageConfig[]>(model.AwsS3ConnectionsJson);
                if (data != null && data.Length > 0) model.StorageConfig.AmazonConfigs.AddRange(data);
            }

            if (!string.IsNullOrEmpty(model.AzureBlobConnectionsJson))
            {
                var data = JsonConvert.DeserializeObject<AzureStorageConfig[]>(model.AzureBlobConnectionsJson);
                if (data != null && data.Length > 0) model.StorageConfig.AzureConfigs.AddRange(data);
            }

            if (!string.IsNullOrEmpty(model.GoogleBlobConnectionsJson))
            {
                var data = JsonConvert.DeserializeObject<GoogleStorageConfig[]>(model.GoogleBlobConnectionsJson);
                if (data != null && data.Length > 0) model.StorageConfig.GoogleConfigs.AddRange(data);
            }

            #endregion

            #region LOAD EDITOR URLS

            if (!string.IsNullOrEmpty(model.EditorUrlsJson))
            {
                var data = JsonConvert.DeserializeObject<EditorUrl[]>(model.EditorUrlsJson);
                model.EditorUrls.AddRange(data);
            }

            #endregion

            #region LOAD SQL Connection Strings

            if (!string.IsNullOrEmpty(model.SqlConnectionsJson))
            {
                var data = JsonConvert.DeserializeObject<SqlConnectionString[]>(model.SqlConnectionsJson);
                model.SqlConnectionStrings.AddRange(data);
            }

            #endregion

        }

        /// <summary>
        /// Setup index page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var connectionString = _options.Value?.SqlConnectionStrings?.FirstOrDefault();

            if (connectionString == null)
            {
                ViewBag.ConnectionStringsMissing = true;
            }
            else
            {
                ViewBag.ConnectionStringsMissing = false;
            }

            using var dbContext = GetDbContext(connectionString.ToString());

            var migrations = await dbContext.Database.GetAppliedMigrationsAsync();

            if (migrations.Any())
            {
                ViewBag.NoMigrations = false;
                var pending = await dbContext.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                {
                    ViewBag.PendingMigrations = true;
                }
                else
                {
                    ViewBag.PendingMigrations = false;
                }
            }
            else
            {
                ViewBag.NoMigrations = true;
            }

            return View();
        }

        /// <summary>
        ///     Configuration wizard
        /// </summary>
        /// <param name="id">Secret name to read from.</param>
        /// <returns></returns>
        public async Task<IActionResult> ConfigWizard(string id)
        {
            if (CanUseConfigWizard())
            {
                ViewData["CanSaveToSecretsManager"] = _options.Value.SiteSettings.AllowConfigEdit && _secretsClient.IsConfigured;

                if (_options.Value == null || _options.Value.SiteSettings == null) // Setup allowed but user not an adminstrator
                    return View(new ConfigureIndexViewModel());


                CosmosConfig config;

                if (string.IsNullOrEmpty(id))
                {
                    id = _options.Value.SecretName;
                    config = _options.Value;
                }
                else
                {
                    var json = await _secretsClient.GetSecret(id);
                    if (string.IsNullOrEmpty(json))
                    {
                        config = _options.Value;
                        config.SecretName = id;
                    }
                    else
                    {
                        config = JsonConvert.DeserializeObject<CosmosConfig>(json);
                    }
                }

                return View(new ConfigureIndexViewModel(id, config));
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        /// <summary>
        /// Configuration wizard post back
        /// </summary>
        /// <param name="model"></param>
        /// <returns>SetupIndexViewModel</returns>
        [HttpPost]
        [ResponseCache(NoStore = true)]
        public async Task<IActionResult> ConfigWizard(ConfigureIndexViewModel model)
        {
            model.PrimaryCloud = string.IsNullOrEmpty(_options.Value.PrimaryCloud) ? "" : _options.Value.PrimaryCloud.ToLower();

            if (CanUseConfigWizard())
            {

                ViewData["CanSaveToSecretsManager"] = _options.Value.SiteSettings.AllowConfigEdit && _secretsClient.IsConfigured;

                if (!string.IsNullOrEmpty(model.ImportJson))
                {
                    ModelState.Clear();
                    var config = JsonConvert.DeserializeObject<CosmosConfig>(model.ImportJson);
                    model = new ConfigureIndexViewModel(_options.Value?.SecretName, config);
                    ViewData["SkipBegin"] = true;
                    return View(model);
                }

                // Load JSON
                LoadJson(model);

                #region BLOB CONNECTIONS

                if (!model.StorageConfig.AmazonConfigs.Any()
                    && !model.StorageConfig.AzureConfigs.Any()
                    && !model.StorageConfig.GoogleConfigs.Any())
                    ModelState.AddModelError("StorageConfig", "At least one storage account required.");

                #endregion

                #region

                if (model.EditorUrls.Count < 1)
                    ModelState.AddModelError("EditorUrlsJson", "Must have at least one Editor Url.");

                #endregion

                #region SQL Connection Strings

                if (model.SqlConnectionStrings.Count < 1)
                {
                    ModelState.AddModelError("SqlConnectionsJson", "Must have at least one connection.");
                }
                else
                {
                    if (model.SqlConnectionStrings.Count(a => a.IsPrimary) != 1)
                        ModelState.AddModelError("SqlConnectionsJson", "Make one connection primary.");

                    foreach (var connectionString in model.SqlConnectionStrings)
                        if (string.IsNullOrEmpty(connectionString.Password) ||
                            string.IsNullOrEmpty(connectionString.CloudName) ||
                            string.IsNullOrEmpty(connectionString.Hostname) ||
                            string.IsNullOrEmpty(connectionString.InitialCatalog) ||
                            string.IsNullOrEmpty(connectionString.Password) ||
                            string.IsNullOrEmpty(connectionString.UserId)
                        )
                        {
                            ModelState.AddModelError("SqlConnectionsJson", "Required DB connection entry is missing.");
                            break;
                        }
                }

                #endregion

                #region AUTHENTICATION CONFIGURATION

                if (model.AuthenticationConfig.Facebook == null
                    &&
                    model.AuthenticationConfig.Google == null
                    &&
                    model.AuthenticationConfig.Microsoft == null
                )
                    ModelState.AddModelError("AuthenticationConfig.AllowLocalRegistration",
                        "Local registration required when another is not defined.");

                if (model.AuthenticationConfig.Facebook != null &&
                    (string.IsNullOrEmpty(model.AuthenticationConfig.Facebook.AppId) == false ||
                     string.IsNullOrEmpty(model.AuthenticationConfig.Facebook.AppSecret) == false))
                {
                    if (string.IsNullOrEmpty(model.AuthenticationConfig.Facebook.AppId))
                        ModelState.AddModelError("AuthenticationConfig.Facebook.AppId", "Facebook App Id required.");

                    if (string.IsNullOrEmpty(model.AuthenticationConfig.Facebook.AppSecret))
                        ModelState.AddModelError("AuthenticationConfig.Facebook.AppSecret",
                            "Facebook App secret required.");
                }

                if (model.AuthenticationConfig.Google != null &&
                    (string.IsNullOrEmpty(model.AuthenticationConfig.Google.ClientId) == false ||
                     string.IsNullOrEmpty(model.AuthenticationConfig.Google.ClientSecret) == false))
                {
                    if (string.IsNullOrEmpty(model.AuthenticationConfig.Google.ClientId))
                        ModelState.AddModelError("AuthenticationConfig.Google.ClientId", "Google client Id required.");

                    if (string.IsNullOrEmpty(model.AuthenticationConfig.Google.ClientSecret))
                        ModelState.AddModelError("AuthenticationConfig.Google.ClientSecret",
                            "Google client secret required.");
                }

                if (model.AuthenticationConfig.Microsoft != null &&
                    (string.IsNullOrEmpty(model.AuthenticationConfig.Microsoft.ClientId) == false ||
                     string.IsNullOrEmpty(model.AuthenticationConfig.Microsoft.ClientSecret) == false))
                {
                    if (string.IsNullOrEmpty(model.AuthenticationConfig.Microsoft.ClientId))
                        ModelState.AddModelError("AuthenticationConfig.Microsoft.ClientId",
                            "Microsoft client Id required.");

                    if (string.IsNullOrEmpty(model.AuthenticationConfig.Microsoft.ClientSecret))
                        ModelState.AddModelError("AuthenticationConfig.Microsoft.ClientSecret",
                            "Microsoft client secret required.");
                }

                #endregion

                #region AKAMAI CDN CONFIGURATION

                if (model.CdnConfig.AkamaiContextConfig != null &&
                    (
                        string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.AccessToken) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.AkamaiHost) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.ClientToken) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.CpCode) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.Secret) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.UrlRoot) == false
                    ))
                {
                    if (string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.AccessToken))
                        ModelState.AddModelError("CdnConfig.AkamaiContextConfig.AccessToken",
                            "Akamai CDN Access Token cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.AkamaiHost))
                        ModelState.AddModelError("CdnConfig.AkamaiContextConfig.AkamaiHost",
                            "Akamai CDN Akamai Host cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.ClientToken))
                        ModelState.AddModelError("CdnConfig.AkamaiContextConfig.ClientToken",
                            "Akamai CDN Client Token cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.CpCode))
                        ModelState.AddModelError("CdnConfig.AkamaiContextConfig.CpCode",
                            "Akamai CDN CP Code cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.Secret))
                        ModelState.AddModelError("CdnConfig.AkamaiContextConfig.Secret",
                            "Akamai CDN Secret cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AkamaiContextConfig.UrlRoot))
                        ModelState.AddModelError("CdnConfig.AkamaiContextConfig.UrlRoot",
                            "Akamai CDN UrlRoot cannot be empty.");
                }

                #endregion

                #region AZURE CDN CONFIGURATION

                if (model.CdnConfig.AzureCdnConfig != null &&
                    (
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientSecret) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProvider) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantId) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProfileName) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientId) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.EndPointName) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ResourceGroup) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.SubscriptionId) == false ||
                        string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantDomainName) == false
                    )
                )
                {
                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientSecret))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.ClientSecret",
                            "Azure CDN Client Secret cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProvider))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.CdnProvider",
                            "Azure CDN Provider cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantId))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.TenantId",
                            "Azure CDN Tenant Id cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProfileName))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.CdnProfileName",
                            "Azure CDN Profile Name cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientId))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.ClientId",
                            "Azure CDN Client Id cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.EndPointName))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.EndPointName",
                            "Azure CDN End Point Name cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ResourceGroup))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.ResourceGroup",
                            "Azure CDN Resource Group cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.SubscriptionId))
                        ModelState.AddModelError("CdnConfig.AzureCdnConfig.SubscriptionId",
                            "Azure CDN Subscription Id cannot be empty.");

                    if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantDomainName))
                        ModelState.AddModelError("model.CdnConfig.AzureCdnConfig.TenantDomainName",
                            "Azure CDN Tenant Domain Name cannot be empty.");
                }

                #endregion

                var outputModel = new ConfigureIndexProcessedModel
                {
                    ModelState = ModelState
                };

                if (ModelState.IsValid)
                {
                    var obj = new CosmosConfig
                    {
                        AuthenticationConfig = model.AuthenticationConfig,
                        CdnConfig = model.CdnConfig,
                        GoogleCloudAuthConfig = model.GoogleCloudAuthConfig,
                        SendGridConfig = model.SendGridConfig,
                        SiteSettings = model.SiteSettings,
                        SqlConnectionStrings = model.SqlConnectionStrings,
                        StorageConfig = model.StorageConfig,
                        PrimaryCloud = model.PrimaryCloud,
                        SecretName = model.SecretName,
                        SecretKey = model.SecretKey,
                        EditorUrls = model.EditorUrls
                    };

                    var json = JsonConvert.SerializeObject(obj);

                    if (_options.Value.SiteSettings.AllowConfigEdit)
                    {
                        await _secretsClient.SetSecret(model.SecretName, json);
                    }

                    ViewData["jsonObject"] = json;
                }
                else
                {
                    ViewData["jsonObject"] = null;
                }

                return View(model);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        /// <summary>
        /// Setup database
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Database([Bind("task")] string task)
        {
            if (string.IsNullOrEmpty(task))
            {
                return RedirectToAction("Index");
            }

            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                var model = new SetupIndexViewModel();

                // Fail if no database connection found.
                if (_options.Value.SqlConnectionStrings == null || !_options.Value.SqlConnectionStrings.Any())
                {
                    return RedirectToAction("Instructions");
                }

                try
                {
                    // If there is a database connection, see if there any users.
                    // If not it is OK to setup admin.
                    if (_options.Value.SqlConnectionStrings != null &&
                        _options.Value.SqlConnectionStrings.Any(a => a.IsPrimary))
                    {
                        // Setup or update the database schema
                        var setupDatabase = new SetupDatabase(_options);
                        var migrationsApplied = await setupDatabase.CreateOrUpdateSchema();
                        // Seed the database with data needed for operations.

                        if (task == "NewInstall")
                        {
                            await setupDatabase.SeedDatabase();
                        }

                        var connectionString = _options.Value.SqlConnectionStrings.FirstOrDefault();
                        using var dbContext = GetDbContext(connectionString.ToString());
                        var sqlSyncContext = new SqlDbSyncContext(_options);
                        dbContext.LoadSyncContext(sqlSyncContext);

                        using var userStore = new UserStore<IdentityUser>(dbContext);
                        using var userManager = new UserManager<IdentityUser>(userStore, null,
                            new PasswordHasher<IdentityUser>(), null,
                            null, null, null, null, null);

                        var results = await userManager.GetUsersInRoleAsync("Administrators");

                        if (results.Count == 0)
                        {
                            // No administrators, setup one now.
                            model.SetupState = SetupState.SetupAdmin;
                        }
                        else if (migrationsApplied > 0)
                        {
                            model.SetupState = SetupState.Upgrade;
                        }
                        else
                        {
                            model.SetupState = SetupState.UpToDate;
                        }
                    }
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                }
                return View(model);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        /// <summary>
        /// Index post method.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Database(SetupIndexViewModel model)
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                if (model != null && ModelState.IsValid)
                {
                    //
                    // Check for any migrations or empty tables.
                    var primary = _options.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);
                    var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    builder.UseSqlServer(primary.ToString());

                    using var dbContext = new ApplicationDbContext(builder.Options);

                    using var userStore = new UserStore<IdentityUser>(dbContext);
                    using var userManager = new UserManager<IdentityUser>(userStore, null,
                        new PasswordHasher<IdentityUser>(), null,
                        null, null, null, null, null);

                    var user = new IdentityUser { UserName = model.AdminEmail, Email = model.AdminEmail, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        var roleResult = await userManager.AddToRoleAsync(user, "Administrators");

                        if (roleResult.Succeeded)
                        {
                            return RedirectToAction("NextSteps");
                        }

                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError("", $"Error: ({error.Code}) {error.Description}");
                        }
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", $"Error: ({error.Code}) {error.Description}");
                        }
                    }
                }
                return View(model);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        /// <summary>
        /// Installation Next steps
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> NextSteps()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                // Check SendGrid
                var sendGridResult = await TestSendGrid(_options.Value.SendGridConfig.SendGridKey);
                return View(sendGridResult);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        /// <summary>
        /// View installation instructions.
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult Instructions()
        {
            return View();
        }

        /// <summary>
        /// Diagnostics display
        /// </summary>
        /// <returns></returns>
        public IActionResult Diagnostics()
        {

            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                return View(_cosmosStatus);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        /// <summary>
        /// Exports configuration as an Excel document.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="base64"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {

            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                var fileContents = Convert.FromBase64String(base64);

                return File(fileContents, contentType, fileName);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        /// <summary>
        ///     Shows how to install the configuration
        /// </summary>
        /// <returns></returns>
        public IActionResult BootConfig()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false) return View();

            return Unauthorized();
        }

        /// <summary>
        ///     Sets up the administrator account.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> SetupAdmin()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                //
                // Double check that there are NO administrators defined yet.
                //

                var connectionString = _options.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);
                using var dbContext = GetDbContext(connectionString.ToString());
                var syncContext = new SqlDbSyncContext(_options);
                dbContext.LoadSyncContext(syncContext);

                using var userManager = GetUserManager(dbContext);
                using var roleManager = GetRoleManager(dbContext);
                var anyUsers = await userManager.Users.AnyAsync();

                if (await userManager.Users.AnyAsync())
                {
                    return Unauthorized();
                }

                return View();
            }

            return Unauthorized();
        }

        /// <summary>
        ///     Finish setup
        /// </summary>
        /// <returns></returns>
        public IActionResult FinishSetup()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false) return View();

            return Unauthorized();
        }

        #region LOCALLY INSTANTIATED OBJECTS

        /*
         * The reason for creating these items here instead of the Startup.cs file
         * is that the setup controller must function even when there is no 
         * configuration.
         * 
         * If we use injection, these objects may not properly exist, and the setup
         * controller can fail upon load.
         * 
         */

        private ApplicationDbContext GetDbContext(string connectionString)
        {
            var sqlBulder = new DbContextOptionsBuilder<ApplicationDbContext>();
            sqlBulder.UseSqlServer(connectionString,
                opts =>
                    opts.CommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds));

            return new ApplicationDbContext(sqlBulder.Options);
        }

        private UserManager<IdentityUser> GetUserManager(ApplicationDbContext dbContext)
        {
            var userStore = new UserStore<IdentityUser>(dbContext);
            var userManager = new UserManager<IdentityUser>(userStore, null, new PasswordHasher<IdentityUser>(), null,
                null, null, null, null, null);
            return userManager;
        }

        private RoleManager<IdentityRole> GetRoleManager(ApplicationDbContext dbContext)
        {
            var userStore = new RoleStore<IdentityRole>(dbContext);
            var userManager = new RoleManager<IdentityRole>(userStore, null, null, null, null);
            return userManager;
        }

        #endregion

        #region CONNECTION TESTS

        /// <summary>
        /// Tests all the connections
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<IActionResult> TestAll(ConfigureIndexViewModel model)
        {
            if (!CanUseConfigWizard()) return Unauthorized();

            var resultsModel = new ValConViewModel();

            var sqlResult = await TestSql(model);
            var storageResult = await TestStorage(model);
            var cdnResult = await TestCdn(model);
            var transResult = await TestTrans(model);
            var sendGridResult = await TestSendGrid(model);

            resultsModel.Results.AddRange(sqlResult.Results);
            resultsModel.Results.AddRange(storageResult.Results);
            resultsModel.Results.AddRange(cdnResult.Results);
            resultsModel.Results.AddRange(transResult.Results);
            resultsModel.Results.AddRange(sendGridResult.Results);

            return Json(resultsModel);
        }

        /// <summary>
        ///     Test SQL Connections
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        private async Task<ValConViewModel> TestSql(ConfigureIndexViewModel model)
        {
            // Load JSON configurations
            LoadJson(model);

            var viewModel = new ValConViewModel();

            // Test SQL connections
            foreach (var dbConn in model.SqlConnectionStrings)
            {
                var connectionResult = new ConnectionResult
                {
                    Host = dbConn.Hostname,
                    ServiceType = "DB"
                };

                try
                {
                    var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    builder.UseSqlServer(dbConn.ToString());

                    await using (var dbContext = new ApplicationDbContext(builder.Options))
                    {
                        if (await dbContext.Database.CanConnectAsync())
                        {
                            connectionResult.Success = true;
                            connectionResult.Message = "";
                        }
                        else
                        {
                            connectionResult.Message = $"Could not connect to DB server: {dbConn.Hostname}.";
                            connectionResult.Success = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    connectionResult.Success = false;
                    connectionResult.Message = e.Message;
                }

                viewModel.Results.Add(connectionResult);
            }

            return viewModel;
        }

        /// <summary>
        ///     Test storage Connections
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        private async Task<ValConViewModel> TestStorage(ConfigureIndexViewModel model)
        {
            // Load JSON configurations
            LoadJson(model);

            var viewModel = new ValConViewModel();

            // Azure Blob Connections
            foreach (var storageConn in model.StorageConfig.AzureConfigs)
            {
                var connectionResult = new ConnectionResult
                {
                    Host = storageConn.AzureBlobStorageEndPoint,
                    ServiceType = "Azure Storage"
                };

                try
                {
                    var client = new BlobServiceClient(storageConn.AzureBlobStorageConnectionString);
                    var containers = client.GetBlobContainersAsync();

                    connectionResult.Success = true;
                    connectionResult.Message = "";
                }
                catch (Exception e)
                {
                    connectionResult.Success = false;
                    connectionResult.Message = e.Message;
                }

                viewModel.Results.Add(connectionResult);
            }

            // Azure Blob Connections
            foreach (var storageConn in model.StorageConfig.AmazonConfigs)
            {
                var connectionResult = new ConnectionResult
                {
                    Host = storageConn.ServiceUrl,
                    ServiceType = "Amazon Storage"
                };

                try
                {
                    var regionIdentifier = RegionEndpoint.GetBySystemName(storageConn.AmazonRegion);
                    using (var client = new AmazonS3Client(storageConn.AmazonAwsAccessKeyId,
                        storageConn.AmazonAwsSecretAccessKey, regionIdentifier))
                    {
                        var bucketList = await client.ListBucketsAsync();

                        connectionResult.Success = true;
                        connectionResult.Message = "";
                    }
                }
                catch (Exception e)
                {
                    connectionResult.Success = false;
                    connectionResult.Message = e.Message;
                }

                viewModel.Results.Add(connectionResult);
            }

            return viewModel;
        }

        /// <summary>
        ///     Test CDN connection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        private async Task<ValConViewModel> TestCdn(ConfigureIndexViewModel model)
        {
            // Load JSON configurations
            LoadJson(model);

            var viewModel = new ValConViewModel();
            var connResult = new ConnectionResult();

            if (!string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientId)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProvider)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientSecret)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantId)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantDomainName)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProfileName)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.EndPointName)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ResourceGroup)
                || !string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.SubscriptionId)
            )
            {
                connResult.Host = model.CdnConfig.AzureCdnConfig.EndPointName;
                connResult.ServiceType = "CDN";

                // Check for missing fields.
                if (string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientId)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProvider)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ClientSecret)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantId)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.TenantDomainName)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.CdnProfileName)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.EndPointName)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.ResourceGroup)
                    || string.IsNullOrEmpty(model.CdnConfig.AzureCdnConfig.SubscriptionId)
                )
                {
                    connResult.Success = false;
                    connResult.Message = "Azure CDN settings are incomplete.";
                    viewModel.Results.Add(connResult);
                }
                else
                {
                    try
                    {
                        var manager = new CdnManagement(model.CdnConfig.AzureCdnConfig);

                        var azResult = await manager.Authenticate();
                        if (azResult.TokenType == "Bearer")
                        {
                            using var client = await manager.GetCdnManagementClient();
                            var profiles = await client.Profiles.ListWithHttpMessagesAsync();
                            if (profiles != null)
                            {
                                connResult.Success = true;
                            }
                            else
                            {
                                connResult.Success = false;
                                connResult.Message = "Azure CDN endpoint connection failed.";
                            }
                        }
                        else
                        {
                            connResult.Success = false;
                            connResult.Message = "Azure CDN authentication connection failed.";
                        }

                        viewModel.Results.Add(connResult);
                    }
                    catch (Exception e)
                    {
                        connResult.Success = false;
                        connResult.Message = e.Message;
                        viewModel.Results.Add(connResult);
                    }
                }
            }

            return viewModel;
        }

        /// <summary>
        ///     Test translation connection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<ValConViewModel> TestTrans(ConfigureIndexViewModel model)
        {
            // Load JSON configurations
            LoadJson(model);

            var viewModel = new ValConViewModel();

            // Is the configuration started?
            if (!string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ClientId)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.AuthProviderX509CertUrl)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.AuthUri)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ClientEmail)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ServiceType)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ProjectId)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.PrivateKeyId)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.PrivateKey)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.TokenUri)
                || !string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ClientX509CertificateUrl)
            )
            {
                var connResult = new ConnectionResult();

                var config = model.GetConfig();

                connResult.Host = model.GoogleCloudAuthConfig.ProjectId;
                connResult.ServiceType = "Google Translate";

                // Check for missing fields.
                if (string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ClientId)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.AuthProviderX509CertUrl)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.AuthUri)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ClientEmail)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ServiceType)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ProjectId)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.PrivateKeyId)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.PrivateKey)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.TokenUri)
                    || string.IsNullOrEmpty(model.GoogleCloudAuthConfig.ClientX509CertificateUrl)
                )
                {
                    connResult.Success = false;
                    connResult.Message = "Google authentication settings are incomplete.";
                }
                else
                {
                    try
                    {
                        var translationServices = new TranslationServices(Options.Create(config));

                        var result = await translationServices.GetSupportedLanguages();

                        if (result.Languages.Count > 0)
                        {
                            connResult.Success = true;
                        }
                        else
                        {
                            connResult.Success = false;
                            connResult.Message = "Failed to connect to Google Translate.";
                        }
                    }
                    catch (Exception e)
                    {
                        connResult.Success = false;
                        connResult.Message = e.Message;
                    }
                }

                viewModel.Results.Add(connResult);
            }


            return viewModel;
        }

        /// <summary>
        ///     Test SendGrid connection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<ValConViewModel> TestSendGrid(ConfigureIndexViewModel model)
        {
            // Load JSON configurations
            LoadJson(model);

            var viewModel = new ValConViewModel();

            var connResult = new ConnectionResult();
            connResult.Host = "sendgrid.com";
            connResult.ServiceType = "SendGrid";
            var result = await TestSendGrid(model.SendGridConfig.SendGridKey);

            try
            {
                switch (result.StatusCode)
                {
                    case HttpStatusCode.OK:
                        connResult.Success = true;
                        connResult.Message = "";
                        break;
                    case HttpStatusCode.Unauthorized:
                        connResult.Success = false;
                        connResult.Message = "Unauthorized";
                        break;
                    default:
                        connResult.Success = false;
                        connResult.Message = result.Body.ToString();
                        break;
                }
            }
            catch (Exception e)
            {
                connResult.Success = false;
                connResult.Message = e.Message;
            }

            viewModel.Results.Add(connResult);

            return viewModel;
        }

        /// <summary>
        /// Performs a connection test to SendGrid in sandbox mode.
        /// </summary>
        /// <param name="sendGridKey"></param>
        /// <returns></returns>
        private async Task<Response> TestSendGrid(string sendGridKey)
        {
            var client = new SendGridClient(sendGridKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress("eric.kauffman@state.ca.gov"),
                Subject = "Test Email",
                PlainTextContent = "Hello World!",
                HtmlContent = "<p>Hello World!</p>"
            };
            msg.AddTo("eric.kauffman@state.ca.gov");

            msg.MailSettings = new MailSettings();
            msg.MailSettings.SandboxMode = new SandboxMode { Enable = true };

            var result = await client.SendEmailAsync(msg);

            return result;
        }

        #endregion
    }
}