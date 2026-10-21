using CookingBaby.Models;
using Microsoft.EntityFrameworkCore;

namespace CookingBaby.Data;

public class CookingBabyDbContext : DbContext
{
    public CookingBabyDbContext(DbContextOptions<CookingBabyDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<SavedRecipe> SavedRecipes => Set<SavedRecipe>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ---------- Users ----------
        mb.Entity<User>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role",
                $"[Role] IN (N'{UserRoles.User}', N'{UserRoles.Admin}')"));

            e.Property(x => x.Username).IsRequired().HasMaxLength(50);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(150);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);
            e.Property(x => x.Role).IsRequired().HasMaxLength(20).HasDefaultValue(UserRoles.User);
            // IsActive: giá trị mặc định true đặt ở C# (tránh bẫy HasDefaultValue(true) với kiểu bool)
            e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");

            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ---------- Categories (4 Đầu Bếp Nhí) ----------
        mb.Entity<Category>(e =>
        {
            e.Property(x => x.CategoryName).IsRequired().HasMaxLength(100);
            e.Property(x => x.MascotName).IsRequired().HasMaxLength(100);
            e.Property(x => x.ThemeColor).HasColumnType("char(7)");
            e.HasIndex(x => x.CategoryName).IsUnique();
        });

        // ---------- Recipes ----------
        mb.Entity<Recipe>(e =>
        {
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Recipes_CookTime", "[CookTime] > 0");
                t.HasCheckConstraint("CK_Recipes_Status",
                    $"[Status] IN (N'{RecipeStatus.Pending}', N'{RecipeStatus.Approved}', N'{RecipeStatus.Hidden}')");
            });

            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).IsRequired().HasMaxLength(500);
            e.Property(x => x.CookTime).HasDefaultValue((short)20);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue(RecipeStatus.Pending);
            e.Property(x => x.ApprovedAt).HasColumnType("datetime2(0)");
            e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

            // Không cho xóa Category / User nếu còn công thức (NO ACTION)
            e.HasOne(x => x.Category).WithMany(c => c.Recipes)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Author).WithMany(u => u.AuthoredRecipes)
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Approver).WithMany(u => u.ApprovedRecipes)
                .HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.Status, x.CategoryId })
                .HasDatabaseName("IX_Recipes_Status_Category")
                .IncludeProperties(x => new { x.AuthorId, x.CookTime });
            e.HasIndex(x => new { x.AuthorId, x.Status })
                .HasDatabaseName("IX_Recipes_Author_Status");        // tab "Quản lý cá nhân"
        });

        // ---------- RecipeIngredients ----------
        mb.Entity<RecipeIngredient>(e =>
        {
            e.HasKey(x => x.IngredientId);   
            e.Property(x => x.IngredientName).IsRequired().HasMaxLength(100);

            e.HasOne(x => x.Recipe).WithMany(r => r.Ingredients)
                .HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.RecipeId, x.IngredientName }).IsUnique().HasDatabaseName("UQ_RecipeIngredients");
            e.HasIndex(x => x.IngredientName)
                .HasDatabaseName("IX_RecipeIngredients_Name")         // lọc nhanh theo nguyên liệu
                .IncludeProperties(x => x.RecipeId);
        });

        // ---------- RecipeSteps ----------
        mb.Entity<RecipeStep>(e =>
        {
            e.HasKey(x => x.StepId); 
            e.ToTable(t => t.HasCheckConstraint("CK_RecipeSteps_Number", "[StepNumber] > 0"));

            e.Property(x => x.Instruction).IsRequired().HasMaxLength(1000);

            e.HasOne(x => x.Recipe).WithMany(r => r.Steps)
                .HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.RecipeId, x.StepNumber }).IsUnique().HasDatabaseName("UQ_RecipeSteps");
        });

        // ---------- Ratings ----------
        mb.Entity<Rating>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_Ratings_Stars", "[Stars] BETWEEN 1 AND 5"));

            e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");

            e.HasOne(x => x.Recipe).WithMany(r => r.Ratings)
                .HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Ratings)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.RecipeId, x.UserId }).IsUnique().HasDatabaseName("UQ_Ratings_Recipe_User");
        });

        // ---------- Comments ----------
        mb.Entity<Comment>(e =>
        {
            e.Property(x => x.Content).IsRequired().HasMaxLength(1000);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");

            e.HasOne(x => x.Recipe).WithMany(r => r.Comments)
                .HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Comments)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.RecipeId, x.CreatedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_Comments_Recipe");
        });

        // ---------- SavedRecipes (khóa chính ghép) ----------
        mb.Entity<SavedRecipe>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RecipeId });
            e.Property(x => x.SavedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");

            e.HasOne(x => x.User).WithMany(u => u.SavedRecipes)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Recipe).WithMany(r => r.SavedRecipes)
                .HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Dữ liệu mẫu ----------
        mb.Seed();
    }
}
