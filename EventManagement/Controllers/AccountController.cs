using EventManagement.Helpers;
using EventManagement.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EventManagement.Controllers
{
    public class AccountController : Controller
    {
        private EventManagementDBEntities1 db = new EventManagementDBEntities1();

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "Username,Password,FullName,Email")] Account account)
        {
            if (string.IsNullOrWhiteSpace(account.Username))
                ModelState.AddModelError("Username", "Vui lòng nhập tên đăng nhập.");

            if (string.IsNullOrEmpty(account.Password))
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");

            if (string.IsNullOrWhiteSpace(account.FullName))
                ModelState.AddModelError("FullName", "Vui lòng nhập họ và tên.");

            if (ModelState.IsValid)
            {
                string cleanUsername = account.Username.Trim();
                bool exists = db.Accounts.Any(a => a.Username == cleanUsername);
                if (!exists)
                {
                    account.Username = cleanUsername;
                    account.FullName = account.FullName.Trim();
                    account.Email = string.IsNullOrWhiteSpace(account.Email) ? null : account.Email.Trim();
                    account.Role = "User";
                    account.Password = PasswordHelper.HashPassword(account.Password);

                    db.Accounts.Add(account);
                    db.SaveChanges();

                    Session["User"] = account;
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                }
            }

            return View(account);
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
                return View();
            }

            string cleanUsername = username.Trim();
            var account = db.Accounts.FirstOrDefault(a => a.Username == cleanUsername);

            if (account != null && PasswordHelper.VerifyPassword(password, account.Password))
            {
                Session["User"] = account;

                if (account.Role == "Admin")
                    return RedirectToAction("Index", "Events");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session["User"] = null;
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}