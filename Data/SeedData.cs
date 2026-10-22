using CookingBaby.Models;
using Microsoft.EntityFrameworkCore;

namespace CookingBaby.Data;

public static class SeedData
{
    // ID người dùng
    private const int Owner = 1, Binh = 2, Nhu = 3, Admin = 4;
    // ID danh mục 
    private const int CatSang = 1, CatChinh = 2, CatBanh = 3, CatHealth = 4;
    // ID công thức
    private const int Soup = 1, Salad = 2, TrungChan = 3, Pancake = 4, TomXao = 5;

    // Chuỗi giữ chỗ
    private const string DemoHash = "DEMO_HASH_CHANGE_ME";

    public static void Seed(this ModelBuilder mb)
    {
        var t = new DateTime(2026, 10, 18, 9, 0, 0);

        // ---------- Users ----------
        mb.Entity<User>().HasData(
            new User { UserId = Owner, Username = "sieudaubepnhi", DisplayName = "Siêu đầu bếp nhí",
                       Email = "owner@cookingbaby.demo", PasswordHash = DemoHash, Role = UserRoles.User, IsActive = true, CreatedAt = t },
            new User { UserId = Binh, Username = "thanhbinh", DisplayName = "Thanh Bình",
                       Email = "binh@cookingbaby.demo", PasswordHash = DemoHash, Role = UserRoles.User, IsActive = true, CreatedAt = t },
            new User { UserId = Nhu, Username = "quynhnhu", DisplayName = "Quỳnh Như",
                       Email = "nhu@cookingbaby.demo", PasswordHash = DemoHash, Role = UserRoles.User, IsActive = true, CreatedAt = t },
            new User { UserId = Admin, Username = "admin", DisplayName = "Quản trị viên",
                       Email = "admin@cookingbaby.demo", PasswordHash = DemoHash, Role = UserRoles.Admin, IsActive = true, CreatedAt = t }
        );

        // ---------- Categories ---------
        mb.Entity<Category>().HasData(
            new Category { CategoryId = CatSang,   CategoryName = "Ăn Sáng & Món Nhanh",    ThemeColor = "#F97316", DisplayOrder = 1 },
            new Category { CategoryId = CatChinh,  CategoryName = "Món Chính & Súp Hầm",    ThemeColor = "#F59E0B", DisplayOrder = 2 },
            new Category { CategoryId = CatBanh,   CategoryName = "Bánh & Tráng Miệng",     ThemeColor = "#EC4899", DisplayOrder = 3 },
            new Category { CategoryId = CatHealth, CategoryName = "Healthy & Rau Củ",       ThemeColor = "#22C55E", DisplayOrder = 4 }
        );

        // ---------- Recipes ----------
        mb.Entity<Recipe>().HasData(
            new Recipe
            {
                RecipeId = Soup, Title = "Súp Bí Đỏ Kem Tươi Hầm Kiểu Âu",
                Description = "Món súp sánh mịn, vàng óng giống hệt nồi súp của 4 em bé trong logo Cooking Baby. Vị ngọt tự nhiên từ bí đỏ quện cùng sữa tươi và bơ lạt béo thơm.",
                CookTime = 25, CategoryId = CatChinh, AuthorId = Owner,
                Status = RecipeStatus.Approved, ApprovedBy = Admin, ApprovedAt = t, CreatedAt = t
            },
            new Recipe
            {
                RecipeId = Salad, Title = "Salad Ức Gà Áp Chảo & Cà Chua Bi Sốt Mè",
                Description = "Ức gà giữ được độ mềm mọng nước nhờ bí quyết áp chảo đậy nắp, ăn kèm rau xanh và cà chua bi tươi mát.",
                CookTime = 20, CategoryId = CatHealth, AuthorId = Binh,
                Status = RecipeStatus.Approved, ApprovedBy = Admin, ApprovedAt = t, CreatedAt = t
            },
            new Recipe
            {
                RecipeId = TrungChan, Title = "Trứng Chần Nấm Đùi Gà Sốt Tương Nhật",
                Description = "Món ăn sáng siêu tốc giàu đạm với lòng đào béo ngậy quyện cùng nấm đùi gà áp chảo giòn ngọt.",
                CookTime = 12, CategoryId = CatSang, AuthorId = Nhu,
                Status = RecipeStatus.Approved, ApprovedBy = Admin, ApprovedAt = t, CreatedAt = t
            },
            new Recipe
            {
                RecipeId = Pancake, Title = "Bánh Kếp (Pancake) Mật Ong Bơ Tươi",
                Description = "Những chiếc bánh pancake tròn xoe, bông xốp thơm lừng mùi bơ sữa, chỉ mất 15 phút chuẩn bị cho bữa sáng cuối tuần.",
                CookTime = 15, CategoryId = CatBanh, AuthorId = Owner,
                Status = RecipeStatus.Approved, ApprovedBy = Admin, ApprovedAt = t, CreatedAt = t
            },
            new Recipe   
            {
                RecipeId = TomXao, Title = "Tôm Tươi Xào Cà Chua Trứng Mềm Mượt",
                Description = "Sự kết hợp kinh điển giữa tôm tươi giòn ngọt và sốt trứng cà chua sánh mịn, cực kỳ bắt cơm.",
                CookTime = 18, CategoryId = CatChinh, AuthorId = Owner,
                Status = RecipeStatus.Pending, CreatedAt = t
            }
        );

        // ---------- RecipeIngredients ----------
        var ingredients = new List<RecipeIngredient>();
        var ingId = 1;
        void AddIngredients(int recipeId, params string[] names)
        {
            byte order = 1;
            foreach (var name in names)
                ingredients.Add(new RecipeIngredient
                {
                    IngredientId = ingId++, RecipeId = recipeId, IngredientName = name, SortOrder = order++
                });
        }

        AddIngredients(Soup,      "Bí đỏ", "Sữa tươi", "Bơ lạt", "Tỏi băm");
        AddIngredients(Salad,     "Ức gà", "Cà chua", "Trứng gà", "Xà lách");
        AddIngredients(TrungChan, "Trứng gà", "Nấm", "Bơ lạt");
        AddIngredients(Pancake,   "Trứng gà", "Sữa tươi", "Bơ lạt", "Bột mì");
        AddIngredients(TomXao,    "Tôm tươi", "Trứng gà", "Cà chua");
        mb.Entity<RecipeIngredient>().HasData(ingredients);

        // ---------- RecipeSteps ----------
        var steps = new List<RecipeStep>();
        var stepId = 1;
        void AddSteps(int recipeId, params string[] instructions)
        {
            byte number = 1;
            foreach (var text in instructions)
                steps.Add(new RecipeStep
                {
                    StepId = stepId++, RecipeId = recipeId, StepNumber = number++, Instruction = text
                });
        }

        AddSteps(Soup,
            "Hấp hoặc luộc bí đỏ cắt miếng đến khi thật mềm.",
            "Phi thơm tỏi băm với bơ lạt, cho bí đỏ vào xào sơ.",
            "Xay nhuyễn, thêm sữa tươi, đun nhỏ lửa 5 phút rồi nêm nếm vừa ăn.");
        AddSteps(Salad,
            "Ướp ức gà với chút muối hồng, tiêu đen và dầu olive trong 10 phút.",
            "Áp chảo lửa vừa mỗi mặt 4 phút, tắt bếp đậy nắp ủ thêm 3 phút rồi thái lát.",
            "Trộn rau xà lách, cà chua bi bổ đôi, trứng luộc cùng sốt mè rang và xếp gà lên trên.");
        AddSteps(TrungChan,
            "Chần trứng trong nước sôi nhẹ khoảng 3 phút cho lòng đào.",
            "Áp chảo nấm đùi gà với bơ lạt đến khi vàng giòn.",
            "Xếp nấm ra đĩa, đặt trứng chần lên trên và rưới sốt tương Nhật.");
        AddSteps(Pancake,
            "Trộn bột mì, trứng gà và sữa tươi thành hỗn hợp sánh mịn.",
            "Đun chảy bơ lạt trong chảo, múc bột và rán lửa nhỏ mỗi mặt 2 phút.",
            "Xếp bánh thành chồng, rưới mật ong và thêm bơ tươi lên trên.");
        AddSteps(TomXao,
            "Sơ chế tôm, ướp chút muối tiêu.",
            "Xào tôm đến khi chín đỏ rồi múc ra.",
            "Xào cà chua đến khi nhuyễn, đổ trứng đã đánh vào, trộn tôm lại và nêm nếm.");
        mb.Entity<RecipeStep>().HasData(steps);

        // ---------- Ratings: Súp 5.0 (2), Salad 5.0 (1), Trứng chần 4.0 (1), Pancake "Mới (0)" ----------
        mb.Entity<Rating>().HasData(
            new Rating { RatingId = 1, RecipeId = Soup,      UserId = Binh,  Stars = 5, CreatedAt = t },
            new Rating { RatingId = 2, RecipeId = Soup,      UserId = Nhu,   Stars = 5, CreatedAt = t },
            new Rating { RatingId = 3, RecipeId = Salad,     UserId = Owner, Stars = 5, CreatedAt = t },
            new Rating { RatingId = 4, RecipeId = TrungChan, UserId = Binh,  Stars = 4, CreatedAt = t }
        );

        // ---------- Comments ----------
        mb.Entity<Comment>().HasData(
            new Comment { CommentId = 1, RecipeId = Salad, UserId = Owner, CreatedAt = t,
                          Content = "Cách ủ ức gà này giúp thịt mềm thật sự, không bị bã chút nào." }
        );

        // ---------- SavedRecipes ----------
        mb.Entity<SavedRecipe>().HasData(
            new SavedRecipe { UserId = Owner, RecipeId = Soup,  SavedAt = t },
            new SavedRecipe { UserId = Owner, RecipeId = Salad, SavedAt = t }
        );
    }
}
