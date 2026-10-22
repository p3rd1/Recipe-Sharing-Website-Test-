using CookingBaby.Data;
using CookingBaby.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CookingBabyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CookingBabyDb")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CookingBabyDbContext>().Database.Migrate();
}

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/demo/index.html"));

app.MapGet("/api/recipes", async (CookingBabyDbContext db) =>
    await db.Recipes
        .AsNoTracking()
        .Where(r => r.Status == RecipeStatus.Approved)
        .Select(r => new
        {
            r.RecipeId,
            r.Title,
            r.Description,
            r.CookTime,
            r.ImageUrl,
            Category = r.Category.CategoryName,
            Author = r.Author.DisplayName,
            AvgRating = r.Ratings.Average(x => (double?)x.Stars),
            RatingCount = r.Ratings.Count(),
            Ingredients = r.Ingredients.OrderBy(i => i.SortOrder).Select(i => i.IngredientName)
        })
        .OrderByDescending(x => x.AvgRating)
        .ThenByDescending(x => x.RatingCount)
        .ToListAsync());

app.Run();