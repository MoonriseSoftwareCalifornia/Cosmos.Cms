using CDT.Cosmos.Cms.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.UI.Services;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using System;
using CDT.Cosmos.Cms.Common.Data.Logic;

namespace CDT.Cosmos.Cms.Website.Controllers
{
    /// <summary>
    /// Contact Us Controller
    /// </summary>
    public class ContactUsController : Controller
    {

        private readonly EmailSender _emailSender;
        private readonly ILogger<ContactUsController> _logger;
        private readonly IOptions<CosmosConfig> _cosmosOptions;
        private readonly ApplicationDbContext _dbContext;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cosmosOptions"></param>
        /// <param name="logger"></param>
        /// <param name="emailSender"></param>
        /// <param name="articleLogic"></param>
        public ContactUsController(
            IOptions<CosmosConfig> cosmosOptions,
            ILogger<ContactUsController> logger,
            IEmailSender emailSender,
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _cosmosOptions = cosmosOptions;
            _logger = logger;
            _emailSender = (EmailSender)emailSender;
        }

        /// <summary>
        /// Contact us email form
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var articleLogic = new ArticleLogic(_dbContext, _cosmosOptions, false);
            var defaultLayout = await articleLogic.GetDefaultLayout("en");
            ViewData["layout"] = defaultLayout;

            return View(new EmailMessageViewModel());
        }


        /// <summary>
        /// Send Email Message
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EmailMessageViewModel model)
        {
            var articleLogic = new ArticleLogic(_dbContext, _cosmosOptions, false);
            var defaultLayout = await articleLogic.GetDefaultLayout("en");
            ViewData["layout"] = defaultLayout;

            if (ModelState.IsValid)
            {
                try
                {
                    await _emailSender.SendEmailAsync(_cosmosOptions.Value.SendGridConfig.EmailFrom,
                        model.Subject, model.Content, model.FromEmail);

                    model.SendSuccess = true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                }
            }

            return View(model);
        }

    }
}
