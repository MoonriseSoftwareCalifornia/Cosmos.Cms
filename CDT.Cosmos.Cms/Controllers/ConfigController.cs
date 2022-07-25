using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services.Secrets;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CDT.Cosmos.Cms.Controllers
{
    /// <summary>
    /// Configuration controller for the Cosmos Editor
    /// </summary>
    public class ConfigController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SetupController> _logger;
        private readonly IOptions<CosmosConfig> _options;
        private readonly CosmosConfigStatus _cosmosStatus;
        private readonly SecretsClient _secretsClient;
        private readonly SqlDbSyncContext _syncContext;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        /// <param name="cosmosStatus"></param>
        /// <param name="secretsClient"></param>
        /// <param name="syncContext"></param>
        public ConfigController(ApplicationDbContext dbContext, ILogger<SetupController> logger, IOptions<CosmosConfig> options, CosmosConfigStatus cosmosStatus, SecretsClient secretsClient, SqlDbSyncContext syncContext)
        {
            _logger = logger;
            _options = options;
            _cosmosStatus = cosmosStatus;
            _secretsClient = secretsClient;
            _syncContext = syncContext;
            _dbContext = dbContext;

            if (_syncContext.IsConfigured())
                _dbContext.LoadSyncContext(_syncContext);

        }

        /// <summary>
        /// Home page
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }


        #region DATA API

        /// <summary>
        /// Get the list of configurations
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Configs([DataSourceRequest] DataSourceRequest request)
        {
            var query = _dbContext.Configs.Include(i => i.User.Email).Select(s => new ConfigListItemViewModel()
            {
                Id = s.Id,
                Created = s.Created,
                IsActive = s.IsActive,
                Notes = s.Notes,
                UserEmail = s.User.Email,
                UserId = s.UserId
            });
            return Json(await query.ToDataSourceResultAsync(request));
        }

        #endregion
    }
}
