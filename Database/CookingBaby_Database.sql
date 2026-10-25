USE master;
GO
IF DB_ID(N'CookingBabyDB') IS NULL
    CREATE DATABASE CookingBabyDB COLLATE Vietnamese_CI_AS;
GO
USE CookingBabyDB;
GO

-- 1. Xem danh sách UserId để biết tài khoản nào là Admin, tài khoản nào là Siêu đầu bếp nhí
SELECT UserId, Username, DisplayName FROM dbo.Users;

-- 2. Cập nhật: Món tôm thuộc về Siêu đầu bếp nhí
UPDATE dbo.Recipes 
SET AuthorId = (SELECT UserId FROM dbo.Users WHERE Username = 'sieudaubepnhi')
WHERE Title LIKE N'%Tôm%';

-- 3. Cập nhật: Món súp và bánh kếp thuộc về Quản trị viên
UPDATE dbo.Recipes 
SET AuthorId = (SELECT UserId FROM dbo.Users WHERE Username = 'admin')
WHERE Title LIKE N'%Súp%' OR Title LIKE N'%Pancake%' OR Title LIKE N'%Bánh Kếp%';
GO
-- Cập nhật tên ảnh cho món tôm
UPDATE dbo.Recipes
SET ImageUrl = 'tomxaotrungcachua.jpg'
WHERE Title LIKE N'%tôm%' OR Title LIKE N'%Tôm%';
GO

UPDATE dbo.Recipes SET ImageUrl = N'supbido.jpg' WHERE RecipeId = 1;
UPDATE dbo.Recipes SET ImageUrl = N'saladucga.jpg' WHERE RecipeId = 2;
UPDATE dbo.Recipes SET ImageUrl = N'trungchan.jpeg' WHERE RecipeId = 3;
UPDATE dbo.Recipes SET ImageUrl = N'pancake.jpg' WHERE RecipeId = 4;
GO

DROP TABLE IF EXISTS dbo.SavedRecipes;
DROP TABLE IF EXISTS dbo.Comments;
DROP TABLE IF EXISTS dbo.Ratings;
DROP TABLE IF EXISTS dbo.RecipeSteps;
DROP TABLE IF EXISTS dbo.RecipeIngredients;
DROP TABLE IF EXISTS dbo.Recipes;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Users;
GO

/* ---------- 1. USERS ---------- */
CREATE TABLE dbo.Users (
    UserId        INT IDENTITY(1,1) NOT NULL,
    Username      NVARCHAR(50)  NOT NULL,
    DisplayName   NVARCHAR(100) NOT NULL,          
    Email         NVARCHAR(150) NOT NULL,
    PasswordHash  NVARCHAR(255) NOT NULL,          
    Role          NVARCHAR(20)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'User',
    IsActive      BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt     DATETIME2(0)  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Users          PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT CK_Users_Role     CHECK (Role IN (N'User', N'Admin'))
);
GO

/* ---------- 2. CATEGORIES ---------- */
CREATE TABLE dbo.Categories (
    CategoryId    INT IDENTITY(1,1) NOT NULL,
    CategoryName  NVARCHAR(100) NOT NULL,            
    ThemeColor    CHAR(7)       NULL,              
    DisplayOrder  TINYINT       NOT NULL CONSTRAINT DF_Categories_Order DEFAULT 0,
    CONSTRAINT PK_Categories      PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_Name UNIQUE (CategoryName)
);
GO

