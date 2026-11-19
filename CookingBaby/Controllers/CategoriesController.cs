using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CookingBaby.Models;
using CookingBaby.Infrastructure;

namespace CookingBaby.Controllers
{
    [SessionAuthorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private CookingBabyDBEntities db = new CookingBabyDBEntities();

        // GET: Categories
        public ActionResult Index()
        {
            ViewBag.ActiveTab = "Categories";
            ViewBag.IsAdminPage = true;
            return View(db.Categories.ToList());
        }

        // GET: Categories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // GET: Categories/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CategoryId,CategoryName,ThemeColor,DisplayOrder")] Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.CategoryName = (category.CategoryName ?? string.Empty).Trim();
            ModelState.Remove(nameof(category.CategoryName));

            if (CategoryNameExists(category.CategoryName))
            {
                ModelState.AddModelError(nameof(category.CategoryName), "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.");
                return View(category);
            }

            try
            {
                db.Categories.Add(category);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm danh mục thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                AddCategorySaveError(ex);
                return View(category);
            }

        }

        // GET: Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CategoryId,CategoryName,ThemeColor,DisplayOrder")] Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.CategoryName = (category.CategoryName ?? string.Empty).Trim();
            ModelState.Remove(nameof(category.CategoryName));

            if (CategoryNameExists(category.CategoryName, category.CategoryId))
            {
                ModelState.AddModelError(nameof(category.CategoryName), "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.");
                return View(category);
            }

            try
            {
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật danh mục thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                AddCategorySaveError(ex);
                return View(category);
            }
        }

        private bool CategoryNameExists(string categoryName, int? excludedCategoryId = null)
        {
            return db.Categories.Any(c => c.CategoryName == categoryName &&
                (!excludedCategoryId.HasValue || c.CategoryId != excludedCategoryId.Value));
        }

        private void AddCategorySaveError(DbUpdateException exception)
        {
            var sqlException = exception.GetBaseException() as SqlException;
            bool isDuplicateKey = sqlException != null &&
                sqlException.Errors.Cast<SqlError>().Any(error => error.Number == 2601 || error.Number == 2627);

            if (isDuplicateKey)
            {
                ModelState.AddModelError(nameof(Category.CategoryName), "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.");
                return;
            }

            ModelState.AddModelError(string.Empty, "Không thể lưu danh mục lúc này. Vui lòng kiểm tra thông tin và thử lại.");
        }

        // GET: Categories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            // Kiểm tra xem danh mục có đang được công thức sử dụng không
            bool isUsed = db.Recipes.Any(r => r.CategoryId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa danh mục này vì đang có công thức thuộc danh mục.";
                return RedirectToAction("Index");
            }
            try
            {
                db.Categories.Remove(category);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Xóa danh mục thành công.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục vì dữ liệu đang được sử dụng. Hãy làm mới trang và thử lại.";
            }
            return RedirectToAction("Index");
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
