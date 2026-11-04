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
        private EventManagementDBEntities db = new EventManagementDBEntities();

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

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/images"), fileName);
                    ImageFile.SaveAs(path);
                    @event.Image = fileName;
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

            if (ModelState.IsValid)
            {
                // 1. Dùng AsNoTracking() để lấy dữ liệu sự kiện cũ ra mà không bị khóa
                var existingEvent = db.Events.AsNoTracking().FirstOrDefault(e => e.EventID == @event.EventID);

                // 2. Xử lý ảnh
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    // Nếu người dùng có chọn tệp ảnh mới -> Lưu đè lên
                    string fileName = System.IO.Path.GetFileName(ImageFile.FileName);
                    string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/"), fileName);
                    ImageFile.SaveAs(path);

                    // Cập nhật tên file ảnh mới cho database
                    @event.Image = fileName;
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
    }
}