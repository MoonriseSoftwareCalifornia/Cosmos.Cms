using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

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

        /// <summary>
        /// Diagnostics display
        /// </summary>
        /// <returns></returns>
        public IActionResult index()
        {

            if (_options.Value.SiteSettings.AllowSetup ?? false)
            {
                return View("~/Views/Setup/Index.cshtml", _cosmosStatus);
            }

            _logger.LogError("Unauthorized access attempted.", new Exception("Unauthorized access attempted."));

            return Unauthorized();
        }

    }
}