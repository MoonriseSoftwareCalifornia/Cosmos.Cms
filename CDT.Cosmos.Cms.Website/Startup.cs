using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Security.Authentication;

namespace CDT.Cosmos.Cms.Website
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
        /// <remarks>This method gets called by the runtime. Use this method to add services to the container.</remarks>
        public void ConfigureServices(IServiceCollection services)
        {

            var appInsightsConfig = Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            services.AddApplicationInsightsTelemetry();

            //
            // Get the boot variables loaded, and
            // do some validation to make sure Cosmos can boot up
            // based on the values given.
            //
            var cosmosStartup = new CosmosStartup(Configuration);

            //
            // Check for boot variable errors and build the options
            //
            var cosmosOptions = cosmosStartup.Build();

            if (cosmosOptions.Value.SqlConnectionStrings.Any())
            {
                cosmosStartup.Diagnostics.AddRange(cosmosStartup.Diagnostics);

                cosmosStartup.Diagnostics.Add(new Diagnostic()
                {
                    Message = "cosmosStartup.TryRun() succesful.",
                    ServiceType = "Startup.cs",
                    Success = true
                });

                cosmosStartup.Diagnostics.Add(new Diagnostic()
                {
                    Message = "services.AddSingleton(cosmosOptions) succesful.",
                    ServiceType = "Startup.cs",
                    Success = true
                });

                // BEGIN
                // Add response compression
                // https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-5.0#providers
                services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                });

                var primary = cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary);

                if (primary != null)
                {
                    cosmosStartup.Diagnostics.Add(new Diagnostic()
                    {
                        Message = "cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary) succesful.",
                        ServiceType = "Startup.cs",
                        Success = true
                    });

                    var connectionStringBuilder = new SqlConnectionStringBuilder(primary.ToString());

                    // Adding a fail over partner within Azure, and using Azure SQL may
                    // result in a rare SQL error about "routes" failing.
                    //var secondary = cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.CloudName.Equals(cosmosStartup.PrimaryCloud, StringComparison.CurrentCultureIgnoreCase) == false);

                    //if (secondary != null)
                    //{
                    //    connectionStringBuilder.FailoverPartner = secondary.Hostname;
                    //    cosmosStartup.Diagnostics.Add(new Diagnostic()
                    //    {
                    //        Message = $"SQL failover partner ({secondary.Hostname}) succesful.",
                    //        ServiceType = "Startup.cs",
                    //        Success = true
                    //    });
                    //}

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlServer(connectionStringBuilder.ToString());
                    });

                    cosmosStartup.Diagnostics.Add(new Diagnostic()
                    {
                        Message = "services.AddDbContext<ApplicationDbContext>() succesful.",
                        ServiceType = "Startup.cs",
                        Success = true
                    });

                }
                else
                {
                    cosmosStartup.Diagnostics.Add(new Diagnostic()
                    {
                        Message = "cosmosOptions.Value.SqlConnectionStrings.FirstOrDefault(f => f.IsPrimary) FAILED!",
                        ServiceType = "Startup.cs",
                        Success = false
                    });
                }

                services.AddResponseCaching(options =>
                {
                    options.UseCaseSensitivePaths = false;
                    // Kestrel fails if bigger than this
                    options.MaximumBodySize = 4096;
                    options.SizeLimit = 4096;
                });

                //
                // Add services
                //
                services.AddTransient<IEmailSender, EmailSender>();
                services.AddTransient<TranslationServices>();

                // Add this before identity
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
                services.ConfigureApplicationCookie(o =>
                {
                    o.ExpireTimeSpan = TimeSpan.FromDays(5);
                    o.SlidingExpiration = true;
                });
            }
            else
            {
                cosmosStartup.Diagnostics.AddRange(cosmosStartup.Diagnostics);
                cosmosStartup.Diagnostics.Add(new Diagnostic()
                {
                    Message = "cosmosStartup.TryRun() FAILED!",
                    ServiceType = "Startup.cs",
                    Success = false
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

            services.AddResponseCompression();
            services.AddControllersWithViews();
            services.AddRazorPages();

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
            // BEGIN
            // When deploying to a Docker container, the OAuth redirect_url
            // parameter may have http instead of https.
            // Providers often do not allow http because it is not secure.
            // So authentication will fail.
            // Article below shows instructions for fixing this.
            //
            // NOTE: There is a companion secton below in the Configure method. Must have this!
            // app.UseForwardedHeaders();
            //
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            //services.Configure<ForwardedHeadersOptions>(options =>
            //{
            //    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
            //                  ForwardedHeaders.XForwardedProto;
            //    // Only loopback proxies are allowed by default.
            //    // Clear that restriction because forwarders are enabled by explicit
            //    // configuration.
            //    options.KnownNetworks.Clear();
            //    options.KnownProxies.Clear();
            //});

            services.AddApplicationInsightsTelemetry(t => t.ConnectionString = Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

            // END
            // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/5.0/middleware-database-error-page-obsolete
            // services.AddDatabaseDeveloperPageExceptionFilter();

            // Save Cosmos configuration status
            services.AddSingleton(cosmosStartup.GetStatus());

        }


        /// <summary>
        ///     This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            CosmosConfigStatus cosmosStatus)
        {
            // Kestrel web server does not support compression, so this is added here.
            // It's position in the configuration is important. It must occur at or near the top.
            // https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-5.0
            app.UseResponseCompression();

            //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1
            app.UseResponseCaching();

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
            //app.UseForwardedHeaders();
            // END

            // This might be needed for code deploy
            //app.UseHttpsRedirection(); // Comment out for Docker container!
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "MyArea",
                    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                if (cosmosStatus.ReadyToRun)
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