/* ---------- 3. RECIPES ---------- */
CREATE TABLE dbo.Recipes (
    RecipeId     INT IDENTITY(1,1) NOT NULL,
    Title        NVARCHAR(200) NOT NULL,          
    Description  NVARCHAR(500) NOT NULL,           
    CookTime     SMALLINT      NOT NULL CONSTRAINT DF_Recipes_CookTime DEFAULT 20,  
    ImageUrl     NVARCHAR(500) NULL,               
    CategoryId   INT           NOT NULL,
    AuthorId     INT           NOT NULL,          
    Status       NVARCHAR(20)  NOT NULL CONSTRAINT DF_Recipes_Status DEFAULT N'Chờ duyệt',
    ApprovedBy   INT           NULL,             
    ApprovedAt   DATETIME2(0)  NULL,
    CreatedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_Recipes_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt    DATETIME2(0)  NULL,
    CONSTRAINT PK_Recipes          PRIMARY KEY (RecipeId),
    CONSTRAINT FK_Recipes_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT FK_Recipes_Author   FOREIGN KEY (AuthorId)   REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Recipes_Approver FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Recipes_CookTime CHECK (CookTime > 0),
    CONSTRAINT CK_Recipes_Status   CHECK (Status IN (N'Chờ duyệt', N'Đã duyệt', N'Đã ẩn'))
);
CREATE INDEX IX_Recipes_Status_Category ON dbo.Recipes (Status, CategoryId) INCLUDE (AuthorId, CookTime);
CREATE INDEX IX_Recipes_Author_Status   ON dbo.Recipes (AuthorId, Status);   -- tab "Quản lý cá nhân"
GO

/* ---------- 4. RECIPE INGREDIENTS ---------- */
CREATE TABLE dbo.RecipeIngredients (
    IngredientId    INT IDENTITY(1,1) NOT NULL,
    RecipeId        INT           NOT NULL,
    IngredientName  NVARCHAR(100) NOT NULL,        -- "Ức gà", "Cà chua"...
    SortOrder       TINYINT       NOT NULL CONSTRAINT DF_RecipeIngredients_Order DEFAULT 0,
    CONSTRAINT PK_RecipeIngredients        PRIMARY KEY (IngredientId),
    CONSTRAINT FK_RecipeIngredients_Recipe FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(RecipeId) ON DELETE CASCADE,
    CONSTRAINT UQ_RecipeIngredients        UNIQUE (RecipeId, IngredientName)
);
CREATE INDEX IX_RecipeIngredients_Name ON dbo.RecipeIngredients (IngredientName) INCLUDE (RecipeId);  -- lọc nhanh theo nguyên liệu
GO

/* ---------- 5. RECIPE STEPS ---------- */
CREATE TABLE dbo.RecipeSteps (
    StepId       INT IDENTITY(1,1) NOT NULL,
    RecipeId     INT            NOT NULL,
    StepNumber   TINYINT        NOT NULL,
    Instruction  NVARCHAR(1000) NOT NULL,
    CONSTRAINT PK_RecipeSteps        PRIMARY KEY (StepId),
    CONSTRAINT FK_RecipeSteps_Recipe FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(RecipeId) ON DELETE CASCADE,
    CONSTRAINT UQ_RecipeSteps        UNIQUE (RecipeId, StepNumber),
    CONSTRAINT CK_RecipeSteps_Number CHECK (StepNumber > 0)
);
GO

/* ---------- 6. RATINGS ---------- */
CREATE TABLE dbo.Ratings (
    RatingId   INT IDENTITY(1,1) NOT NULL,
    RecipeId   INT          NOT NULL,
    UserId     INT          NOT NULL,
    Stars      TINYINT      NOT NULL,
    CreatedAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Ratings_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Ratings        PRIMARY KEY (RatingId),
    CONSTRAINT FK_Ratings_Recipe FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(RecipeId) ON DELETE CASCADE,
    CONSTRAINT FK_Ratings_User   FOREIGN KEY (UserId)   REFERENCES dbo.Users(UserId)     ON DELETE CASCADE,
    CONSTRAINT UQ_Ratings_Recipe_User UNIQUE (RecipeId, UserId),
    CONSTRAINT CK_Ratings_Stars  CHECK (Stars BETWEEN 1 AND 5)
);
GO

/* ---------- 7. COMMENTS ---------- */
CREATE TABLE dbo.Comments (
    CommentId  INT IDENTITY(1,1) NOT NULL,
    RecipeId   INT            NOT NULL,
    UserId     INT            NOT NULL,
    Content    NVARCHAR(1000) NOT NULL,
    CreatedAt  DATETIME2(0)   NOT NULL CONSTRAINT DF_Comments_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Comments        PRIMARY KEY (CommentId),
    CONSTRAINT FK_Comments_Recipe FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(RecipeId) ON DELETE CASCADE,
    CONSTRAINT FK_Comments_User   FOREIGN KEY (UserId)   REFERENCES dbo.Users(UserId)     ON DELETE CASCADE
);
CREATE INDEX IX_Comments_Recipe ON dbo.Comments (RecipeId, CreatedAt DESC);
GO

