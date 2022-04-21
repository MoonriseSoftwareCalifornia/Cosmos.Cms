using Azure.Identity;
using CDT.Cosmos.BlobService;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Controllers;
using CDT.Cosmos.Cms.Data.Logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.Tests
{

    public class Utilities
    {
        private IOptions<CosmosConfig>? _cosmosOptions;

        /// <summary>
        /// Constructor
        /// </summary>
        public Utilities()
        {
            _cosmosOptions = GetCosmosConfigOptions();
        }

        /// <summary>
        ///     Gets the application Db context with sync context loaded.
        /// </summary>
        /// <returns></returns>
        public ApplicationDbContext GetApplicationDbContext(SqlConnectionString? connectionString = null, IOptions<CosmosConfig>? cosmosConfig = null)
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

            if (cosmosConfig == null)
            {
                cosmosConfig = GetCosmosConfigOptions();
            }

            var primary = connectionString ?? cosmosConfig.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);

            Debug.Assert(primary != null, nameof(primary) + " != null");
            builder.UseSqlServer(primary.ToString());

            var context = new ApplicationDbContext(builder.Options);
            var syncContext = GetSqlDbSyncContext();
            context.LoadSyncContext(syncContext);

            //context.Database.EnsureDeleted();
            //context.Database.EnsureCreated();

            //context.Database.Migrate();

            return context;
        }

        public ArticleLogic GetArticleLogic(ApplicationDbContext dbContext, bool isEditor = true)
        {
            var options = GetCosmosConfigOptions();
            options.Value.SiteSettings.IsEditor = false;
            return new ArticleLogic(
                dbContext,
                options);
        }

        public ArticleLogic GetArticleLogicNoRedis(ApplicationDbContext dbContext, bool isEditor = true)
        {
            var options = GetCosmosConfigOptions();
            options.Value.SiteSettings.IsEditor = false;
            return new ArticleLogic(
                dbContext,
                options);
        }

        public ArticleLogic GetArticleLogicNoRedisNoSql(bool isEditor = true)
        {
            var options = GetCosmosConfigOptions();
            options.Value.SqlConnectionStrings[0].InitialCatalog = "messedup";
            options.Value.SiteSettings.IsEditor = false;
            var dbContext = this.GetApplicationDbContext(options.Value.SqlConnectionStrings[0]);
            return new ArticleLogic(
                dbContext,
                options);
        }

        public ArticleEditLogic GetArticleEditLogic(ApplicationDbContext dbContext, bool allSetupOn = true)
        {
            //var siteOptions = Options.Create(new SiteSettings
            //{
            //    ReadWriteMode = readWriteModeOn,
            //    AllowSetup = allSetupOn
            //});

            var options = GetCosmosConfigOptions();

            options.Value.SiteSettings.AllowSetup = allSetupOn;
            options.Value.SiteSettings.AllowReset = allSetupOn;

            var syncContext = GetSqlDbSyncContext();

            return new ArticleEditLogic(
                dbContext,
                options, syncContext);
        }

        public IFormFile GetFormFile(string fileName)
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes($"This is a file for {fileName}."));
            return new FormFile(stream, 0, stream.Length, fileName, fileName);
        }

        public SqlDbSyncContext GetSqlDbSyncContext(IOptions<CosmosConfig>? config = null)
        {
            if (config == null)
            {
                config = GetCosmosConfigOptions();
            }
            return new(config);
        }

        /// <summary>
        ///     Use this context to test database install
        /// </summary>
        /// <returns></returns>
        public SqlDbSyncContext GetInstallTestSqlDbSyncContext()
        {
            var config = GetCosmosConfigOptions();

            config.Value.SqlConnectionStrings[0].InitialCatalog = "cosmos-mc-unittest-install";
            config.Value.SqlConnectionStrings[0].IsPrimary = true;

            config.Value.SqlConnectionStrings[1].InitialCatalog = "cosmos-mc-unittest-install-secondary";
            config.Value.SqlConnectionStrings[1].IsPrimary = false;

            return new SqlDbSyncContext(config);
        }

        /// <summary>
        ///     Compares date/times (not kind)
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        public bool DateTimesAreEqual(DateTimeOffset expected, DateTimeOffset actual)
        {
            //
            // The date/time should stay exactly the same after the save.
            //
            var isValid = expected.Year == actual.Year &&
                          expected.Month == actual.Month &&
                          expected.Day == actual.Day &&
                          expected.Hour == actual.Hour &&
                          expected.Minute == actual.Minute &&
                          expected.Second == actual.Second;

            return isValid;
        }

        #region CONFIGURATION MOCK UPS

        //private const string EditorRoleName = "Editors";
        private IConfiguration _configuration;

        private CosmosStartup _cosmosBootConfig;
        //private  ClientSecretCredential _clientSecretCredential;

        public IOptions<CosmosConfig> GetCosmosConfigOptions(bool awsSecrets = false, string secretKeyName = "")
        {
            if (_cosmosOptions == null)
            {
                _configuration = GetConfig(true);
                var optionsBuilder = new CosmosStartup(_configuration);

                var _cosmosOptions = optionsBuilder.Build();

                if (_cosmosOptions == null)
                {
                    throw new InvalidOperationException("Could not load configuration");
                }
                return _cosmosOptions;
            }

            return _cosmosOptions;
        }

        public IOptions<CosmosConfig> GetCosmosConfigOptionsInSetupMode(bool awsSecrets = false, string secretKeyName = "")
        {
            var options = GetCosmosConfigOptions(awsSecrets, secretKeyName).Value;


            var cosmosConfig = new CosmosConfig()
            {
                AuthenticationConfig = options.AuthenticationConfig,
                CdnConfig = options.CdnConfig,
                EditorUrls = options.EditorUrls,
                SecretName = options.SecretName,
                GoogleCloudAuthConfig = options.GoogleCloudAuthConfig,
                PrimaryCloud = options.PrimaryCloud,
                SecretKey = options.SecretKey,
                SendGridConfig = options.SendGridConfig,
                SqlConnectionStrings = options.SqlConnectionStrings,
                StorageConfig = options.StorageConfig,
                SiteSettings = new SiteSettings()
                {
                    AllowSetup = true, // SETUP MODE HERE
                    AllowConfigEdit = false,
                    AllowedFileTypes = options.SiteSettings.AllowedFileTypes,
                    AllowReset = false,
                    BlobPublicUrl = options.SiteSettings.BlobPublicUrl,
                    ContentSecurityPolicy = options.SiteSettings.ContentSecurityPolicy,
                    PublisherUrl = options.SiteSettings.PublisherUrl,
                    XFrameOptions = options.SiteSettings.XFrameOptions
                }
            };

            return Options.Create(cosmosConfig);
        }

        public IOptions<CosmosConfig> GetCosmosConfigOptionsNotInSetupMode(bool awsSecrets = false, string secretKeyName = "")
        {
            var options = GetCosmosConfigOptions(awsSecrets, secretKeyName).Value;


            var cosmosConfig = new CosmosConfig()
            {
                AuthenticationConfig = options.AuthenticationConfig,
                CdnConfig = options.CdnConfig,
                EditorUrls = options.EditorUrls,
                SecretName = options.SecretName,
                GoogleCloudAuthConfig = options.GoogleCloudAuthConfig,
                PrimaryCloud = options.PrimaryCloud,
                SecretKey = options.SecretKey,
                SendGridConfig = options.SendGridConfig,
                SqlConnectionStrings = options.SqlConnectionStrings,
                StorageConfig = options.StorageConfig,
                SiteSettings = new SiteSettings()
                {
                    AllowSetup = false, // SETUP MODE HERE
                    AllowConfigEdit = false,
                    AllowedFileTypes = options.SiteSettings.AllowedFileTypes,
                    AllowReset = false,
                    BlobPublicUrl = options.SiteSettings.BlobPublicUrl,
                    ContentSecurityPolicy = options.SiteSettings.ContentSecurityPolicy,
                    PublisherUrl = options.SiteSettings.PublisherUrl,
                    XFrameOptions = options.SiteSettings.XFrameOptions
                }
            };

            return Options.Create(cosmosConfig);
        }

        public Guid GetCacheId()
        {
            return new("4dc3249c-64a9-4731-babc-fe5b2b1a7af7");
        }

        private string GetKeyValue(IConfigurationRoot config, string key)
        {
            var data = config[key];
            if (string.IsNullOrEmpty(data))
            {
                data = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrEmpty(data))
                {
                    data = Environment.GetEnvironmentVariable(key.ToUpper());
                }
            }
            return data;
        }

        internal IConfiguration GetConfig(bool allowSetup = false)
        {
            if (_configuration != null) return _configuration;

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonFile(jsonConfig, true)
                .AddEnvironmentVariables() // Added to read environment variables from GitHub Actions
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true); // User secrets override all - put here

            var config = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(1));

            // From either local secrets or app config, get connection info for Azure Vault.
            var tenantId = GetKeyValue(config, "CosmosAzureVaultTenantId");

            var clientId = GetKeyValue(config, "CosmosAzureVaultClientId");

            var key = GetKeyValue(config, "CosmosAzureVaultClientSecret");

            var vaultUrl = GetKeyValue(config, "CosmosAzureVaultUrl");

            var secretName = GetKeyValue(config, "CosmosSecretName");

            var strCosmosUseAwsSecretsMgr = GetKeyValue(config, "CosmosUseAwsSecretsMgr");

            _ = bool.TryParse(strCosmosUseAwsSecretsMgr, out var useAwsSecretsManager);

            var awsSecretsRegion = GetKeyValue(config, "CosmosAwsSecretsRegion");

            var awsKeyId = GetKeyValue(config, "CosmosAwsKeyId");

            var awsSecretAccessKey = GetKeyValue(config, "CosmosAwsSecretAccessKey");

            var primaryCloud = GetKeyValue(config, "CosmosPrimaryCloud");

            _cosmosBootConfig = new CosmosStartup()
            {
                AllowSetup = allowSetup,
                AllowConfigEdit = true,
                AzureVaultClientId = clientId,
                AzureVaultClientSecret = key,
                CosmosAzureVaultUrl = vaultUrl,
                SecretName = secretName,
                UseAzureVault = true,
                UseAwsSecretsMgr = useAwsSecretsManager,
                AwsSecretsRegion = awsSecretsRegion,
                AwsSecretAccessKey = awsSecretAccessKey,
                AwsKeyId = awsKeyId,
                AzureVaultTenantId = tenantId,
                PrimaryCloud = primaryCloud
            };


            var clientSecretCredential = new ClientSecretCredential(
                tenantId,
                clientId,
                key);

            builder.AddAzureKeyVault(new Uri(vaultUrl), clientSecretCredential);

            _configuration = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(1));

            return _configuration;
        }

        public CosmosStartup GetCosmosBootConfig()
        {
            if (_cosmosBootConfig == null)
                GetConfig();

            return _cosmosBootConfig;
        }

        //public  ClientSecretCredential GetClientSecretCredential()
        //{
        //    return _clientSecretCredential;
        //}

        //public  SimpleProxyConfigs GetSimpleProxyConfigs()
        //{
        //    return new SimpleProxyConfigs()
        //    {
        //        Configs = new ProxyConfig[]
        //         {
        //             new ProxyConfig()
        //                {
        //                    ContentType = "text/html; charset=UTF-8",
        //                    Method = "GET",
        //                    Name = "GoogleAnonymous",
        //                    Password = "",
        //                    UriEndpoint = "https://www.google.com",
        //                    UserName = "",
        //                    Roles = new string[] { "Anonymous" }
        //                },
        //             new ProxyConfig()
        //                {
        //                    ContentType = "application/x-www-form-urlencoded",
        //                    Method = "GET",
        //                    Name = "TableauAnonymous",
        //                    Password = "",
        //                    UriEndpoint = "https://worldtimeapi.org/api/timezone",
        //                    UserName = "",
        //                    Roles = new string[] { "Anonymous" }
        //                },
        //             new ProxyConfig()
        //                {
        //                    ContentType = "application/x-www-form-urlencoded",
        //                    Method = "GET",
        //                    Name = "TableauAuthenticated",
        //                    Password = "",
        //                    UriEndpoint = "https://worldtimeapi.org/api/timezone",
        //                    UserName = "",
        //                    Roles = new string[] { "Authenticated" }
        //                },
        //             new ProxyConfig()
        //                {
        //                    ContentType = "application/x-www-form-urlencoded",
        //                    Method = "GET",
        //                    Name = "TableauAdministrators",
        //                    Password = "",
        //                    UriEndpoint = "https://worldtimeapi.org/api/timezone",
        //                    UserName = "",
        //                    Roles = new string[] { "Administrators" }
        //                }
        //         }
        //    };
        //}

        #endregion

        #region CONTROLLER MOCK UPS

        #region CONTROLLER MOCK CONTEXT

        public ILogger<T> GetLogger<T>()
        {
            return new Logger<T>(new NullLoggerFactory());
        }

        public HttpContext GetMockContext(ClaimsPrincipal user)
        {
            return new DefaultHttpContext
            {
                FormOptions = new FormOptions(),
                Items = new Dictionary<object, object>(),
                RequestAborted = default,
                RequestServices = null!,
                ServiceScopeFactory = null!,
                Session = null!,
                TraceIdentifier = null!,
                User = user
            };
        }

        #endregion

        #region CONTROLLERS

        public EditorController GetEditorController(ClaimsPrincipal user, bool allowSetup = false)
        {
            var logger = new Logger<EditorController>(new NullLoggerFactory());

            var options = GetCosmosConfigOptionsNotInSetupMode();

            var dbContext = GetApplicationDbContext(null, options);

            var controller = new EditorController(
                logger,
                dbContext,
                GetUserManager(dbContext),
                GetArticleEditLogic(dbContext),
                options,
                GetSqlDbSyncContext(options), null)
            {
                ControllerContext = { HttpContext = GetMockContext(user) }
            };
            return controller;
        }

        public HomeController GetHomeController(ClaimsPrincipal user, bool allowSetup, IOptions<CosmosConfig>? siteOptions = null)
        {
            if (siteOptions == null)
            {
                siteOptions = GetCosmosConfigOptions();
            }
            siteOptions.Value.SiteSettings.AllowSetup = allowSetup;

            var dbContext = GetApplicationDbContext();

            var logger = new Logger<HomeController>(new NullLoggerFactory());
            //var blobOptions = Options.Create(new AzureBlobServiceConfig());
            var controller = new HomeController(logger,
                siteOptions,
                dbContext,
                GetArticleEditLogic(dbContext)
            )
            {
                ControllerContext = { HttpContext = GetMockContext(user) }
            };
            return controller;
        }

        public LayoutsController GetLayoutsController()
        {
            var logger = new Logger<LayoutsController>(new NullLoggerFactory());

            var options = GetCosmosConfigOptionsNotInSetupMode();

            var dbContext = GetApplicationDbContext(null, options);

            var user = GetPrincipal(TestUsers.Foo).Result;

            var controller = new LayoutsController(
                dbContext,
                GetUserManager(dbContext),
                GetArticleEditLogic(dbContext),
                GetSqlDbSyncContext(options),
                options,
                logger, null)
            {
                ControllerContext = { HttpContext = GetMockContext(user) }
            }; ;

            return controller;
        }

        public SetupController GetSetupController()
        {
            var logger = new Logger<SetupController>(new NullLoggerFactory());
            //var blobOptions = Options.Create(new AzureBlobServiceConfig());
            //var claimsPrincipal = GetPrincipal(TestUsers.Foo).Result;
            //var redisConfig = Options.Create(GetRedisContextConfig());
            //var dbContext = GetApplicationDbContext();
            var cosmosStartup = GetCosmosBootConfig();
            cosmosStartup.AllowConfigEdit = true;

            var cosmosConfig = GetCosmosConfigOptionsInSetupMode();

            var controller = new SetupController(
                logger,
                cosmosConfig,
                null,
                new CDT.Cosmos.Cms.Services.Secrets.SecretsClient(cosmosStartup)
                );

            return controller;
        }

        public TeamsController GetTeamsController()
        {
            //var logger = new Logger<TeamsController>(new NullLoggerFactory());
            //var blobOptions = Options.Create(new AzureBlobServiceConfig());
            var claimsPrincipal = GetPrincipal(TestUsers.Foo).Result;

            // Clone the options because we are going to set allow setup to false
            var siteOptions = Options.Create(_cosmosOptions.Value);

            siteOptions.Value.SiteSettings.AllowSetup = false;

            var dbContext = GetApplicationDbContext();

            var controller = new TeamsController(
                GetCosmosConfigOptions(),
                dbContext,
                GetUserManager(),
                GetArticleEditLogic(dbContext),
                GetSqlDbSyncContext())
            {
                ControllerContext = { HttpContext = GetMockContext(claimsPrincipal) }
            };
            return controller;
        }

        #endregion

        #endregion

        #region SERVICE MOCK UPS

        public StorageContext GetStorageContext()
        {
            var service = new StorageContext(GetCosmosConfigOptions(), GetMemoryCache());
            return service;
        }

        public StorageContext GetStorageContext_AmazonPrimary()
        {
            var config = GetCosmosConfigOptions();
            config.Value.PrimaryCloud = "amazon";
            return new StorageContext(config, GetMemoryCache());
        }

        public StorageContext GetStorageContext_AzurePrimary()
        {
            var config = GetCosmosConfigOptions();
            config.Value.PrimaryCloud = "azure";
            return new StorageContext(config, GetMemoryCache());
        }

        private IMemoryCache GetMemoryCache()
        {
            var options = Options.Create(new MemoryCacheOptions()
            {
                SizeLimit = 20000000 // 20 megabytes decimal
            });
            return new MemoryCache(options);
        }

        public IEmailSender GetEmailSender()
        {
            var emailSender = new CDT.Cosmos.Cms.Common.Services.EmailSender(_cosmosOptions);
            return emailSender;
        }


        #endregion

        #region USER MOCK UPS

        public async Task<IdentityUser> GetIdentityUser(string emailAddress)
        {
            using var userManager = GetUserManager();
            var user = await userManager.FindByEmailAsync(emailAddress);
            if (user == null)
            {
                await userManager.CreateAsync(new IdentityUser(emailAddress)
                {
                    Email = emailAddress,
                    Id = Guid.NewGuid().ToString(),
                    EmailConfirmed = true
                });
                user = await userManager.FindByEmailAsync(emailAddress);
            }

            return user;
        }

        public async Task<ClaimsPrincipal> GetPrincipal(string emailAddress)
        {
            var user = await GetIdentityUser(emailAddress);
            using var userManager = GetUserManager();
            var claims = await userManager.GetClaimsAsync(user);

            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

            var roles = await userManager.GetRolesAsync(user);

            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));

            return principal;
        }

        public UserManager<IdentityUser> GetUserManager(ApplicationDbContext? dbContext = null)
        {
            if (dbContext == null)
            {
                dbContext = GetApplicationDbContext();
            }
            var userStore = new UserStore<IdentityUser>(dbContext);
            var userManager = new UserManager<IdentityUser>(userStore, null, new PasswordHasher<IdentityUser>(), null,
                null, null, null, null, GetLogger<UserManager<IdentityUser>>());
            return userManager;
        }

        public RoleManager<IdentityRole> GetRoleManager()
        {
            var userStore = new RoleStore<IdentityRole>(GetApplicationDbContext());
            var userManager = new RoleManager<IdentityRole>(userStore, null, null, null, GetLogger<RoleManager<IdentityRole>>());
            return userManager;
        }

        #endregion
    }
}
