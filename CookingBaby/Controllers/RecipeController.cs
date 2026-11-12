using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CookingBaby.Models;
using CookingBaby.Infrastructure;

namespace CookingBaby.Controllers
{
    public class RecipeController : Controller
    {
        private CookingBabyDBEntities db = new CookingBabyDBEntities();

        private int CurrentUserId { get { return (int)Session["UserID"]; } }

        // 1. TAB QUẢN LÝ CÁ NHÂN (MÓN CỦA TÔI)
        [SessionAuthorize]
        public ActionResult MyRecipes()
        {
            int chefId = CurrentUserId;

            string query = "SELECT * FROM dbo.vw_RecipeCards WHERE AuthorId = @p0 AND Status != N'Đã ẩn' ORDER BY CreatedAt DESC";
            var myRecipes = db.Database.SqlQuery<RecipeCardViewModel>(query, chefId).ToList();

            return View(myRecipes);
        }

        public ActionResult Details(int id)
        {
            var recipe = db.Recipes
                .Include(r => r.Category)
                .Include(r => r.User1)
                .FirstOrDefault(r => r.RecipeId == id);

            if (recipe == null || (recipe.Status != "Đã duyệt" &&
                !(Session["Role"] as string == "Admin" ||
                  (Session["UserID"] is int && (int)Session["UserID"] == recipe.AuthorId))))
                return HttpNotFound();
            return View(recipe);
        }

        [HttpGet]
        public JsonResult GetRecipeDetail(int id)
        {
            try
            {
                var recipe = db.Recipes
                    .Include(r => r.Category)
                    .Include(r => r.User1)
                    .FirstOrDefault(r => r.RecipeId == id);

                if (recipe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy công thức." }, JsonRequestBehavior.AllowGet);
                }

                int currentUserId = Session["UserID"] is int ? (int)Session["UserID"] : 0;
                if (recipe.Status != "Đã duyệt" &&
                    !(Session["Role"] as string == "Admin" || currentUserId == recipe.AuthorId))
                    return Json(new { success = false, message = "Không tìm thấy công thức." }, JsonRequestBehavior.AllowGet);

                bool isSaved = db.SavedRecipes.Any(s => s.UserId == currentUserId && s.RecipeId == id);

                var ingredients = db.RecipeIngredients
                    .Where(ri => ri.RecipeId == id)
                    .OrderBy(ri => ri.SortOrder)
                    .Select(ri => ri.IngredientName)
                    .ToList();

                var steps = db.Database.SqlQuery<string>(
                    "SELECT Instruction FROM dbo.RecipeSteps WHERE RecipeId = @p0 ORDER BY StepNumber ASC",
                    id
                ).ToList();

                var ratings = db.Ratings.Where(r => r.RecipeId == id);
                double avgRating = ratings.Any() ? (double)ratings.Average(r => r.Stars) : 0;
                int reviewCount = ratings.Count();

                var data = new
                {
                    success = true,
                    recipeId = recipe.RecipeId,
                    title = recipe.Title,
                    categoryName = recipe.Category != null ? recipe.Category.CategoryName : "Món ngon",
                    cookTime = recipe.CookTime,
                    description = recipe.Description,
                    authorName = recipe.User1 != null ? recipe.User1.DisplayName : "Siêu đầu bếp nhí",
                    authorId = recipe.AuthorId,
                    currentUserId = currentUserId,
                    isSaved = isSaved,
                    imageUrl = recipe.ImageUrl,
                    avgRating = avgRating,
                    reviewCount = reviewCount,
                    ingredients = ingredients,
                    steps = steps
                };

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 2. THÊM MÓN MỚI (LƯU ẢNH VÀO /Uploads/Recipes/ VỚI Guid + 1-N INGREDIENTS & STEPS)
        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRecipe(Recipe recipe, HttpPostedFileBase ImageFile, string IngredientsList, string Instructions, string StepsList)
        {
            try
            {
                db.Configuration.ValidateOnSaveEnabled = false;

                string rawSteps = !string.IsNullOrWhiteSpace(Instructions) ? Instructions : StepsList;

                if (string.IsNullOrWhiteSpace(recipe.Title) ||
                    string.IsNullOrWhiteSpace(IngredientsList) ||
                    string.IsNullOrWhiteSpace(rawSteps))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Tên món ăn, Nguyên liệu và Các bước thực hiện!";
                    return RedirectToAction("MyRecipes");
                }

                recipe.AuthorId = CurrentUserId;
                recipe.ApprovedBy = null;
                recipe.ApprovedAt = null;

                recipe.Title = recipe.Title.Trim();
                recipe.CreatedAt = DateTime.Now;
                recipe.Status = "Chờ duyệt";

                if (recipe.CookTime <= 0) recipe.CookTime = 15;
                recipe.Description = !string.IsNullOrWhiteSpace(recipe.Description)
                    ? recipe.Description.Trim()
                    : "Công thức nấu ăn thơm ngon từ Cooking Baby.";

                // Upload ảnh vào /Uploads/Recipes/ bằng Server.MapPath và Guid
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    recipe.ImageUrl = SaveUploadedImage(ImageFile);
                }
                else
                {
                    recipe.ImageUrl = "pancake.jpg";
                }

                db.Recipes.Add(recipe);
                db.SaveChanges();

                // Lưu danh sách quan hệ 1-N
                SaveIngredientsHelper(recipe.RecipeId, IngredientsList);
                SaveStepsHelper(recipe.RecipeId, rawSteps);

                TempData["SuccessMessage"] = "Đăng công thức mới thành công! Bài viết đang chờ Ban Quản Trị duyệt.";
                return RedirectToAction("MyRecipes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi CreateRecipe: " + ex.Message);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng bài: " + ex.Message;
                return RedirectToAction("MyRecipes");
            }
        }