/* ---------- 8. SAVED RECIPES ---------- */
CREATE TABLE dbo.SavedRecipes (
    UserId    INT          NOT NULL,
    RecipeId  INT          NOT NULL,
    SavedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_SavedRecipes_SavedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_SavedRecipes        PRIMARY KEY (UserId, RecipeId),
    CONSTRAINT FK_SavedRecipes_User   FOREIGN KEY (UserId)   REFERENCES dbo.Users(UserId)     ON DELETE CASCADE,
    CONSTRAINT FK_SavedRecipes_Recipe FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(RecipeId) ON DELETE CASCADE
);

/* =====================================================================
   VIEWS 
   ===================================================================== */
GO

-- Thẻ món ăn: điểm TB (số lượt) / "Mới (0)", tag nguyên liệu, đầu bếp, danh mục
CREATE OR ALTER VIEW dbo.vw_RecipeCards
AS
SELECT  r.RecipeId, r.Title, r.Description, r.CookTime, r.ImageUrl, r.Status, r.CreatedAt,
        c.CategoryId, c.CategoryName, c.ThemeColor,
        u.UserId      AS AuthorId,
        u.DisplayName AS AuthorName,
        rs.AvgRating,                   
        rs.RatingCount,
        ing.Ingredients                 
FROM dbo.Recipes r
JOIN dbo.Categories c ON c.CategoryId = r.CategoryId
JOIN dbo.Users u      ON u.UserId     = r.AuthorId
OUTER APPLY (
    SELECT CAST(AVG(CAST(Stars AS DECIMAL(4,2))) AS DECIMAL(2,1)) AS AvgRating,
           COUNT(*) AS RatingCount
    FROM dbo.Ratings WHERE RecipeId = r.RecipeId
) rs
OUTER APPLY (
    SELECT STRING_AGG(IngredientName, N', ') WITHIN GROUP (ORDER BY SortOrder) AS Ingredients
    FROM dbo.RecipeIngredients WHERE RecipeId = r.RecipeId
) ing;
GO

-- Đánh giá & Bình luận 
CREATE OR ALTER VIEW dbo.vw_RecipeReviews
AS
SELECT  cm.CommentId, cm.RecipeId, cm.UserId,
        u.DisplayName AS UserName,
        rt.Stars,                      
        cm.Content, cm.CreatedAt
FROM dbo.Comments cm
JOIN dbo.Users u ON u.UserId = cm.UserId
LEFT JOIN dbo.Ratings rt ON rt.RecipeId = cm.RecipeId AND rt.UserId = cm.UserId;
GO

-- Admin Control Panel
CREATE OR ALTER VIEW dbo.vw_AdminStatusCounts
AS
SELECT  COUNT(CASE WHEN Status = N'Đã duyệt'  THEN 1 END) AS DangHienThi,
        COUNT(CASE WHEN Status = N'Chờ duyệt' THEN 1 END) AS ChoDuyet,
        COUNT(CASE WHEN Status = N'Đã ẩn'     THEN 1 END) AS DaAn
FROM dbo.Recipes;
GO

/* =====================================================================
   STORED PROCEDURES 
   ===================================================================== */

-- Nút "Duyệt bài" (chỉ Admin)
CREATE OR ALTER PROCEDURE dbo.sp_ApproveRecipe
    @RecipeId INT, @AdminId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @AdminId AND Role = N'Admin' AND IsActive = 1)
        THROW 50001, N'Truy cập bị từ chối: Chỉ tài khoản Quản trị viên mới được duyệt bài!', 1;

    UPDATE dbo.Recipes
       SET Status = N'Đã duyệt', ApprovedBy = @AdminId,
           ApprovedAt = SYSDATETIME(), UpdatedAt = SYSDATETIME()
     WHERE RecipeId = @RecipeId;

    IF @@ROWCOUNT = 0 THROW 50002, N'Không tìm thấy công thức.', 1;
