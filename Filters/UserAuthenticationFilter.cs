using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PrimeMarket.Filters
{
    public class UserAuthenticationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Check if user is logged in
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // User is not logged in, redirect to login page
                context.Result = new RedirectToActionResult("Login", "User", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}