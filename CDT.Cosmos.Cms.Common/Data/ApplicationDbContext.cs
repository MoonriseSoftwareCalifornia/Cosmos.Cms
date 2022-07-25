using CDT.Cosmos.Cms.Common.Services.Configurations;
using Dotmim.Sync;
using Dotmim.Sync.Enumerations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Database Context for Cosmos CMS
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        private SqlDbSyncContext _syncContext;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="options"></param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        ///    Constructor that also loads the database synchronization context.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cosmosOptions"></param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IOptions<CosmosConfig> cosmosOptions) : base(options)
        {
            this.LoadSyncContext(new SqlDbSyncContext(cosmosOptions));
        }

        /// <summary>
        ///     Gets the last synchronization result
        /// </summary>
        public List<SyncResult> GetLastSyncResult { get; private set; }

        /// <summary>
        ///     Determine if this service is configured
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsConfigured()
        {
            return await base.Database.CanConnectAsync();
        }

        /// <summary>
        ///     Load database synchronization context
        /// </summary>
        public void LoadSyncContext(SqlDbSyncContext syncContext)
        {
            _syncContext = syncContext;
        }

        #region OVERRIDES

        /// <summary>
        ///     On model creating
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //
            // The following two converters ensure UTC date times are 
            // going into the database, and they are retried with
            // DateTime.Kind set to UTC
            // More information:
            // * https://stackoverflow.com/questions/4648540/entity-framework-datetime-and-utc
            // * https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions?tabs=data-annotations

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            modelBuilder.Entity<Article>()
                .HasIndex(p => new { p.UrlPath }).IsUnique(false);

            modelBuilder.Entity<Article>()
                .HasIndex(p => new { p.UrlPath, p.Published, p.StatusCode })
                .HasFilter("[Published] IS NOT NULL");

            modelBuilder.Entity<Article>()
                .Property(p => p.Updated)
                .HasConversion(dateTimeConverter);

            modelBuilder.Entity<Article>()
                .Property(p => p.Published)
                .HasConversion(nullableDateTimeConverter);

            modelBuilder.Entity<Article>()
                .Property(p => p.Expires)
                .HasConversion(nullableDateTimeConverter);

            modelBuilder.Entity<ArticleLog>()
                .Property(p => p.DateTimeStamp)
                .HasConversion(dateTimeConverter);

            modelBuilder.Entity<MenuItem>()
                .Property(b => b.Guid)
                .HasDefaultValueSql("newid()");

            modelBuilder.Entity<ArticleLock>()
                .Property(p => p.Id)
                .HasDefaultValueSql("newid()");

            modelBuilder.Entity<Config>()
                .Property(p => p.IsActive)
                .HasDefaultValue(false);

            modelBuilder.Entity<Config>()
                .Property(p => p.Created)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Config>()
                .Property(p => p.Updated)
                .HasDefaultValueSql("GETUTCDATE()");

            base.OnModelCreating(modelBuilder);
        }


        /// <inheritdoc />
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return SaveChanges(acceptAllChangesOnSuccess);
        }

        /// <inheritdoc />
        public override int SaveChanges()
        {
            return SaveChanges(null);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
        {
            return SaveChangesAsync(null, cancellationToken);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = new())
        {
            return SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        #region PRIVATE OVERRIDE METHODS

        /// <summary>
        ///     Uses the synchronous SaveChanges method.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <returns></returns>
        private int SaveChanges(bool? acceptAllChangesOnSuccess)
        {
            var count = acceptAllChangesOnSuccess.HasValue
                ? base.SaveChanges(acceptAllChangesOnSuccess.Value)
                : base.SaveChanges();


            SyncDatabases(new CancellationToken());

            return count;
        }

        /// <summary>
        ///     Uses the asynchronous base SaveChangesAsync method.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task<int> SaveChangesAsync(bool? acceptAllChangesOnSuccess, CancellationToken cancellationToken)
        {
            var task = acceptAllChangesOnSuccess.HasValue
                ? base.SaveChangesAsync(acceptAllChangesOnSuccess.Value, cancellationToken)
                : base.SaveChangesAsync(cancellationToken);

            task.Wait(cancellationToken);

            SyncDatabases(cancellationToken);

            return task;
        }

        private void SyncDatabases(CancellationToken cancellationToken)
        {
            if (_syncContext != null)
                GetLastSyncResult = _syncContext.SyncDatabases(SyncDirection.Bidirectional, cancellationToken).Result;
        }

        #endregion

        #endregion

        #region DbContext

        /// <summary>
        ///     Articles
        /// </summary>
        public DbSet<Article> Articles { get; set; }

        /// <summary>
        /// Article locks
        /// </summary>
        public DbSet<ArticleLock> ArticleLocks { get; set; }

        /// <summary>
        ///     Article activity logs
        /// </summary>
        public DbSet<ArticleLog> ArticleLogs { get; set; }

        /// <summary>
        ///     Configurations
        /// </summary>
        public DbSet<Config> Configs { get; set; }

        /// <summary>
        ///     Menu items
        /// </summary>
        public DbSet<MenuItem> MenuItems { get; set; }

        /// <summary>
        ///     Web page templates
        /// </summary>
        public DbSet<Template> Templates { get; set; }

        /// <summary>
        ///     Website layouts
        /// </summary>
        public DbSet<Layout> Layouts { get; set; }

        /// <summary>
        ///     Page teams
        /// </summary>
        public DbSet<Team> Teams { get; set; }

        /// <summary>
        ///     Team membership
        /// </summary>
        public DbSet<TeamMember> TeamMembers { get; set; }

        #endregion
    }
}