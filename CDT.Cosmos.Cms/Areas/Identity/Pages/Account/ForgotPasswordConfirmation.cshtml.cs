using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CDT.Cosmos.Cms.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Forgot password confirmation page model
    /// </summary>
    [AllowAnonymous]
    public class ForgotPasswordConfirmation : PageModel
    {
        /// <summary>
        /// On get method handler
        /// </summary>
        public void OnGet()
        {
        }
    }
}