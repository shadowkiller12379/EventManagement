using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Net;
using EventManagement.Models;

namespace EventManagement.Controllers
{
    public class HomeController : Controller
    {
        
        private EventManagementDBEntities db = new EventManagementDBEntities();

        
        public ActionResult Index(string searchString, int? categoryId)
        {
            var events = db.Events.Include(e => e.Category).AsQueryable();

            // Tìm kiếm theo tên sự kiện hoặc địa điểm
            if (!string.IsNullOrEmpty(searchString))
            {
                events = events.Where(e => e.Title.Contains(searchString) || e.Location.Contains(searchString));
            }

            // Lọc theo Danh mục sự kiện
            if (categoryId.HasValue)
            {
                events = events.Where(e => e.CategoryID == categoryId.Value);
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", categoryId);
            ViewBag.CurrentFilter = searchString;

            return View(events.OrderByDescending(e => e.StartDate).ToList());
        }

        // TRANG CHI TIẾT SỰ KIỆN
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Event @event = db.Events.Include(e => e.Category).FirstOrDefault(e => e.EventID == id);
            if (@event == null)
            {
                return HttpNotFound();
            }

            return View(@event);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
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