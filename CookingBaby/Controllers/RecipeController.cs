using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CookingBaby.Models;

namespace CookingBaby.Controllers
{
    public class RecipeController : Controller
    {
        private CookingBabyDBEntities db = new CookingBabyDBEntities();

        // 1. TAB QUẢN LÝ CÁ NHÂN (MÓN CỦA TÔI)
        public ActionResult MyRecipes()
        {
            var chef = db.Users.FirstOrDefault(u => u.Username == "sieudaubepnhi");
            int chefId = chef != null ? chef.UserId : 1;

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

            if (recipe == null)
            {
                return HttpNotFound();
            }

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

                // Lấy ID của Siêu đầu bếp nhí đang đăng nhập hiện tại (mặc định ID = 1 hoặc theo username)
                var chef = db.Users.FirstOrDefault(u => u.Username == "sieudaubepnhi");
                int currentUserId = chef != null ? chef.UserId : 1;

                // Kiểm tra xem user hiện tại đã lưu món này chưa
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
                    currentUserId = currentUserId, // Gửi ID user hiện tại sang để so sánh quyền
                    isSaved = isSaved,             // Trạng thái thực tế đã lưu hay chưa
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

        // 2. THÊM MÓN MỚI TỪ MODAL (LƯU ẢNH + TÁCH DÒNG INGREDIENTS & STEPS + KIỂM TRA ĐẦU VÀO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRecipe(Recipe recipe, HttpPostedFileBase ImageFile, string IngredientsList, string Instructions, string StepsList)
        {
            try
            {
                db.Configuration.ValidateOnSaveEnabled = false;

                string rawSteps = !string.IsNullOrWhiteSpace(Instructions) ? Instructions : StepsList;

                // Kiểm tra dữ liệu đầu vào phía Server
                if (string.IsNullOrWhiteSpace(recipe.Title) ||
                    string.IsNullOrWhiteSpace(IngredientsList) ||
                    string.IsNullOrWhiteSpace(rawSteps))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Tên món ăn, Nguyên liệu và Các bước thực hiện!";
                    return RedirectToAction("MyRecipes");
                }

                var user = db.Users.FirstOrDefault(u => u.Username == "sieudaubepnhi") ?? db.Users.FirstOrDefault();
                recipe.AuthorId = user != null ? user.UserId : 1;

                recipe.Title = recipe.Title.Trim();
                recipe.CreatedAt = DateTime.Now;
                recipe.Status = "Chờ duyệt";

                if (recipe.CookTime <= 0) recipe.CookTime = 15;
                recipe.Description = !string.IsNullOrWhiteSpace(recipe.Description)
                    ? recipe.Description.Trim()
                    : "Công thức nấu ăn thơm ngon từ Cooking Baby.";

                // Xử lý kiểm tra và lưu file ảnh
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string ext = Path.GetExtension(ImageFile.FileName).ToLower();
                    var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    if (!allowedExts.Contains(ext))
                    {
                        TempData["ErrorMessage"] = "Định dạng ảnh không hợp lệ! Chỉ chấp nhận file .jpg, .png, .webp.";
                        return RedirectToAction("MyRecipes");
                    }

                    string fileName = Path.GetFileName(ImageFile.FileName);
                    string folder = Server.MapPath("~/Content/images");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    ImageFile.SaveAs(Path.Combine(folder, fileName));
                    recipe.ImageUrl = fileName;
                }
                else
                {
                    recipe.ImageUrl = "pancake.jpg";
                }

                db.Recipes.Add(recipe);
                db.SaveChanges();

                // Tách dòng và lưu Nguyên liệu (RecipeIngredients) - Lọc trùng để không lỗi UQ_RecipeIngredients
                SaveIngredientsHelper(recipe.RecipeId, IngredientsList);

                // Tách dòng và lưu Các bước nấu (RecipeSteps)
                SaveStepsHelper(recipe.RecipeId, rawSteps);

                TempData["SuccessMessage"] = "Đăng công thức mới thành công! Bài viết đang chờ duyệt.";
                return RedirectToAction("MyRecipes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi CreateRecipe: " + ex.Message);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng bài: " + ex.Message;
                return RedirectToAction("MyRecipes");
            }
        }

        // 3. SỬA MÓN ĂN (GET)
        public ActionResult EditRecipe(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("MyRecipes");
            }

            var recipe = db.Recipes.FirstOrDefault(r => r.RecipeId == id.Value);
            if (recipe == null)
            {
                return HttpNotFound();
            }

            ViewBag.CategoryId = new SelectList(db.Categories.OrderBy(c => c.DisplayOrder), "CategoryId", "CategoryName", recipe.CategoryId);
            // 1. Nối danh sách nguyên liệu từ bảng RecipeIngredients
            var ingredients = db.RecipeIngredients
                                .Where(ri => ri.RecipeId == recipe.RecipeId)
                                .OrderBy(ri => ri.SortOrder)
                                .Select(ri => ri.IngredientName)
                                .ToList();
            ViewBag.IngredientsString = ingredients.Any() ? string.Join(", ", ingredients) : "";

