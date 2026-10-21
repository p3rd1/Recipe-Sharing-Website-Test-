namespace CookingBaby.Models;

public static class UserRoles
{
    public const string User  = "User";
    public const string Admin = "Admin";
}

public static class RecipeStatus
{
    public const string Pending  = "Chờ duyệt";
    public const string Approved = "Đã duyệt";
    public const string Hidden   = "Đã ẩn";
}

// ---- 1. Users ----
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;   // "Siêu đầu bếp nhí", "Thanh Bình"...
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<Recipe> AuthoredRecipes { get; set; } = new List<Recipe>();   // Recipes.AuthorId
    public ICollection<Recipe> ApprovedRecipes { get; set; } = new List<Recipe>();   // Recipes.ApprovedBy
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<SavedRecipe> SavedRecipes { get; set; } = new List<SavedRecipe>();
}

// ---- 2. Categories ----
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;  // "Món Chính & Súp Hầm"
    public string MascotName { get; set; } = string.Empty;    // "Bé Trai Tạp Dề Vàng"
    public string? ThemeColor { get; set; }                   // "#F59E0B"
    public byte DisplayOrder { get; set; }

    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}

// ---- 3. Recipes ----
public class Recipe
{
    public int RecipeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public short CookTime { get; set; } = 20;                 
    public string? ImageUrl { get; set; }                         
    public int CategoryId { get; set; }
    public int AuthorId { get; set; }
    public string Status { get; set; } = RecipeStatus.Pending;
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public User Author { get; set; } = null!;
    public User? Approver { get; set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<SavedRecipe> SavedRecipes { get; set; } = new List<SavedRecipe>();
}

// ---- 4. RecipeIngredients ----
public class RecipeIngredient
{
    public int IngredientId { get; set; }
    public int RecipeId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public byte SortOrder { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

// ---- 5. RecipeSteps ----
public class RecipeStep
{
    public int StepId { get; set; }
    public int RecipeId { get; set; }
    public byte StepNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;

    public Recipe Recipe { get; set; } = null!;
}

// ---- 6. Ratings ----
public class Rating
{
    public int RatingId { get; set; }
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public byte Stars { get; set; }                           // 1..5
    public DateTime CreatedAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
}

// ---- 7. Comments ----
public class Comment
{
    public int CommentId { get; set; }
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
}

// ---- 8. SavedRecipes ----
public class SavedRecipe
{
    public int UserId { get; set; }
    public int RecipeId { get; set; }
    public DateTime SavedAt { get; set; }

    public User User { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
