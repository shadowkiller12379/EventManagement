using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using EventManagement.Models;

namespace EventManagement.Controllers
{
    public class HomeController : Controller
    {
        private EventManagementDBEntities1 db = new EventManagementDBEntities1();

        public ActionResult Index(string searchString, int? categoryId, DateTime? fromDate, DateTime? toDate)
        {
            var events = db.Events.Include(e => e.Category).AsQueryable();

            // Tìm kiếm theo nhiều từ khóa (tách theo khoảng trắng, mỗi từ khớp Title hoặc Location)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keywords = searchString.Trim()
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var kw in keywords)
                {
                    string term = kw;
                    events = events.Where(e => e.Title.Contains(term) || e.Location.Contains(term));
                }
            }

            // Lọc theo Danh mục sự kiện
            if (categoryId.HasValue)
            {
                events = events.Where(e => e.CategoryID == categoryId.Value);
            }

            // Lọc theo ngày bắt đầu (A15)
            if (fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                events = events.Where(e => e.StartDate >= start);
            }

            if (toDate.HasValue)
            {
                DateTime endExclusive = toDate.Value.Date.AddDays(1);
                events = events.Where(e => e.StartDate < endExclusive);
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", categoryId);
            ViewBag.CurrentFilter = searchString;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(events.OrderByDescending(e => e.StartDate).ToList());
        }

        // TRANG CHI TIẾT SỰ KIỆN
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Event @event = db.Events.Include(e => e.Category).FirstOrDefault(e => e.EventID == id);
            if (@event == null)
                return HttpNotFound();

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
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}