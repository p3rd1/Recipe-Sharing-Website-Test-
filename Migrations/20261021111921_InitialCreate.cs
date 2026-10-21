using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CookingBaby.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MascotName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThemeColor = table.Column<string>(type: "char(7)", nullable: true),
                    DisplayOrder = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "User"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN (N'User', N'Admin')");
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    RecipeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CookTime = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)20),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Chờ duyệt"),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.RecipeId);
                    table.CheckConstraint("CK_Recipes_CookTime", "[CookTime] > 0");
                    table.CheckConstraint("CK_Recipes_Status", "[Status] IN (N'Chờ duyệt', N'Đã duyệt', N'Đã ẩn')");
                    table.ForeignKey(
                        name: "FK_Recipes_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipes_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_Comments_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    RatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Stars = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.RatingId);
                    table.CheckConstraint("CK_Ratings_Stars", "[Stars] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Ratings_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ratings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    IngredientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    IngredientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.IngredientId);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeSteps",
                columns: table => new
                {
                    StepId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    StepNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    Instruction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeSteps", x => x.StepId);
                    table.CheckConstraint("CK_RecipeSteps_Number", "[StepNumber] > 0");
                    table.ForeignKey(
                        name: "FK_RecipeSteps_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedRecipes",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedRecipes", x => new { x.UserId, x.RecipeId });
                    table.ForeignKey(
                        name: "FK_SavedRecipes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedRecipes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "DisplayOrder", "MascotName", "ThemeColor" },
                values: new object[,]
                {
                    { 1, "Ăn Sáng & Món Nhanh", (byte)1, "Bé Gái Khăn Cam", "#F97316" },
                    { 2, "Món Chính & Súp Hầm", (byte)2, "Bé Trai Tạp Dề Vàng", "#F59E0B" },
                    { 3, "Bánh & Tráng Miệng", (byte)3, "Bé Gái Khăn Hồng", "#EC4899" },
                    { 4, "Healthy & Rau Củ", (byte)4, "Bé Trai Khăn Xanh", "#22C55E" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "DisplayName", "Email", "IsActive", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Siêu đầu bếp nhí", "owner@cookingbaby.demo", true, "DEMO_HASH_CHANGE_ME", "User", "sieudaubepnhi" },
                    { 2, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Thanh Bình", "binh@cookingbaby.demo", true, "DEMO_HASH_CHANGE_ME", "User", "thanhbinh" },
                    { 3, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Quỳnh Như", "nhu@cookingbaby.demo", true, "DEMO_HASH_CHANGE_ME", "User", "quynhnhu" },
                    { 4, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị viên", "admin@cookingbaby.demo", true, "DEMO_HASH_CHANGE_ME", "Admin", "admin" }
                });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "RecipeId", "ApprovedAt", "ApprovedBy", "AuthorId", "CategoryId", "CookTime", "CreatedAt", "Description", "ImageUrl", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 4, 1, 2, (short)25, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Món súp sánh mịn, vàng óng giống hệt nồi súp của 4 em bé trong logo Cooking Baby. Vị ngọt tự nhiên từ bí đỏ quện cùng sữa tươi và bơ lạt béo thơm.", null, "Đã duyệt", "Súp Bí Đỏ Kem Tươi Hầm Kiểu Âu", null },
                    { 2, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 4, 2, 4, (short)20, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Ức gà giữ được độ mềm mọng nước nhờ bí quyết áp chảo đậy nắp, ăn kèm rau xanh và cà chua bi tươi mát.", null, "Đã duyệt", "Salad Ức Gà Áp Chảo & Cà Chua Bi Sốt Mè", null },
                    { 3, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 4, 3, 1, (short)12, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Món ăn sáng siêu tốc giàu đạm với lòng đào béo ngậy quyện cùng nấm đùi gà áp chảo giòn ngọt.", null, "Đã duyệt", "Trứng Chần Nấm Đùi Gà Sốt Tương Nhật", null },
                    { 4, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 4, 1, 3, (short)15, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Những chiếc bánh pancake tròn xoe, bông xốp thơm lừng mùi bơ sữa, chỉ mất 15 phút chuẩn bị cho bữa sáng cuối tuần.", null, "Đã duyệt", "Bánh Kếp (Pancake) Mật Ong Bơ Tươi", null },
                    { 5, null, null, 1, 2, (short)18, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), "Sự kết hợp kinh điển giữa tôm tươi giòn ngọt và sốt trứng cà chua sánh mịn, cực kỳ bắt cơm.", null, "Chờ duyệt", "Tôm Tươi Xào Cà Chua Trứng Mềm Mượt", null }
                });

            migrationBuilder.InsertData(
                table: "Comments",
                columns: new[] { "CommentId", "Content", "CreatedAt", "RecipeId", "UserId" },
                values: new object[] { 1, "Cách ủ ức gà này giúp thịt mềm thật sự, không bị bã chút nào.", new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 2, 1 });

            migrationBuilder.InsertData(
                table: "Ratings",
                columns: new[] { "RatingId", "CreatedAt", "RecipeId", "Stars", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 1, (byte)5, 2 },
                    { 2, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 1, (byte)5, 3 },
                    { 3, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 2, (byte)5, 1 },
                    { 4, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified), 3, (byte)4, 2 }
                });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "IngredientId", "IngredientName", "RecipeId", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Bí đỏ", 1, (byte)1 },
                    { 2, "Sữa tươi", 1, (byte)2 },
                    { 3, "Bơ lạt", 1, (byte)3 },
                    { 4, "Tỏi băm", 1, (byte)4 },
                    { 5, "Ức gà", 2, (byte)1 },
                    { 6, "Cà chua", 2, (byte)2 },
                    { 7, "Trứng gà", 2, (byte)3 },
                    { 8, "Xà lách", 2, (byte)4 },
                    { 9, "Trứng gà", 3, (byte)1 },
                    { 10, "Nấm", 3, (byte)2 },
                    { 11, "Bơ lạt", 3, (byte)3 },
                    { 12, "Trứng gà", 4, (byte)1 },
                    { 13, "Sữa tươi", 4, (byte)2 },
                    { 14, "Bơ lạt", 4, (byte)3 },
                    { 15, "Bột mì", 4, (byte)4 },
                    { 16, "Tôm tươi", 5, (byte)1 },
                    { 17, "Trứng gà", 5, (byte)2 },
                    { 18, "Cà chua", 5, (byte)3 }
                });

            migrationBuilder.InsertData(
                table: "RecipeSteps",
                columns: new[] { "StepId", "Instruction", "RecipeId", "StepNumber" },
                values: new object[,]
                {
                    { 1, "Hấp hoặc luộc bí đỏ cắt miếng đến khi thật mềm.", 1, (byte)1 },
                    { 2, "Phi thơm tỏi băm với bơ lạt, cho bí đỏ vào xào sơ.", 1, (byte)2 },
                    { 3, "Xay nhuyễn, thêm sữa tươi, đun nhỏ lửa 5 phút rồi nêm nếm vừa ăn.", 1, (byte)3 },
                    { 4, "Ướp ức gà với chút muối hồng, tiêu đen và dầu olive trong 10 phút.", 2, (byte)1 },
                    { 5, "Áp chảo lửa vừa mỗi mặt 4 phút, tắt bếp đậy nắp ủ thêm 3 phút rồi thái lát.", 2, (byte)2 },
                    { 6, "Trộn rau xà lách, cà chua bi bổ đôi, trứng luộc cùng sốt mè rang và xếp gà lên trên.", 2, (byte)3 },
                    { 7, "Chần trứng trong nước sôi nhẹ khoảng 3 phút cho lòng đào.", 3, (byte)1 },
                    { 8, "Áp chảo nấm đùi gà với bơ lạt đến khi vàng giòn.", 3, (byte)2 },
                    { 9, "Xếp nấm ra đĩa, đặt trứng chần lên trên và rưới sốt tương Nhật.", 3, (byte)3 },
                    { 10, "Trộn bột mì, trứng gà và sữa tươi thành hỗn hợp sánh mịn.", 4, (byte)1 },
                    { 11, "Đun chảy bơ lạt trong chảo, múc bột và rán lửa nhỏ mỗi mặt 2 phút.", 4, (byte)2 },
                    { 12, "Xếp bánh thành chồng, rưới mật ong và thêm bơ tươi lên trên.", 4, (byte)3 },
                    { 13, "Sơ chế tôm, ướp chút muối tiêu.", 5, (byte)1 },
                    { 14, "Xào tôm đến khi chín đỏ rồi múc ra.", 5, (byte)2 },
                    { 15, "Xào cà chua đến khi nhuyễn, đổ trứng đã đánh vào, trộn tôm lại và nêm nếm.", 5, (byte)3 }
                });

            migrationBuilder.InsertData(
                table: "SavedRecipes",
                columns: new[] { "RecipeId", "UserId", "SavedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 1, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Recipe",
                table: "Comments",
                columns: new[] { "RecipeId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId",
                table: "Ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Ratings_Recipe_User",
                table: "Ratings",
                columns: new[] { "RecipeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_Name",
                table: "RecipeIngredients",
                column: "IngredientName")
                .Annotation("SqlServer:Include", new[] { "RecipeId" });

            migrationBuilder.CreateIndex(
                name: "UQ_RecipeIngredients",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "IngredientName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ApprovedBy",
                table: "Recipes",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Author_Status",
                table: "Recipes",
                columns: new[] { "AuthorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CategoryId",
                table: "Recipes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Status_Category",
                table: "Recipes",
                columns: new[] { "Status", "CategoryId" })
                .Annotation("SqlServer:Include", new[] { "AuthorId", "CookTime" });

            migrationBuilder.CreateIndex(
                name: "UQ_RecipeSteps",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedRecipes_RecipeId",
                table: "SavedRecipes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "RecipeSteps");

            migrationBuilder.DropTable(
                name: "SavedRecipes");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
