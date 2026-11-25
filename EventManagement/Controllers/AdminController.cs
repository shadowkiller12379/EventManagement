using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using EventManagement.Filters;
using EventManagement.Models;

namespace EventManagement.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private EventManagementDBEntities1 db = new EventManagementDBEntities1();

        public ActionResult Index()
        {
            ViewBag.TotalEvents = db.Events.Count();
            ViewBag.TotalCategories = db.Categories.Count();
            ViewBag.TotalBookings = db.Bookings.Count();
            ViewBag.PendingBookings = db.Bookings.Count(b => b.Status == "Pending");
            ViewBag.ApprovedBookings = db.Bookings.Count(b => b.Status == "Approved");
            ViewBag.CancelledBookings = db.Bookings.Count(b => b.Status == "Cancelled");
            ViewBag.TotalRevenue = db.Bookings
                .Where(b => b.Status != "Cancelled")
                .Sum(b => (decimal?)b.TotalPrice) ?? 0;

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
            // A06: Chặn mọi trạng thái ngoài Approved / Cancelled (ví dụ Approved -> Pending)
            if (status != "Approved" && status != "Cancelled")
            {
                TempData["Error"] = $"Không thể chuyển trạng thái đơn #{id} sang '{status}'. Chỉ chấp nhận Approved hoặc Cancelled.";
                return RedirectToLocalOrBookingList(returnUrl);
            }

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
                        return HttpNotFound();
                    }

                    // A02 & A06: Đơn đã hủy thì không được duyệt lại hay hoàn chỗ lần 2
                    if (booking.Status == "Cancelled")
                    {
                        tx.Rollback();
                        TempData["Error"] = $"Đơn #{booking.BookingID} đã bị hủy trước đó, không thể đổi sang '{status}' hay hoàn chỗ lần 2.";
                        return RedirectToLocalOrBookingList(returnUrl);
                    }

                    // A06: Chặn duyệt lặp đơn đã Approved
                    if (booking.Status == "Approved" && status == "Approved")
                    {
                        tx.Rollback();
                        TempData["Error"] = $"Đơn #{booking.BookingID} đã ở trạng thái Approved trước đó.";
                        return RedirectToLocalOrBookingList(returnUrl);
                    }

                    if (status == "Cancelled")
                    {
                        // TC37 / A01: Hủy đơn Pending hoặc Approved -> hoàn đúng số chỗ
                        booking.Status = "Cancelled";
                        db.Database.ExecuteSqlCommand(
                            "UPDATE dbo.Events SET TotalTickets = ISNULL(TotalTickets, 0) + @p0 WHERE EventID = @p1",
                            booking.Quantity, booking.EventID);

                        db.SaveChanges();
                        tx.Commit();
                        TempData["Success"] = $"Đã hủy đơn #{booking.BookingID} và hoàn lại {booking.Quantity} chỗ.";
                    }
                    else if (status == "Approved" && booking.Status == "Pending")
                    {
                        booking.Status = "Approved";
                        db.SaveChanges();
                        tx.Commit();
                        TempData["Success"] = $"Đã duyệt đơn #{booking.BookingID} thành công.";
                    }
                    else
                    {
                        tx.Rollback();
                        TempData["Error"] = $"Không thể chuyển trạng thái từ '{booking.Status}' sang '{status}'.";
                    }
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return RedirectToLocalOrBookingList(returnUrl);
        }

        private ActionResult RedirectToLocalOrBookingList(string returnUrl)
        {
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