END
GO

-- Nút "Ẩn bài" (chỉ Admin)
CREATE OR ALTER PROCEDURE dbo.sp_HideRecipe
    @RecipeId INT, @AdminId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @AdminId AND Role = N'Admin' AND IsActive = 1)
        THROW 50001, N'Truy cập bị từ chối: Chỉ tài khoản Quản trị viên mới được ẩn bài!', 1;

    UPDATE dbo.Recipes
       SET Status = N'Đã ẩn', UpdatedAt = SYSDATETIME()
     WHERE RecipeId = @RecipeId;

    IF @@ROWCOUNT = 0 THROW 50002, N'Không tìm thấy công thức.', 1;
END
GO

-- Nút "Gửi Đánh Giá"
CREATE OR ALTER PROCEDURE dbo.sp_SubmitReview
    @RecipeId INT, @UserId INT, @Stars TINYINT, @Content NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE RecipeId = @RecipeId AND Status = N'Đã duyệt')
        THROW 50003, N'Công thức không tồn tại hoặc chưa được hiển thị.', 1;

    BEGIN TRAN;
        UPDATE dbo.Ratings SET Stars = @Stars, CreatedAt = SYSDATETIME()
         WHERE RecipeId = @RecipeId AND UserId = @UserId;
        IF @@ROWCOUNT = 0
            INSERT dbo.Ratings (RecipeId, UserId, Stars) VALUES (@RecipeId, @UserId, @Stars);

        IF NULLIF(LTRIM(RTRIM(@Content)), N'') IS NOT NULL
            INSERT dbo.Comments (RecipeId, UserId, Content)
            VALUES (@RecipeId, @UserId, LTRIM(RTRIM(@Content)));
    COMMIT;
END
GO

-- Nút * : lưu / bỏ lưu 
CREATE OR ALTER PROCEDURE dbo.sp_ToggleSaved
    @UserId INT, @RecipeId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.SavedRecipes WHERE UserId = @UserId AND RecipeId = @RecipeId)
    BEGIN
        DELETE dbo.SavedRecipes WHERE UserId = @UserId AND RecipeId = @RecipeId;
        SELECT CAST(0 AS BIT) AS IsSaved;
    END
    ELSE
    BEGIN
        INSERT dbo.SavedRecipes (UserId, RecipeId) VALUES (@UserId, @RecipeId);
        SELECT CAST(1 AS BIT) AS IsSaved;
    END
END
GO

/* =====================================================================
   DỮ LIỆU MẪU 
   ===================================================================== */
INSERT dbo.Users (Username, DisplayName, Email, PasswordHash, Role) VALUES
 (N'sieudaubepnhi', N'Siêu đầu bếp nhí', N'owner@cookingbaby.demo', N'DEMO_HASH_CHANGE_ME', N'User'),
 (N'thanhbinh',     N'Thanh Bình',       N'binh@cookingbaby.demo',  N'DEMO_HASH_CHANGE_ME', N'User'),
 (N'quynhnhu',      N'Quỳnh Như',        N'nhu@cookingbaby.demo',   N'DEMO_HASH_CHANGE_ME', N'User'),
 (N'admin',         N'Quản trị viên',    N'admin@cookingbaby.demo', N'DEMO_HASH_CHANGE_ME', N'Admin');

INSERT dbo.Categories (CategoryName, ThemeColor, DisplayOrder) VALUES
 (N'Ăn Sáng & Món Nhanh', '#F97316', 1),
 (N'Món Chính & Súp Hầm', '#F59E0B', 2),
 (N'Bánh & Tráng Miệng', '#EC4899', 3),
 (N'Healthy & Rau Củ', '#22C55E', 4);
GO

DECLARE @owner INT = (SELECT UserId FROM dbo.Users WHERE Username = N'sieudaubepnhi'),
        @binh  INT = (SELECT UserId FROM dbo.Users WHERE Username = N'thanhbinh'),
        @nhu   INT = (SELECT UserId FROM dbo.Users WHERE Username = N'quynhnhu'),
        @admin INT = (SELECT UserId FROM dbo.Users WHERE Username = N'admin');

