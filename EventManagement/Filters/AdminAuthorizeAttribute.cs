using System.Web.Mvc;
using System.Web.Routing;
using EventManagement.Models;

namespace EventManagement.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = filterContext.HttpContext.Session?["User"] as Account;

            if (user == null)
            {
                filterContext.Controller.TempData["Error"] = "Vui lòng đăng nhập bằng tài khoản Quản trị viên.";
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "Login" }));
            }
            else if (user.Role != "Admin")
            {
                filterContext.Controller.TempData["Error"] = "Bạn không có quyền truy cập chức năng quản trị.";
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