        // 3. SỬA MÓN ĂN (GET: Hỗ trợ cả /Recipe/Edit và /Recipe/EditRecipe)
        [SessionAuthorize]
        public ActionResult Edit(int? id)
        {
            return EditRecipe(id);
        }

        [SessionAuthorize]
        public ActionResult EditRecipe(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("MyRecipes");
            }

            var recipe = db.Recipes.FirstOrDefault(r => r.RecipeId == id.Value);
            if (recipe == null) return HttpNotFound();

            bool isAdmin = Session["Role"] as string == "Admin";
            if (recipe.AuthorId != CurrentUserId && !isAdmin)
            {
                return new HttpStatusCodeResult(403, "Bạn không có quyền chỉnh sửa công thức này.");
            }

            ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.DisplayOrder), "CategoryId", "CategoryName", recipe.CategoryId);

            // Nối danh sách nguyên liệu từ bảng RecipeIngredients
            var ingredients = db.RecipeIngredients
                                .Where(ri => ri.RecipeId == recipe.RecipeId)
                                .OrderBy(ri => ri.SortOrder)
                                .Select(ri => ri.IngredientName)
                                .ToList();
            ViewBag.IngredientsString = ingredients.Any() ? string.Join(", ", ingredients) : "";

            // Nối danh sách các bước từ bảng RecipeSteps
            var steps = db.Database.SqlQuery<string>(
                "SELECT Instruction FROM dbo.RecipeSteps WHERE RecipeId = @p0 ORDER BY StepNumber ASC",
                recipe.RecipeId
            ).ToList();
            ViewBag.InstructionsString = steps.Any() ? string.Join("\r\n", steps) : "";

            return View(recipe);
        }

        // 3. SỬA MÓN ĂN (POST: Hỗ trợ cả /Recipe/Edit và /Recipe/EditRecipe)
        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Recipe recipe, HttpPostedFileBase ImageFile, string IngredientsList, string Instructions)
        {
            return EditRecipe(recipe, ImageFile, IngredientsList, Instructions);
        }

        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult EditRecipe(Recipe recipe, HttpPostedFileBase ImageFile, string IngredientsList, string Instructions)
        {
            try
            {
                db.Configuration.ValidateOnSaveEnabled = false;

                bool hasError = false;
                if (string.IsNullOrWhiteSpace(recipe.Title))
                {
                    ModelState.AddModelError("Title", "Tên món ăn không được để trống.");
                    hasError = true;
                }
                if (recipe.CookTime <= 0 || recipe.CookTime > 1440)
                {
                    ModelState.AddModelError("CookTime", "Thời gian nấu phải từ 1 đến 1440 phút.");
                    hasError = true;
                }
                if (string.IsNullOrWhiteSpace(recipe.Description))
                {
                    ModelState.AddModelError("Description", "Mô tả ngắn không được để trống.");
                    hasError = true;
                }
                if (string.IsNullOrWhiteSpace(IngredientsList))
                {
                    ViewBag.IngredientsError = "Vui lòng nhập ít nhất 1 nguyên liệu.";
                    hasError = true;
                }
                if (string.IsNullOrWhiteSpace(Instructions))
                {
                    ViewBag.InstructionsError = "Vui lòng nhập các bước thực hiện.";
                    hasError = true;
                }

                if (hasError)
                {
                    ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.DisplayOrder), "CategoryId", "CategoryName", recipe.CategoryId);
                    ViewBag.IngredientsString = IngredientsList ?? "";
                    ViewBag.InstructionsString = Instructions ?? "";
                    return View(recipe);
                }

                var existingRecipe = db.Recipes.Find(recipe.RecipeId);
                if (existingRecipe == null) return HttpNotFound();

                bool isAdmin = Session["Role"] as string == "Admin";
                if (existingRecipe.AuthorId != CurrentUserId && !isAdmin)
                    return new HttpStatusCodeResult(403);

                existingRecipe.Title = recipe.Title.Trim();
                existingRecipe.CategoryId = recipe.CategoryId;
                existingRecipe.CookTime = recipe.CookTime;
                existingRecipe.Description = recipe.Description.Trim();
                existingRecipe.UpdatedAt = DateTime.Now;

                // Cập nhật ảnh nếu chọn file mới
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string oldImage = existingRecipe.ImageUrl;
                    string newImagePath = SaveUploadedImage(ImageFile);
                    existingRecipe.ImageUrl = newImagePath;

                    // Dọn dẹp file ảnh cũ nếu trước đó là ảnh tự upload
                    if (!string.IsNullOrWhiteSpace(oldImage) && oldImage.StartsWith("/Uploads/Recipes/"))
                    {
                        try
                        {
                            string oldPhysical = Server.MapPath("~" + oldImage);
                            if (System.IO.File.Exists(oldPhysical)) System.IO.File.Delete(oldPhysical);
                        }
                        catch { }
                    }
                }

                db.SaveChanges();

                // Cập nhật quan hệ 1-N: RecipeIngredients & RecipeSteps
                SaveIngredientsHelper(recipe.RecipeId, IngredientsList);
                SaveStepsHelper(recipe.RecipeId, Instructions);

                TempData["SuccessMessage"] = "Cập nhật công thức thành công!";
                return RedirectToAction("MyRecipes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi EditRecipe: " + ex.Message);
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);
            }

            ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.DisplayOrder), "CategoryId", "CategoryName", recipe.CategoryId);
            ViewBag.IngredientsString = IngredientsList ?? "";
            ViewBag.InstructionsString = Instructions ?? "";
            return View(recipe);
        }

        // 4. XÓA MÓN ĂN (POST: Hỗ trợ cả Delete và DeleteMyRecipe)
        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                var recipe = db.Recipes.Find(id);
                if (recipe == null) return HttpNotFound();

                bool isAdmin = Session["Role"] as string == "Admin";
                if (recipe.AuthorId != CurrentUserId && !isAdmin)
                {
                    return new HttpStatusCodeResult(403, "Bạn không có quyền xóa công thức này.");
                }

                // Xóa file ảnh vật lý trên Server nếu là ảnh do user upload vào /Uploads/Recipes/
                if (!string.IsNullOrWhiteSpace(recipe.ImageUrl) && recipe.ImageUrl.StartsWith("/Uploads/Recipes/"))
                {
                    try
                    {
                        string physicalPath = Server.MapPath("~" + recipe.ImageUrl);
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi xóa file ảnh: " + ex.Message);
                    }
                }

                // Xóa các bảng liên quan 1-N
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.RecipeSteps WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.RecipeIngredients WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.SavedRecipes WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.Ratings WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.Comments WHERE RecipeId = @p0", id);

                db.Recipes.Remove(recipe);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đã xóa công thức thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa công thức: " + ex.Message;
            }

            return RedirectToAction("MyRecipes");
        }

        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMyRecipe(int id)
        {
            return Delete(id);
        }

        // 5. TRANG CÔNG THỨC ĐÃ LƯU (SAVED RECIPES) CHO USER ĐANG ĐĂNG NHẬP
        [SessionAuthorize]
        public ActionResult SavedRecipes()
        {
            ViewBag.ActiveTab = "SavedRecipes";

            try
            {
                int userId = CurrentUserId;

                // Truy vấn LINQ bảng SavedRecipes kết hợp Recipe, Category, User, Ratings
                var recipes = db.SavedRecipes
                    .Where(s => s.UserId == userId && s.Recipe.Status != "Đã ẩn")
                    .OrderByDescending(s => s.SavedAt)
                    .Select(s => new RecipeCardViewModel
                    {
                        RecipeId = s.Recipe.RecipeId,
                        Title = s.Recipe.Title,
                        Description = s.Recipe.Description,
                        CookTime = s.Recipe.CookTime,
                        ImageUrl = s.Recipe.ImageUrl,
                        CategoryId = s.Recipe.CategoryId,
                        CategoryName = s.Recipe.Category.CategoryName,
                        AuthorName = s.Recipe.User1 != null
                            ? s.Recipe.User1.DisplayName
                            : "Không xác định",
                        Status = s.Recipe.Status,
                        RatingCount = s.Recipe.Ratings.Count(),
                        AvgRating = s.Recipe.Ratings
                            .Select(r => (decimal?)r.Stars)
                            .Average()
                    })
                    .ToList();

                return View(recipes);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách công thức đã lưu: " + ex.Message;
                return View(new List<RecipeCardViewModel>());
            }
        }

        // HÀM TIỆN ÍCH LƯU FILE ẢNH VÀO /Uploads/Recipes/
        private string SaveUploadedImage(HttpPostedFileBase imageFile)
        {
            if (imageFile == null || imageFile.ContentLength == 0) return null;

            string ext = Path.GetExtension(imageFile.FileName).ToLower();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowedExts.Contains(ext))
            {
                throw new InvalidOperationException("Định dạng ảnh không hợp lệ! Chỉ chấp nhận file .jpg, .jpeg, .png, .webp.");
            }

            if (imageFile.ContentLength > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Kích thước file ảnh không được vượt quá 5MB.");
            }

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string folder = Server.MapPath("~/Uploads/Recipes");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string physicalPath = Path.Combine(folder, uniqueFileName);
            imageFile.SaveAs(physicalPath);

            return "/Uploads/Recipes/" + uniqueFileName;
        }

        // HÀM TIỆN ÍCH 1-N: Tách và cập nhật danh sách nguyên liệu
        private void SaveIngredientsHelper(int recipeId, string ingredientsList)
        {
            db.Database.ExecuteSqlCommand("DELETE FROM dbo.RecipeIngredients WHERE RecipeId = @p0", recipeId);

            if (!string.IsNullOrWhiteSpace(ingredientsList))
            {
                var items = ingredientsList
                    .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                byte sort = 1;
                foreach (var item in items)
                {
                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder) VALUES (@p0, @p1, @p2)",
                        recipeId, item, sort++
                    );
                }
            }
        }

        // HÀM TIỆN ÍCH 1-N: Tách và cập nhật danh sách các bước nấu
        private void SaveStepsHelper(int recipeId, string instructionsText)
        {
            db.Database.ExecuteSqlCommand("DELETE FROM dbo.RecipeSteps WHERE RecipeId = @p0", recipeId);

            if (!string.IsNullOrWhiteSpace(instructionsText))
            {
                var stepLines = instructionsText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                byte stepNo = 1;
                foreach (var line in stepLines)
                {
                    string cleanInstruction = System.Text.RegularExpressions.Regex.Replace(
                        line, @"^(Bước|Step)\s*\d+\s*[:.-]\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    if (string.IsNullOrWhiteSpace(cleanInstruction)) cleanInstruction = line;

                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.RecipeSteps (RecipeId, StepNumber, Instruction) VALUES (@p0, @p1, @p2)",
                        recipeId, stepNo++, cleanInstruction
                    );
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [ChildActionOnly]
        public ActionResult _NavbarTabs(string activeTab)
        {
            int myRecipeCount = 0;
            int savedCount = 0;

            try
            {
                if (Session["UserID"] is int)
                {
                    int chefId = CurrentUserId;
                    myRecipeCount = db.Recipes.Count(r => r.AuthorId == chefId && r.Status != "Đã ẩn");
                    savedCount = db.SavedRecipes.Count(s => s.UserId == chefId);
                }
            }
            catch
            {
                myRecipeCount = 0;
                savedCount = 0;
            }

            ViewBag.MyRecipeCount = myRecipeCount;
            ViewBag.SavedCount = savedCount;
            ViewBag.ActiveTab = activeTab;

            return PartialView("_NavbarTabs");
        }
    }
}