DECLARE @catSang   INT = (SELECT CategoryId FROM dbo.Categories WHERE DisplayOrder = 1),
        @catChinh  INT = (SELECT CategoryId FROM dbo.Categories WHERE DisplayOrder = 2),
        @catBanh   INT = (SELECT CategoryId FROM dbo.Categories WHERE DisplayOrder = 3),
        @catHealth INT = (SELECT CategoryId FROM dbo.Categories WHERE DisplayOrder = 4);

DECLARE @r1 INT, @r2 INT, @r3 INT, @r4 INT, @r5 INT;

-- Recipes 
INSERT dbo.Recipes (Title, Description, CookTime, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Súp Bí Đỏ Kem Tươi Hầm Kiểu Âu',
        N'Món súp sánh mịn, vàng óng giống hệt nồi súp của 4 em bé trong logo Cooking Baby. Vị ngọt tự nhiên từ bí đỏ quện cùng sữa tươi và bơ lạt béo thơm.',
        25, @catChinh, @owner, N'Đã duyệt', @admin, SYSDATETIME());
SET @r1 = SCOPE_IDENTITY();

INSERT dbo.Recipes (Title, Description, CookTime, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Salad Ức Gà Áp Chảo & Cà Chua Bi Sốt Mè',
        N'Ức gà giữ được độ mềm mọng nước nhờ bí quyết áp chảo đậy nắp, ăn kèm rau xanh và cà chua bi tươi mát.',
        20, @catHealth, @binh, N'Đã duyệt', @admin, SYSDATETIME());
SET @r2 = SCOPE_IDENTITY();

INSERT dbo.Recipes (Title, Description, CookTime, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Trứng Chần Nấm Đùi Gà Sốt Tương Nhật',
        N'Món ăn sáng siêu tốc giàu đạm với lòng đào béo ngậy quyện cùng nấm đùi gà áp chảo giòn ngọt.',
        12, @catSang, @nhu, N'Đã duyệt', @admin, SYSDATETIME());
SET @r3 = SCOPE_IDENTITY();

INSERT dbo.Recipes (Title, Description, CookTime, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Bánh Kếp (Pancake) Mật Ong Bơ Tươi',
        N'Những chiếc bánh pancake tròn xoe, bông xốp thơm lừng mùi bơ sữa, chỉ mất 15 phút chuẩn bị cho bữa sáng cuối tuần.',
        15, @catBanh, @owner, N'Đã duyệt', @admin, SYSDATETIME());
SET @r4 = SCOPE_IDENTITY();

INSERT dbo.Recipes (Title, Description, CookTime, CategoryId, AuthorId, Status)   -- đang chờ Admin duyệt
VALUES (N'Tôm Tươi Xào Cà Chua Trứng Mềm Mượt',
        N'Sự kết hợp kinh điển giữa tôm tươi giòn ngọt và sốt trứng cà chua sánh mịn, cực kỳ bắt cơm.',
        18, @catChinh, @owner, N'Chờ duyệt');
SET @r5 = SCOPE_IDENTITY();

-- RecipeIngredients
INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder) VALUES
 (@r1, N'Bí đỏ', 1),    (@r1, N'Sữa tươi', 2), (@r1, N'Bơ lạt', 3),   (@r1, N'Tỏi băm', 4),
 (@r2, N'Ức gà', 1),    (@r2, N'Cà chua', 2),  (@r2, N'Trứng gà', 3), (@r2, N'Xà lách', 4),
 (@r3, N'Trứng gà', 1), (@r3, N'Nấm', 2),      (@r3, N'Bơ lạt', 3),
 (@r4, N'Trứng gà', 1), (@r4, N'Sữa tươi', 2), (@r4, N'Bơ lạt', 3),   (@r4, N'Bột mì', 4),
 (@r5, N'Tôm tươi', 1), (@r5, N'Trứng gà', 2), (@r5, N'Cà chua', 3);