            // 2. Nối danh sách các bước từ bảng RecipeSteps (mỗi bước 1 dòng)
            var steps = db.Database.SqlQuery<string>(
                "SELECT Instruction FROM dbo.RecipeSteps WHERE RecipeId = @p0 ORDER BY StepNumber ASC",
                recipe.RecipeId
            ).ToList();
            ViewBag.InstructionsString = steps.Any() ? string.Join("\r\n", steps) : "";

            return View(recipe);
        }

        // 3. SỬA MÓN ĂN (POST) + KIỂM TRA DỮ LIỆU ĐẦU VÀO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRecipe(Recipe recipe, HttpPostedFileBase ImageFile, string IngredientsList, string Instructions)
        {
            try
            {
                db.Configuration.ValidateOnSaveEnabled = false;

                // Kiểm tra dữ liệu đầu vào chặt chẽ
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
                if (existingRecipe != null)
                {
                    existingRecipe.Title = recipe.Title.Trim();
                    existingRecipe.CategoryId = recipe.CategoryId;
                    existingRecipe.CookTime = recipe.CookTime;
                    existingRecipe.Description = recipe.Description.Trim();
                    existingRecipe.UpdatedAt = DateTime.Now;

                    // Cập nhật ảnh nếu có chọn file mới
                    if (ImageFile != null && ImageFile.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(ImageFile.FileName);
                        string folder = Server.MapPath("~/Content/images");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        ImageFile.SaveAs(Path.Combine(folder, fileName));
                        existingRecipe.ImageUrl = fileName;
                    }

                    db.SaveChanges();

                    // Cập nhật lại bảng RecipeIngredients & RecipeSteps
                    SaveIngredientsHelper(recipe.RecipeId, IngredientsList);
                    SaveStepsHelper(recipe.RecipeId, Instructions);

                    TempData["SuccessMessage"] = "Cập nhật công thức thành công!";
                    return RedirectToAction("MyRecipes");
                }
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

        // HÀM PHỤ TRỢ 1: Tách và lưu danh sách nguyên liệu (Chống trùng lặp tên nguyên liệu trong cùng 1 món)
        private void SaveIngredientsHelper(int recipeId, string ingredientsList)
        {
            db.Database.ExecuteSqlCommand("DELETE FROM dbo.RecipeIngredients WHERE RecipeId = @p0", recipeId);

            if (!string.IsNullOrWhiteSpace(ingredientsList))
            {
                var items = ingredientsList
                    .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase) // Loại bỏ nguyên liệu nhập trùng
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

        // HÀM PHỤ TRỢ 2: Tách dòng và lưu danh sách các bước thực hiện vào dbo.RecipeSteps
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
                    // Tự động cắt bỏ chữ "Bước 1:", "Bước 2:" ở đầu dòng nếu người dùng lỡ gõ vào
                    string cleanInstruction = System.Text.RegularExpressions.Regex.Replace(line, @"^(Bước|Step)\s*\d+\s*[:.-]\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    if (string.IsNullOrWhiteSpace(cleanInstruction)) cleanInstruction = line;

                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.RecipeSteps (RecipeId, StepNumber, Instruction) VALUES (@p0, @p1, @p2)",
                        recipeId, stepNo++, cleanInstruction
                    );
                }
            }
        }

        // 4. XÓA MÓN ĂN TRONG QUẢN LÝ CÁ NHÂN
        public ActionResult DeleteMyRecipe(int id)
        {
            var recipe = db.Recipes.Find(id);
            if (recipe != null)
            {
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.RecipeSteps WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.RecipeIngredients WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.SavedRecipes WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.Ratings WHERE RecipeId = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.Comments WHERE RecipeId = @p0", id);

                db.Recipes.Remove(recipe);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa món ăn thành công!";
            }
            return RedirectToAction("MyRecipes");
        }

        // 5. TRANG CÔNG THỨC ĐÃ LƯU
        public ActionResult SavedRecipes()
        {
            ViewBag.ActiveTab = "SavedRecipes";

            int userId = 1;

            // Lấy danh sách công thức đã lưu bằng LINQ
            var recipes = db.SavedRecipes
                .Where(s => s.UserId == userId)
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
                var chef = db.Users.FirstOrDefault(u => u.Username == "sieudaubepnhi");
                int chefId = chef != null ? chef.UserId : 1;

                myRecipeCount = db.Recipes.Count(r => r.AuthorId == chefId && r.Status != "Đã ẩn");
                savedCount = db.SavedRecipes.Count(s => s.UserId == chefId);
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