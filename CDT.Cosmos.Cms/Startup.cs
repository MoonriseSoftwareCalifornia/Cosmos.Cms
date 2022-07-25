using CDT.Cosmos.BlobService;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Hubs;
using CDT.Cosmos.Cms.Services;
using CDT.Cosmos.Cms.Services.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace CDT.Cosmos.Cms
{
    /// <summary>
    ///     Startup class for the website.
    /// </summary>
    public class Startup
    {

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        ///     Configuration for the website.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     Method configures services for the website.
        /// </summary>
        /// <param name="services"></param>
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appInsightsConfig = Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            SqlConnectionStringBuilder connectionStringBuilder = null;

            services.AddApplicationInsightsTelemetry(appInsightsConfig);

            //
            // Get the boot variables loaded, and
            // do some validation to make sure Cosmos can boot up
            // based on the values given.
            //
            var cosmosStartup = new CosmosStartup(Configuration);

            //
            // Check for boot variable errors and build the options object
            //
            var cosmosOptions = cosmosStartup.Build();

            if (!cosmosStartup.HasErrors)
            {
                //
                // Adds a secret client. For it to work must set environment variable CosmosAllowConfigEdit set to true.
                //
                services.AddSingleton(new SecretsClient(cosmosStartup));

                var primary = cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);

                if (primary != null)
                {
                    connectionStringBuilder = new SqlConnectionStringBuilder(primary.ToString());

                    var secondary = cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.CloudName.Equals(cosmosStartup.PrimaryCloud, StringComparison.CurrentCultureIgnoreCase) == false);

                    if (secondary != null)
                    {
                        // See this link as to why this is commented out right now...
                        //https://our.umbraco.com/forum/extending-umbraco-and-using-the-api/78775-added-a-server-to-the-failover-partner-property-in-the-connection-breaks-the-connection-completely
                        //connectionStringBuilder.FailoverPartner = secondary.Hostname;
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlServer(connectionStringBuilder.ToString());
                    });
                }

                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
                services.ConfigureApplicationCookie(o =>
                {
                    o.ExpireTimeSpan = TimeSpan.FromDays(5);
                    o.SlidingExpiration = true;
                });

                //
                // Add services
                //
                services.AddTransient<StorageContext>(); // Blob storage
                services.AddTransient<SqlDbSyncContext>(); // SQL DB Synchronization
                services.AddTransient<IEmailSender, EmailSender>();
                services.AddTransient<TranslationServices>();
                services.AddTransient<ArticleEditLogic>();

                // This is used by the ViewRenderingService 
                // to export web pages for external editing.
                services.AddScoped<IViewRenderService, ViewRenderService>();

                // Add this before identity
                services.AddControllersWithViews();
                services.AddRazorPages();

                // End before identity

                // See: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
                // And see: https://stackoverflow.com/questions/46320189/asp-net-core-2-unable-to-resolve-service-for-type-microsoft-entityframeworkcore
                // requires
                // using Microsoft.AspNetCore.Identity.UI.Services;
                // using WebPWrecover.Services;
                // Setup SendGrid as EmailSender: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
                // requires
                // using Microsoft.AspNetCore.Identity.UI.Services;
                // using WebPWrecover.Services;

                // https://forums.asp.net/t/2130410.aspx?Roles+and+RoleManager+in+ASP+NET+Core+2
                services.AddIdentity<IdentityUser, IdentityRole>(options =>
                    {
                        options.SignIn.RequireConfirmedAccount = true;
                    })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddUserManager<UserManager<IdentityUser>>()
                    .AddRoleManager<RoleManager<IdentityRole>>()
                    .AddDefaultTokenProviders();

                //
                // Configure authentication providers.
                //
                if (cosmosOptions.Value.AuthenticationConfig.Microsoft != null &&
                    !string.IsNullOrEmpty(cosmosOptions.Value.AuthenticationConfig.Microsoft.ClientId))
                    // Microsoft
                    services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
                    {
                        // Microsoft https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?view=aspnetcore-3.1
                        microsoftOptions.ClientId = cosmosOptions.Value.AuthenticationConfig.Microsoft.ClientId;
                        microsoftOptions.ClientSecret = cosmosOptions.Value.AuthenticationConfig.Microsoft.ClientSecret;
                    });

                if (cosmosOptions.Value.AuthenticationConfig.Google != null &&
                    !string.IsNullOrEmpty(cosmosOptions.Value.AuthenticationConfig.Google.ClientId))
                    services.AddAuthentication()
                        .AddGoogle(options =>
                        {
                            // Google https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-3.1#create-a-google-api-console-project-and-client-id
                            // Dashboard https://console.developers.google.com/projectselector2/apis/dashboard?authuser=0&organizationId=0&supportedpurview=project
                            // On dashboard, on menu left, click "Credentials." It will be one of the "Oauth" settings.

                            options.ClientId = cosmosOptions.Value.AuthenticationConfig.Google.ClientId;
                            options.ClientSecret = cosmosOptions.Value.AuthenticationConfig.Google.ClientSecret;
                        });

                // Add Kendo services (required for Editor)
                services.AddKendo();

                // Need to add this for Telerik
                // https://docs.telerik.com/aspnet-core/getting-started/prerequisites/environment-support#json-serialization
                services.AddMvc()
                    .AddNewtonsoftJson(options =>
                        options.SerializerSettings.ContractResolver =
                            new DefaultContractResolver())
                    .AddRazorPagesOptions(options =>
                    {
                        // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                        //options.AllowAreas = true;
                        options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                        options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                    });

                // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-2.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts
                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(365);
                    //options.ExcludedHosts.Add("example.com");
                    //options.ExcludedHosts.Add("www.example.com");
                });

                services.ConfigureApplicationCookie(options =>
                {
                    // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                    options.LoginPath = "/Identity/Account/Login";
                    options.LogoutPath = "/Identity/Account/Logout";
                    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                });

                // BEGIN
                // When deploying to a Docker container, the OAuth redirect_url
                // parameter may have http instead of https.
                // Providers often do not allow http because it is not secure.
                // So authentication will fail.
                // Article below shows instructions for fixing this.
                //
                // NOTE: There is a companion secton below in the Configure method. Must have this
                // app.UseForwardedHeaders();
                //
                // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                               ForwardedHeaders.XForwardedProto;
                    // Only loopback proxies are allowed by default.
                    // Clear that restriction because forwarders are enabled by explicit
                    // configuration.
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
                // END

                // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/5.0/middleware-database-error-page-obsolete
                //services.AddDatabaseDeveloperPageExceptionFilter();

            }
            else
            {

                //
                // Adds a secret client. For it to work must set environment variable CosmosAllowConfigEdit set to true.
                //
                services.AddSingleton(new SecretsClient(cosmosStartup));

                //
                // Cosmos Startup was not successful.
                // Load what is necessary to show diagnostic
                // and setup pages.
                //

                services.AddControllersWithViews();
                services.AddRazorPages();

                // Add Kendo services (required for Editor)
                services.AddKendo();
                // Need to add this for Telerik
                // https://docs.telerik.com/aspnet-core/getting-started/prerequisites/environment-support#json-serialization
                services.AddMvc()
                    .AddNewtonsoftJson(options =>
                        options.SerializerSettings.ContractResolver =
                            new DefaultContractResolver())
                    .AddRazorPagesOptions(options =>
                    {
                        // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                        //options.AllowAreas = true;
                        options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                        options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                    });

                // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-2.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts
                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(365);
                    //options.ExcludedHosts.Add("example.com");
                    //options.ExcludedHosts.Add("www.example.com");
                });
            }

            //
            // Cosmos startup was successful.  Continue with startup
            //
            // Add the configuration to services here.
            if (cosmosOptions == null || cosmosOptions.Value == null)
            {
                // Cosmos not yet configured
                cosmosOptions = Options.Create(new CosmosConfig());
            }
            else
            {
                services.AddSingleton(cosmosOptions);
            }

            services.ConfigureApplicationCookie(options =>
            {
                // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            // BEGIN
            // When deploying to a Docker container, the OAuth redirect_url
            // parameter may have http instead of https.
            // Providers often do not allow http because it is not secure.
            // So authentication will fail.
            // Article below shows instructions for fixing this.
            //
            // NOTE: There is a companion secton below in the Configure method. Must have this
            // app.UseForwardedHeaders();
            //
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                           ForwardedHeaders.XForwardedProto;
                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit
                // configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            // END

            // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/5.0/middleware-database-error-page-obsolete
            //services.AddDatabaseDeveloperPageExceptionFilter();

            // Save Cosmos configuration status
            services.AddSingleton(cosmosStartup.GetStatus());

            // Add the SignalR service.
            if (connectionStringBuilder == null)
            {
                services.AddSignalR();
            }
            else
            {
                // If there is a DB connection, then use SQL backplane.
                // See: https://github.com/IntelliTect/IntelliTect.AspNetCore.SignalR.SqlServer
                services.AddSignalR().AddSqlServer(o =>
                {
                    o.ConnectionString = connectionStringBuilder.ToString();
                    // See above - attempts to enable Service Broker on the database at startup
                    // if not already enabled. Default false, as this can hang if the database has other sessions.
                    o.AutoEnableServiceBroker = false;
                    // Every hub has its own message table(s). 
                    // This determines the part of the table named that is derived from the hub name.
                    // IF THIS IS NOT UNIQUE AMONG ALL HUBS, YOUR HUBS WILL COLLIDE AND MESSAGES MIX.
                    o.TableSlugGenerator = hubType => hubType.Name;
                    // The number of tables per Hub to use. Adding a few extra could increase throughput
                    // by reducing table contention, but all servers must agree on the number of tables used.
                    // If you find that you need to increase this, it is probably a hint that you need to switch to Redis.
                    o.TableCount = 1;
                    // The SQL Server schema to use for the backing tables for this backplane.
                    o.SchemaName = "SignalRCore";
                });
            }

        }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="cosmosStatus"></param>
        /// <param name="options"></param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            CosmosConfigStatus cosmosStatus,
            IOptions<CosmosConfig> options)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // BEGIN
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            app.UseForwardedHeaders();
            // END

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseResponseCaching(); //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1

            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                // Point to the route that will return the SignalR Hub.
                endpoints.MapHub<ChatHub>("/chat");

                endpoints.MapControllerRoute(
                    "MsValidation",
                    ".well-known/microsoft-identity-association.json",
                    new { controller = "Home", action = "GetMicrosoftIdentityAssociation" });

                endpoints.MapControllerRoute(
                    "MyArea",
                    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                if (cosmosStatus.ReadyToRun && (options.Value.SiteSettings.AllowSetup ?? false) == false)
                {
                    endpoints.MapControllerRoute(
                        "default",
                        "{controller=Home}/{action=Index}/{id?}");
                }
                else
                {
                    endpoints.MapControllerRoute(
                       "default",
                       "{controller=Setup}/{action=Index}/{id?}");
                }

                endpoints.MapRazorPages();

                // This route must go last.  A page name can't conflict with any of the above.
                // This route allows page titles to become URLs.
                endpoints.MapControllerRoute("DynamicPage", "/{id?}/{lang?}",
                    new { controller = "Home", action = "Index" });
            });
        }

    }
}