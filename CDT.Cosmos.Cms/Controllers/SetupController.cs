﻿using Amazon;
using Amazon.S3;
using Azure.Storage.Blobs;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Common.Services.Configurations.Storage;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using Kendo.Mvc.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Controllers
{

    public class SetupController : Controller
    {
        private readonly ILogger<SetupController> _logger;
        private readonly IOptions<CosmosConfig> _options;
        private readonly CosmosConfigStatus _cosmosStatus;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public SetupController(ILogger<SetupController> logger,
            IOptions<CosmosConfig> options,
            CosmosConfigStatus cosmosStatus
        )
        {
            _logger = logger;
            _options = options;
            _cosmosStatus = cosmosStatus;
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

            return _options.Value.SiteSettings.AllowSetup ?? false;
        }

        /// <summary>
        ///     Installs the database
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="dropDatabase"></param>
        /// <returns></returns>
        /// <remarks>Drops database if AllowReset set to true.</remarks>
        private async Task DropAndInstallDb(string connectionString, bool? dropDatabase)
        {
            using var dbContext = GetDbContext(connectionString);

            // Drop database if requested
            if ((_options.Value.SiteSettings.AllowSetup ?? false) &&
                (_options.Value.SiteSettings.AllowReset ?? false) &&
                (dropDatabase ?? false)) await dbContext.Database.EnsureDeletedAsync();

            // Create database if it does not exist, and schema
            await dbContext.Database.MigrateAsync();
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
        ///     Setup home page
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                var model = new SetupIndexViewModel();

                // Fail if no database connection found.
                if (_options.Value.SqlConnectionStrings == null || !_options.Value.SqlConnectionStrings.Any())
                {
                    return RedirectToAction("Instructions");
                }

                //
                // Check for any migrations or empty tables.
                var primary = _options.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);
                var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
                builder.UseSqlServer(primary.ToString());

                using var dbContext = new ApplicationDbContext(builder.Options);

                // Are there any applied migrations?
                var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
                // Are there any pending migrations? (Upgrade)
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

                // If there is a database connection, see if there are no administrators yet.
                if (_options.Value.SqlConnectionStrings != null &&
                    _options.Value.SqlConnectionStrings.Any(a => a.IsPrimary))
                {

                    if (!dbContext.Database.GetPendingMigrations().Any())
                    {
                        using var userStore = new UserStore<IdentityUser>(dbContext);
                        using var userManager = new UserManager<IdentityUser>(userStore, null,
                            new PasswordHasher<IdentityUser>(), null,
                            null, null, null, null, null);

                        var results = userManager.GetUsersInRoleAsync("Administrators").Result;

                        if (results.Count == 0) return RedirectToAction("SetupAdmin");
                    }
                }

                return View(_cosmosStatus);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Index(SetupIndexViewModel model)
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
                        return RedirectToAction("NextSteps");
                    }
                    else
                    {
                        foreach(var error in result.Errors)
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

        [AllowAnonymous]
        public IActionResult NextSteps()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                return View();
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

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
        ///     Configuration wizard
        /// </summary>
        /// <returns></returns>
        public IActionResult ConfigWizard()
        {
            if (CanUseConfigWizard())
            {
                if (
                    _options.Value == null
                    || _options.Value.SiteSettings == null
                    || _options.Value.SiteSettings.AllowSetup == false
                    || User.Identity.IsAuthenticated == false
                    || User.Identity.IsAuthenticated &&
                    User.IsInRole("Administrators") == false) // Setup allowed but user not an adminstrator
                    return View(new ConfigureIndexViewModel());

                return View(new ConfigureIndexViewModel(_options.Value.EnvironmentVariable, _options.Value));
            }

            return Unauthorized();
        }

        /// <summary>
        ///     Configuraton wizard post back
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ResponseCache(NoStore = true)]
        public IActionResult ConfigWizard(ConfigureIndexViewModel model)
        {
            model.PrimaryCloud = string.IsNullOrEmpty(_options.Value.PrimaryCloud) ? "" : _options.Value.PrimaryCloud.ToLower();
            if (CanUseConfigWizard())
            {
                if (!string.IsNullOrEmpty(model.ImportJson))
                {
                    ModelState.Clear();
                    var config = JsonConvert.DeserializeObject<CosmosConfig>(model.ImportJson);
                    model = new ConfigureIndexViewModel(_options.Value?.EnvironmentVariable, config);
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
                        EnvironmentVariable = _options.Value?.EnvironmentVariable,
                        SecretKey = model.SecretKey,
                        EditorUrls = model.EditorUrls
                    };
                    ViewData["jsonObject"] = JsonConvert.SerializeObject(obj);
                }
                else
                {
                    ViewData["jsonObject"] = null;
                }

                return View(model);
            }

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
        ///     Installs the C/CMS database
        /// </summary>
        /// <returns></returns>
        public IActionResult InstallDatabase()
        {
            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                ViewData["HasConfig"] = _options.Value.SqlConnectionStrings != null &&
                                        _options.Value.SqlConnectionStrings.Any();
                return View();
            }

            return Unauthorized();
        }

        /// <summary>
        ///     Installs C/CMS database, schema and seeds selected tables
        /// </summary>
        /// <param name="enableInstall">The user has enabled database install.</param>
        /// <returns></returns>
        /// <remarks>This may take several minutes!</remarks>
        [HttpPost]
        public async Task<IActionResult> InstallDatabase(bool enableInstall)
        {
            var hasConfig = _options.Value.SqlConnectionStrings != null && _options.Value.SqlConnectionStrings.Any();
            ViewData["HasConfig"] = hasConfig;
            // Safety check!
            if (!enableInstall || hasConfig == false) return View();

            var dropDatabase = _options.Value?.SiteSettings.AllowReset ?? false;

            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                if (_options.Value == null || !_options.Value.SqlConnectionStrings.Any())
                {
                    ModelState.AddModelError("", "Database configuration not found.");
                }
                else if (!_options.Value.SqlConnectionStrings.Any(a => a.IsPrimary))
                {
                    ModelState.AddModelError("", "Primary database connection not set.");
                }
                else
                {
                    try
                    {
                        var tasks = new List<Task>();
                        // Drop existing databases
                        foreach (var connection in _options.Value.SqlConnectionStrings)
                            tasks.Add(DropAndInstallDb(connection.ToString(), dropDatabase));
                        Task.WaitAll(tasks.ToArray());
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("", e.Message);
                    }

                    try
                    {
                        // Pre load tables. Records will automatically be replicated among all database instances.
                        // Install icons and layout

                        var primary = _options.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);

                        using var sqlContext = GetDbContext(primary.ToString());
                        var syncContext = new SqlDbSyncContext(_options);
                        sqlContext.LoadSyncContext(syncContext);

                        var tblSeeding = new TableSeeding(sqlContext);

                        await tblSeeding.LoadTemplates();
                        await tblSeeding.LoadLayouts();
                        await tblSeeding.LoadFontIcons();

                        // Setup roles //
                        using var roleManager = GetRoleManager(sqlContext);

                        if (!await roleManager.RoleExistsAsync("Administrators"))
                            await roleManager.CreateAsync(new IdentityRole("Administrators"));

                        if (!await roleManager.RoleExistsAsync("Editors"))
                            await roleManager.CreateAsync(new IdentityRole("Editors"));

                        if (!await roleManager.RoleExistsAsync("Authors"))
                            await roleManager.CreateAsync(new IdentityRole("Authors"));

                        if (!await roleManager.RoleExistsAsync("Reviewers"))
                            await roleManager.CreateAsync(new IdentityRole("Reviewers"));

                        if (!await roleManager.RoleExistsAsync("Team Members"))
                            await roleManager.CreateAsync(new IdentityRole("Team Members"));
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("", e.Message);
                    }
                }

                return Json(new
                {
                    ModelState.IsValid,
                    Errors = ModelState.Values
                });
            }

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
                var admins = await userManager.GetUsersInRoleAsync("Administrators");
                var anyUsers = await userManager.Users.AnyAsync();

                IdentityResult result = null;

                var user = await userManager.GetUserAsync(User);

                if (user == null) return Redirect("~/Account/Logout");

                if (admins != null && admins.Count < 1)
                    // Add the first registered user as the first administrator
                    result = await userManager.AddToRoleAsync(user, "Administrators");
                else if (User.Identity.IsAuthenticated)
                    if (await userManager.IsInRoleAsync(user, "Administrators"))
                        result = IdentityResult.Success;

                return View(result);
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
        ///     Test SQL Connections
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> TestSql(ConfigureIndexViewModel model)
        {
            if (!CanUseConfigWizard()) return Unauthorized();

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

            return Json(viewModel);
        }

        /// <summary>
        ///     Test SQL Connections
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> TestStorage(ConfigureIndexViewModel model)
        {
            if (!CanUseConfigWizard()) return Unauthorized();

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

            return Json(viewModel);
        }

        /// <summary>
        ///     Test CDN connection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> TestCdn(ConfigureIndexViewModel model)
        {
            if (!CanUseConfigWizard()) return Unauthorized();

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
                        if (azResult.AccessTokenType == "Bearer")
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

            return Json(viewModel);
        }

        /// <summary>
        ///     Test translation connection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<IActionResult> TestTrans(ConfigureIndexViewModel model)
        {
            if (!CanUseConfigWizard()) return Unauthorized();

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


            return Json(viewModel);
        }

        /// <summary>
        ///     Test SendGrid connection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<IActionResult> TestSendGrid(ConfigureIndexViewModel model)
        {
            if (!CanUseConfigWizard()) return Unauthorized();

            // Load JSON configurations
            LoadJson(model);

            var viewModel = new ValConViewModel();

            var client = new SendGridClient(model.SendGridConfig.SendGridKey);
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

            var connResult = new ConnectionResult();

            connResult.Host = "sendgrid.com";
            connResult.ServiceType = "SendGrid";

            try
            {
                var result = await client.SendEmailAsync(msg);
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

            return Json(viewModel);
        }

        #endregion
    }
}