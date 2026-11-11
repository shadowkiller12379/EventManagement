using EventManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EventManagement.Controllers
{
    public class AccountController : Controller
    {
        private EventManagementDBEntities db = new EventManagementDBEntities();

        // HÀM HỖ TRỢ: Mã hóa MD5 cho mật khẩu
        public static string GetMD5(string str)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] b = System.Text.Encoding.UTF8.GetBytes(str);
                byte[] hash = md5.ComputeHash(b);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (byte x in hash)
                {
                    sb.Append(x.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public ActionResult Register()
        {
            return View();
        }

        // DANG KY (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Account account)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem Username đã tồn tại chưa
                var check = db.Accounts.FirstOrDefault(a => a.Username == account.Username);
                if (check == null)
                {
                    // Mặc định tài khoản đăng ký mới có Role là 'User'
                    account.Role = "User";

                    // 1. Mã hóa mật khẩu sang MD5 trước khi lưu xuống CSDL
                    account.Password = GetMD5(account.Password);

                    db.Accounts.Add(account);
                    db.SaveChanges();

                    // Đăng ký xong tự động lưu Session Đăng nhập luôn
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

        // DANG NHAP (GET)
        public ActionResult Login()
        {
            return View();
        }

        // DANG NHAP (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            // 1. Mã hóa password người dùng vừa nhập nhập trên form ra MD5
            string md5Pass = GetMD5(password);

            // 2. Tìm tài khoản khớp Username và Password ĐÃ MÃ HÓA trong Database
            var account = db.Accounts.FirstOrDefault(a => a.Username == username && a.Password == md5Pass);

            if (account != null)
            {
                // Lưu thông tin người dùng vào Session
                Session["User"] = account;

                // Nếu là Admin thì chuyển hướng về trang Quản lý Sự kiện, ngược lại về Trang chủ
                if (account.Role == "Admin")
                {
                    return RedirectToAction("Index", "Events");
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        // DANG XUAT
        public ActionResult Logout()
        {
            Session["User"] = null; // Xóa Session
            return RedirectToAction("Index", "Home");
        }

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }
    }
}