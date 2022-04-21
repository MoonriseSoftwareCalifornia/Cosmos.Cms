using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Logout page model
    /// </summary>
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="signInManager"></param>
        /// <param name="logger"></param>
        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Handle GET method
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Handle POST method
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
                return LocalRedirect(returnUrl);
            return RedirectToPage();
        }
    }
}