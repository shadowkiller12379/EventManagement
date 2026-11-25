using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EventManagement.Filters;
using EventManagement.Models;

namespace EventManagement.Controllers
{
    [AdminAuthorize]
    public class EventsController : Controller
    {
        private EventManagementDBEntities1 db = new EventManagementDBEntities1();
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const int MaxImageSize = 2 * 1024 * 1024; // 2 MB

        // GET: Events
        public ActionResult Index()
        {
            var events = db.Events.Include(e => e.Category).OrderByDescending(e => e.EventID);
            return View(events.ToList());
        }

        // GET: Events/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Event @event = db.Events.Include(e => e.Category).FirstOrDefault(e => e.EventID == id);
            if (@event == null)
                return HttpNotFound();

            return View(@event);
        }

        // GET: Events/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "EventID,Title,Description,Location,StartDate,EndDate,TicketPrice,TotalTickets,CategoryID")] Event @event,
            HttpPostedFileBase ImageFile)
        {
            ValidateEvent(@event, isCreate: true);

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                var imgError = ValidateImage(ImageFile);
                if (imgError != null)
                    ModelState.AddModelError("Image", imgError);
            }

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                    @event.Image = SaveImage(ImageFile);

                db.Events.Add(@event);
                db.SaveChanges();
                TempData["Success"] = "Tạo sự kiện mới thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", @event.CategoryID);
            return View(@event);
        }

        // GET: Events/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Event @event = db.Events.Find(id);
            if (@event == null)
                return HttpNotFound();

            int soldTickets = db.Bookings
                .Where(b => b.EventID == @event.EventID && b.Status != "Cancelled")
                .Sum(b => (int?)b.Quantity) ?? 0;

            ViewBag.SoldTickets = soldTickets;
            ViewBag.TotalCapacity = soldTickets + (@event.TotalTickets ?? 0);
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", @event.CategoryID);
            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include = "EventID,Title,Description,Image,Location,StartDate,EndDate,TicketPrice,TotalTickets,CategoryID")] Event @event,
            int? totalCapacity,
            HttpPostedFileBase ImageFile)
        {
            int soldTickets = db.Bookings
                .Where(b => b.EventID == @event.EventID && b.Status != "Cancelled")
                .Sum(b => (int?)b.Quantity) ?? 0;

            ValidateEvent(@event, isCreate: false);

            // A16: Kiểm soát sức chứa không âm và không thấp hơn số vé đã đặt hợp lệ
            int newRemainingTickets;
            if (totalCapacity.HasValue)
            {
                ModelState.Remove("TotalTickets");
                if (totalCapacity.Value < 0)
                {
                    ModelState.AddModelError("TotalTickets", "Tổng sức chứa không được âm.");
                    newRemainingTickets = @event.TotalTickets ?? 0;
                }
                else if (totalCapacity.Value < soldTickets)
                {
                    ModelState.AddModelError("TotalTickets",
                        $"Tổng sức chứa mới ({totalCapacity.Value}) không được nhỏ hơn số vé đã đặt hợp lệ ({soldTickets}).");
                    newRemainingTickets = @event.TotalTickets ?? 0;
                }
                else
                {
                    newRemainingTickets = totalCapacity.Value - soldTickets;
                }
            }
            else
            {
                newRemainingTickets = @event.TotalTickets ?? 0;
            }

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                var imgError = ValidateImage(ImageFile);
                if (imgError != null)
                    ModelState.AddModelError("Image", imgError);
            }

            var existingEvent = db.Events.Find(@event.EventID);
            if (existingEvent == null)
                return HttpNotFound();

            if (ModelState.IsValid)
            {
                existingEvent.Title = @event.Title;
                existingEvent.Description = @event.Description;
                existingEvent.Location = @event.Location;
                existingEvent.StartDate = @event.StartDate;
                existingEvent.EndDate = @event.EndDate;
                existingEvent.TicketPrice = @event.TicketPrice;
                existingEvent.TotalTickets = newRemainingTickets;
                existingEvent.CategoryID = @event.CategoryID;

                if (ImageFile != null && ImageFile.ContentLength > 0)
                    existingEvent.Image = SaveImage(ImageFile);

                db.SaveChanges();
                TempData["Success"] = "Cập nhật sự kiện thành công!";
                return RedirectToAction("Index");
            }

            @event.Image = existingEvent.Image;
            ViewBag.SoldTickets = soldTickets;
            ViewBag.TotalCapacity = totalCapacity ?? (soldTickets + (existingEvent.TotalTickets ?? 0));
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", @event.CategoryID);
            return View(@event);
        }

        // GET: Events/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Event @event = db.Events.Include(e => e.Category).FirstOrDefault(e => e.EventID == id);
            if (@event == null)
                return HttpNotFound();

            ViewBag.BookingCount = db.Bookings.Count(b => b.EventID == @event.EventID);
            ViewBag.ActiveBookingCount = db.Bookings.Count(b => b.EventID == @event.EventID && b.Status != "Cancelled");
            return View(@event);
        }

        // POST: Events/Delete/5 (A11: Bảo toàn lịch sử giao dịch, không xóa mù Bookings)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Event @event = db.Events.Find(id);
            if (@event == null)
                return HttpNotFound();

            int bookingCount = db.Bookings.Count(b => b.EventID == id);
            if (bookingCount > 0)
            {
                TempData["Error"] = $"Không thể xóa sự kiện '{@event.Title}' vì đã phát sinh {bookingCount} đơn đặt vé. " +
                                    "Hệ thống giữ lại sự kiện để bảo toàn lịch sử giao dịch (có thể sửa sức chứa bằng số vé đã bán để đóng nhận thêm vé).";
                return RedirectToAction("Delete", new { id = id });
            }

            db.Events.Remove(@event);
            db.SaveChanges();
            TempData["Success"] = $"Đã xóa sự kiện '{@event.Title}' thành công!";
            return RedirectToAction("Index");
        }

        private void ValidateEvent(Event e, bool isCreate)
        {
            if (e.TicketPrice.HasValue && e.TicketPrice.Value < 0)
                ModelState.AddModelError("TicketPrice", "Giá vé không được âm.");

            if (isCreate && (!e.TotalTickets.HasValue || e.TotalTickets.Value < 1))
                ModelState.AddModelError("TotalTickets", "Tổng số vé khởi tạo phải từ 1 trở lên.");
            else if (!isCreate && e.TotalTickets.HasValue && e.TotalTickets.Value < 0)
                ModelState.AddModelError("TotalTickets", "Số vé còn lại không được âm.");

            if (e.StartDate.HasValue && e.EndDate.HasValue && e.EndDate.Value <= e.StartDate.Value)
                ModelState.AddModelError("EndDate", "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }

        private string ValidateImage(HttpPostedFileBase file)
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                return "Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .gif.";

            if (file.ContentLength > MaxImageSize)
                return "Ảnh không được vượt quá 2 MB.";

            try
            {
                using (var img = System.Drawing.Image.FromStream(file.InputStream)) { }
            }
            catch
            {
                return "Tệp tải lên không phải là hình ảnh hợp lệ.";
            }
            finally
            {
                file.InputStream.Position = 0;
            }
            return null;
        }

        private string SaveImage(HttpPostedFileBase file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var dir = Server.MapPath("~/Content/images");
            Directory.CreateDirectory(dir);
            file.SaveAs(Path.Combine(dir, fileName));
            return fileName;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}