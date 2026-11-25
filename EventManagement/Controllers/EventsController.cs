using System.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EventManagement.Models;
using System.Data.Entity;

namespace EventManagement.Controllers
{
    public class EventsController : Controller
    {
        private EventManagementDBEntities1 db = new EventManagementDBEntities1();

        // GET: Events
        public ActionResult Index()
        {
            var events = db.Events.Include(e => e.Category);
            return View(events.ToList());
        }

        // GET: Events/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = db.Events.Find(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            return View(@event);
        }

        // GET: Events/Create
        public ActionResult Create()
        {
            var user = Session["User"] as EventManagement.Models.Account;
            if (user == null || user.Role != "Admin")
            {
                // Nếu chưa đăng nhập hoặc không phải Admin -> Đẩy về trang Login
                return RedirectToAction("Login", "Account");
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Events/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EventID,Title,Description,Location,StartDate,EndDate,TicketPrice,TotalTickets,CategoryID")] Event @event, HttpPostedFileBase ImageFile)
        {
            var user = Session["User"] as EventManagement.Models.Account;
            if (user == null || user.Role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            ValidateEvent(@event);
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                var imgError = ValidateImage(ImageFile);
                if (imgError != null)
                    ModelState.AddModelError("Image", imgError);
            }
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    
                    @event.Image = SaveImage(ImageFile);
                }

                db.Events.Add(@event);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", @event.CategoryID);
            return View(@event);
        }

        // GET: Events/Edit/5
        public ActionResult Edit(int? id)
        {
            var user = Session["User"] as EventManagement.Models.Account;
            if (user == null || user.Role != "Admin")
            {
                // Nếu chưa đăng nhập hoặc không phải Admin -> Đẩy về trang Login
                return RedirectToAction("Login", "Account");
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = db.Events.Find(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", @event.CategoryID);
            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EventID,Title,Description,Image,Location,StartDate,EndDate,TicketPrice,TotalTickets,CategoryID")] Event @event, HttpPostedFileBase ImageFile)
        {
            // Kiểm tra quyền Admin (giữ nguyên nếu bạn đang dùng)
            var user = Session["User"] as EventManagement.Models.Account;
            if (user == null || user.Role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            ValidateEvent(@event);

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                var imgError = ValidateImage(ImageFile);
                if (imgError != null)
                    ModelState.AddModelError("Image", imgError);
            }
            if (ModelState.IsValid)
            {
                // 1. Dùng AsNoTracking() để lấy dữ liệu sự kiện cũ ra mà không bị khóa
                var existingEvent = db.Events.AsNoTracking().FirstOrDefault(e => e.EventID == @event.EventID);

                // 2. Xử lý ảnh
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    // Có chọn ảnh mới -> lưu với tên ngẫu nhiên
                    @event.Image = SaveImage(ImageFile);
                }
                else
                {
                    // Nếu không chọn tệp mới -> Lấy lại tên ảnh cũ đắp vào, chống mất ảnh
                    if (existingEvent != null)
                    {
                        @event.Image = existingEvent.Image;
                    }
                }

                // 3. Cập nhật các thông tin khác vào CSDL
                db.Entry(@event).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", @event.CategoryID);
            return View(@event);
        }

        // GET: Events/Delete/5
        public ActionResult Delete(int? id)
        {
            var user = Session["User"] as EventManagement.Models.Account;
            if (user == null || user.Role != "Admin")
            {
                // Nếu chưa đăng nhập hoặc không phải Admin -> Đẩy về trang Login
                return RedirectToAction("Login", "Account");
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = db.Events.Find(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            
            var user = Session["User"] as EventManagement.Models.Account;
            if (user == null || user.Role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            Event @event = db.Events.Find(id);
            if (@event != null)
            {
                
                var relatedBookings = db.Bookings.Where(b => b.EventID == id).ToList();
                if (relatedBookings.Any())
                {
                    db.Bookings.RemoveRange(relatedBookings);
                }

                
                db.Events.Remove(@event);

                
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        private void ValidateEvent(Event e)
        {
            if (e.TicketPrice.HasValue && e.TicketPrice.Value < 0)
                ModelState.AddModelError("TicketPrice", "Giá vé không được âm.");

            if (e.TotalTickets.HasValue && e.TotalTickets.Value < 0)
                ModelState.AddModelError("TotalTickets", "Tổng số vé không được âm.");

            if (e.StartDate.HasValue && e.EndDate.HasValue && e.EndDate.Value < e.StartDate.Value)
                ModelState.AddModelError("EndDate", "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const int MaxImageSize = 2 * 1024 * 1024; // 2 MB

        // Trả về thông báo lỗi, hoặc null nếu ảnh hợp lệ
        private string ValidateImage(HttpPostedFileBase file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return "Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .gif.";

            if (file.ContentLength > MaxImageSize)
                return "Ảnh không được vượt quá 2 MB.";

            // Kiểm tra nội dung thật sự là ảnh (chặn đổi đuôi .txt thành .jpg)
            try
            {
                using (var img = System.Drawing.Image.FromStream(file.InputStream)) { }
            }
            catch
            {
                return "Tệp không phải là ảnh hợp lệ.";
            }
            finally
            {
                file.InputStream.Position = 0; // trả con trỏ về đầu để còn lưu tệp
            }
            return null;
        }

        // Lưu ảnh với tên ngẫu nhiên, trả về tên tệp để ghi vào CSDL
        private string SaveImage(HttpPostedFileBase file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var path = Path.Combine(Server.MapPath("~/Content/images"), fileName);
            file.SaveAs(path);
            return fileName;
        }
    }
}