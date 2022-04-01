using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CDT.Cosmos.Cms.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Lockout page model
    /// </summary>
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        /// <summary>
        /// On get method handler
        /// </summary>
        public void OnGet()
        {
        }
    }
}