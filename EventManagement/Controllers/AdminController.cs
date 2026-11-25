using EventManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace EventManagement.Controllers
{
    public class AdminController : Controller
    {
        private EventManagementDBEntities1 db = new EventManagementDBEntities1();

        // Chạy trước mọi action trong controller này: kiểm tra đăng nhập + quyền Admin
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = Session["User"] as Account;

            if (user == null)
                filterContext.Result = RedirectToAction("Login", "Account");
            else if (user.Role != "Admin")
                filterContext.Result = RedirectToAction("Index", "Home");

            base.OnActionExecuting(filterContext);
        }

       
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult BookingList()
        {
            var bookings = db.Bookings
                .Include(b => b.Account)
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .ToList();

            return View(bookings);
        }
        public ActionResult AttendeeList(int? eventId)
        {
            IQueryable<Booking> bookings = db.Bookings
                .Include(b => b.Account)
                .Include(b => b.Event);

            if (eventId.HasValue)
                bookings = bookings.Where(b => b.EventID == eventId.Value);

            ViewBag.Events = new SelectList(db.Events, "EventID", "Title", eventId);

            return View(bookings.OrderByDescending(b => b.BookingDate).ToList());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string status, string returnUrl)
        {
            if (status != "Approved" && status != "Cancelled")
                return new HttpStatusCodeResult(400);

            var booking = db.Bookings.Find(id);
            if (booking == null)
                return HttpNotFound();

            // Đơn đã hủy thì không đổi nữa (số chỗ đã được hoàn lại trước đó)
            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Đơn này đã bị hủy, không thể thay đổi trạng thái.";
            }
            else
            {
                // Hủy đơn Pending/Approved -> hoàn lại đúng số chỗ của đơn
                if (status == "Cancelled")
                {
                    var ev = db.Events.Find(booking.EventID);
                    if (ev != null)
                        ev.TotalTickets = (ev.TotalTickets ?? 0) + booking.Quantity;
                }

                booking.Status = status;
                db.SaveChanges();   // một lần: đổi trạng thái và hoàn chỗ cùng thành công hoặc cùng thất bại
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("BookingList");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        
    }
}