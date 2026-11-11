using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using EventManagement.Models;

namespace EventManagement.Controllers
{
    public class BookingsController : Controller
    {
        private EventManagementDBEntities db = new EventManagementDBEntities();

        // 1. Xem danh sách vé đã đặt (Mục 8-4: 0.20đ)
        public ActionResult MyTickets()
        {
            var user = Session["User"] as Account;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = db.Bookings
                             .Include(b => b.Event)
                             .Where(b => b.AccountID == user.AccountID)
                             .OrderByDescending(b => b.BookingDate)
                             .ToList();
            ViewBag.TotalSpent = bookings.Where(b => b.Status != "Cancelled").Sum(b => (decimal?)b.TotalPrice) ?? 0;
            return View(bookings);
        }

        // 2. Đặt vé + Kiểm tra đủ 3 Business Logic của Nhóm N12 (Mục 8-3, 8-5, 8-6: 0.95đ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BookTicket(int eventId, int quantity)
        {
            var user = Session["User"] as Account;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ev = db.Events.Find(eventId);
            if (ev == null)
            {
                return HttpNotFound();
            }

            // [NGHIỆP VỤ 1 - Mục 8-6]: Kiểm tra hạn đăng ký (Không đăng ký sau khi sự kiện đã bắt đầu/kết thúc)
            if (ev.StartDate.HasValue && ev.StartDate.Value <= DateTime.Now)
            {
                TempData["Error"] = "Rất tiếc! Sự kiện này đã hết hạn đăng ký hoặc đã diễn ra.";
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            // [NGHIỆP VỤ 2]: Không cho đăng ký trùng cùng 1 sự kiện (khi vé cũ chưa bị hủy)
            bool isAlreadyBooked = db.Bookings.Any(b => b.AccountID == user.AccountID
                                                     && b.EventID == eventId
                                                     && b.Status != "Cancelled");
            if (isAlreadyBooked)
            {
                TempData["Error"] = "Bạn đã đăng ký sự kiện này rồi! Không thể đăng ký trùng.";
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            // [NGHIỆP VỤ 3 - Mục 8-5]: Kiểm tra giới hạn số chỗ (Không đặt số lượng <= 0 hoặc vượt quá số vé còn lại)
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng vé đặt phải lớn hơn 0!";
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            if (ev.TotalTickets == null || quantity > ev.TotalTickets)
            {
                TempData["Error"] = "Không đủ chỗ! Sự kiện chỉ còn lại " + (ev.TotalTickets ?? 0) + " vé.";
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            // Nếu hợp lệ hết -> Lưu đơn đặt vé và trừ số chỗ còn lại
            Booking booking = new Booking
            {
                AccountID = user.AccountID,
                EventID = eventId,
                Quantity = quantity,
                TotalPrice = (ev.TicketPrice ?? 0) * quantity,
                BookingDate = DateTime.Now,
                Status = "Pending"
            };

            ev.TotalTickets = ev.TotalTickets - quantity;

            db.Bookings.Add(booking);
            db.SaveChanges();

            TempData["Success"] = "Đặt vé tham dự sự kiện thành công!";
            return RedirectToAction("MyTickets");
        }

        // 3. Hủy đăng ký và hoàn lại số chỗ (Mục 8-4)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelTicket(int id)
        {
            var user = Session["User"] as Account;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = db.Bookings.Find(id);
            if (booking != null && booking.AccountID == user.AccountID && booking.Status != "Cancelled")
            {
                booking.Status = "Cancelled";

                var ev = db.Events.Find(booking.EventID);
                if (ev != null)
                {
                    ev.TotalTickets = (ev.TotalTickets ?? 0) + booking.Quantity;
                }

                db.SaveChanges();
                TempData["Success"] = "Đã hủy đăng ký và hoàn lại chỗ thành công!";
            }

            return RedirectToAction("MyTickets");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}