-- RecipeSteps
INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction) VALUES
 (@r1, 1, N'Hấp hoặc luộc bí đỏ cắt miếng đến khi thật mềm.'),
 (@r1, 2, N'Phi thơm tỏi băm với bơ lạt, cho bí đỏ vào xào sơ.'),
 (@r1, 3, N'Xay nhuyễn, thêm sữa tươi, đun nhỏ lửa 5 phút rồi nêm nếm vừa ăn.'),

 (@r2, 1, N'Ướp ức gà với chút muối hồng, tiêu đen và dầu olive trong 10 phút.'),
 (@r2, 2, N'Áp chảo lửa vừa mỗi mặt 4 phút, tắt bếp đậy nắp ủ thêm 3 phút rồi thái lát.'),
 (@r2, 3, N'Trộn rau xà lách, cà chua bi bổ đôi, trứng luộc cùng sốt mè rang và xếp gà lên trên.'),

 (@r3, 1, N'Chần trứng trong nước sôi nhẹ khoảng 3 phút cho lòng đào.'),
 (@r3, 2, N'Áp chảo nấm đùi gà với bơ lạt đến khi vàng giòn.'),
 (@r3, 3, N'Xếp nấm ra đĩa, đặt trứng chần lên trên và rưới sốt tương Nhật.'),

 (@r4, 1, N'Trộn bột mì, trứng gà và sữa tươi thành hỗn hợp sánh mịn.'),
 (@r4, 2, N'Đun chảy bơ lạt trong chảo, múc bột và rán lửa nhỏ mỗi mặt 2 phút.'),
 (@r4, 3, N'Xếp bánh thành chồng, rưới mật ong và thêm bơ tươi lên trên.'),

 (@r5, 1, N'Sơ chế tôm, ướp chút muối tiêu.'),
 (@r5, 2, N'Xào tôm đến khi chín đỏ rồi múc ra.'),
 (@r5, 3, N'Xào cà chua đến khi nhuyễn, đổ trứng đã đánh vào, trộn tôm lại và nêm nếm.');

-- Ratings
INSERT dbo.Ratings (RecipeId, UserId, Stars) VALUES
 (@r1, @binh, 5), (@r1, @nhu, 5),
 (@r2, @owner, 5),
 (@r3, @binh, 4);

-- Comments
INSERT dbo.Comments (RecipeId, UserId, Content) VALUES
 (@r2, @owner, N'Cách ủ ức gà này giúp thịt mềm thật sự, không bị bã chút nào.');

-- SavedRecipes
INSERT dbo.SavedRecipes (UserId, RecipeId) VALUES (@owner, @r1), (@owner, @r2);
GO

/* =====================================================================
   TRUY VẤN MẪU
   ===================================================================== */
DECLARE @me INT = (SELECT UserId FROM dbo.Users WHERE Username = N'sieudaubepnhi');

-- 1 Khám Phá Công Thức: món đã duyệt, sắp xếp "Điểm đánh giá cao nhất"
SELECT * FROM dbo.vw_RecipeCards
WHERE Status = N'Đã duyệt'
ORDER BY AvgRating DESC, RatingCount DESC;

-- 2 Lọc nhanh theo nguyên liệu sẵn có 
DECLARE @pick TABLE (Name NVARCHAR(100));
INSERT @pick VALUES (N'Trứng gà'), (N'Bơ lạt');
SELECT c.*
FROM dbo.vw_RecipeCards c
WHERE c.Status = N'Đã duyệt'
  AND (SELECT COUNT(*) FROM dbo.RecipeIngredients i
       JOIN @pick p ON p.Name = i.IngredientName
       WHERE i.RecipeId = c.RecipeId) = (SELECT COUNT(*) FROM @pick);

-- 3 Quản Lý Cá Nhân
SELECT * FROM dbo.vw_RecipeCards WHERE AuthorId = @me ORDER BY CreatedAt DESC;

-- 4 Đã Lưu
SELECT c.* FROM dbo.SavedRecipes s
JOIN dbo.vw_RecipeCards c ON c.RecipeId = s.RecipeId
WHERE s.UserId = @me ORDER BY s.SavedAt DESC;

-- 5 Admin
SELECT * FROM dbo.vw_AdminStatusCounts;
SELECT RecipeId, Title, CategoryName, AuthorName, AvgRating, RatingCount, Status
FROM dbo.vw_RecipeCards ORDER BY CreatedAt;
GO
