using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using EventManagement.Models;

namespace EventManagement.Controllers
{
    public class BookingsController : Controller
    {
        private EventManagementDBEntities1 db = new EventManagementDBEntities1();

        // 1. Xem danh sách vé đã đặt của tôi
        public ActionResult MyTickets()
        {
            var user = Session["User"] as Account;
            if (user == null)
                return RedirectToAction("Login", "Account");

            var bookings = db.Bookings
                             .Include(b => b.Event)
                             .Where(b => b.AccountID == user.AccountID)
                             .OrderByDescending(b => b.BookingDate)
                             .ToList();

            ViewBag.TotalSpent = bookings
                .Where(b => b.Status != "Cancelled")
                .Sum(b => (decimal?)b.TotalPrice) ?? 0;

            return View(bookings);
        }

        // 2. Xem chi tiết 1 đơn đặt vé (A05: chặn User B xem trộm đơn của User A)
        public ActionResult Details(int? id)
        {
            var user = Session["User"] as Account;
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var booking = db.Bookings
                            .Include(b => b.Event)
                            .Include(b => b.Account)
                            .FirstOrDefault(b => b.BookingID == id.Value);

            if (booking == null)
                return HttpNotFound();

            if (booking.AccountID != user.AccountID && user.Role != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền xem thông tin đặt vé của người dùng khác!";
                return RedirectToAction("MyTickets");
            }

            return View(booking);
        }

        // 3. Đặt vé (A03, A07, A08: Transaction + UPDLOCK, ROWLOCK + Atomic SQL UPDATE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BookTicket(int eventId, int quantity)
        {
            var user = Session["User"] as Account;
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng vé đặt phải lớn hơn 0!";
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            using (var tx = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    // Khóa dòng sự kiện đang đặt để hai tài khoản đặt vé cuối cùng lúc xếp hàng tuần tự
                    var ev = db.Events.SqlQuery(
                        "SELECT * FROM dbo.Events WITH (UPDLOCK, ROWLOCK) WHERE EventID = @p0", eventId)
                        .FirstOrDefault();

                    if (ev == null)
                    {
                        tx.Rollback();
                        return HttpNotFound();
                    }

                    // [NGHIỆP VỤ 1]: Hạn đăng ký
                    if (ev.StartDate.HasValue && ev.StartDate.Value <= DateTime.Now)
                    {
                        tx.Rollback();
                        TempData["Error"] = "Rất tiếc! Sự kiện này đã hết hạn đăng ký hoặc đã diễn ra.";
                        return RedirectToAction("Details", "Home", new { id = eventId });
                    }

                    // [NGHIỆP VỤ 2]: Chống đặt trùng (cho phép đặt lại nếu đơn cũ đã Cancelled - A03)
                    bool isAlreadyBooked = db.Bookings.Any(b => b.AccountID == user.AccountID
                                                             && b.EventID == eventId
                                                             && b.Status != "Cancelled");
                    if (isAlreadyBooked)
                    {
                        tx.Rollback();
                        TempData["Error"] = "Bạn đã đăng ký sự kiện này rồi! Không thể đăng ký trùng.";
                        return RedirectToAction("Details", "Home", new { id = eventId });
                    }

                    // [NGHIỆP VỤ 3 + A07]: Trừ số chỗ nguyên tử ở SQL Server, không bao giờ âm
                    int affected = db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.Events SET TotalTickets = TotalTickets - @p0 WHERE EventID = @p1 AND TotalTickets >= @p0",
                        quantity, eventId);

                    if (affected == 0)
                    {
                        tx.Rollback();
                        TempData["Error"] = "Không đủ chỗ! Sự kiện chỉ còn lại " + (ev.TotalTickets ?? 0) + " vé.";
                        return RedirectToAction("Details", "Home", new { id = eventId });
                    }

                    // [A08]: Luôn tính đơn giá tại Server từ TicketPrice trong DB
                    var booking = new Booking
                    {
                        AccountID = user.AccountID,
                        EventID = eventId,
                        Quantity = quantity,
                        TotalPrice = (ev.TicketPrice ?? 0) * quantity,
                        BookingDate = DateTime.Now,
                        Status = "Pending"
                    };

                    db.Bookings.Add(booking);
                    db.SaveChanges();
                    tx.Commit();

                    TempData["Success"] = $"Đặt vé tham dự sự kiện thành công! Mã đơn: #{booking.BookingID}";
                    return RedirectToAction("MyTickets");
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // 4. Hủy đăng ký và hoàn lại số chỗ (A04: chặn hủy vé người khác, chống hoàn 2 lần)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelTicket(int id)
        {
            var user = Session["User"] as Account;
            if (user == null)
                return RedirectToAction("Login", "Account");

            using (var tx = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var booking = db.Bookings.SqlQuery(
                        "SELECT * FROM dbo.Bookings WITH (UPDLOCK, ROWLOCK) WHERE BookingID = @p0", id)
                        .FirstOrDefault();

                    if (booking == null)
                    {
                        tx.Rollback();
                        TempData["Error"] = "Không tìm thấy đơn đặt vé!";
                        return RedirectToAction("MyTickets");
                    }

                    if (booking.AccountID != user.AccountID)
                    {
                        tx.Rollback();
                        TempData["Error"] = "Bạn không có quyền hủy đơn đặt vé của người dùng khác!";
                        return RedirectToAction("MyTickets");
                    }

                    if (booking.Status == "Cancelled")
                    {
                        tx.Rollback();
                        TempData["Error"] = "Đơn đặt vé này đã được hủy trước đó.";
                        return RedirectToAction("MyTickets");
                    }

                    booking.Status = "Cancelled";
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.Events SET TotalTickets = ISNULL(TotalTickets, 0) + @p0 WHERE EventID = @p1",
                        booking.Quantity, booking.EventID);

                    db.SaveChanges();
                    tx.Commit();

                    TempData["Success"] = $"Đã hủy đơn #{booking.BookingID} và hoàn lại {booking.Quantity} chỗ thành công!";
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return RedirectToAction("MyTickets");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}