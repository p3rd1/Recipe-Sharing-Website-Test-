/*
    CookingBabyDB - script khoi tao day du cho Recipe Sharing Website
    SQL Server 2017+; tao schema, tai khoan mau, 5 cong thuc co ban va them 15 cong thuc.

    CANH BAO: Script xoa va tao lai cac bang du lieu cua CookingBabyDB.
    Chi chay tren database test/empty hoac sau khi da sao luu du lieu can giu.
*/
USE master;
GO
IF DB_ID(N'CookingBabyDB') IS NULL
    CREATE DATABASE CookingBabyDB COLLATE Vietnamese_CI_AS;
GO
USE CookingBabyDB;
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
    -- AccountController khóa 15 phút khi nhập sai mật khẩu đủ 5 lần.
    FailedLoginCount INT        NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0),
    LockoutEnd    DATETIME2(0)  NULL,
    CONSTRAINT PK_Users          PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT CK_Users_Role     CHECK (Role IN (N'User', N'Admin')),
    CONSTRAINT CK_Users_FailedLoginCount_NonNegative CHECK (FailedLoginCount >= 0)
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
CREATE INDEX IX_Recipes_Author_Status   ON dbo.Recipes (AuthorId, Status);
GO

/* ---------- 4. RECIPE INGREDIENTS ---------- */
CREATE TABLE dbo.RecipeIngredients (
    IngredientId    INT IDENTITY(1,1) NOT NULL,
    RecipeId        INT           NOT NULL,
    IngredientName  NVARCHAR(100) NOT NULL,
    SortOrder       TINYINT       NOT NULL CONSTRAINT DF_RecipeIngredients_Order DEFAULT 0,
    CONSTRAINT PK_RecipeIngredients        PRIMARY KEY (IngredientId),
    CONSTRAINT FK_RecipeIngredients_Recipe FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(RecipeId) ON DELETE CASCADE,
    CONSTRAINT UQ_RecipeIngredients        UNIQUE (RecipeId, IngredientName)
);
CREATE INDEX IX_RecipeIngredients_Name ON dbo.RecipeIngredients (IngredientName) INCLUDE (RecipeId);
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
GO

/* =====================================================================
   VIEWS
   ===================================================================== */

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
SELECT
    ISNULL(cm.CommentId, 0) AS CommentId,
    rt.RecipeId,
    rt.UserId,
    u.DisplayName AS UserName,
    rt.Stars,
    cm.Content,
    ISNULL(cm.CreatedAt, rt.CreatedAt) AS CreatedAt
FROM dbo.Ratings rt
JOIN dbo.Users u ON u.UserId = rt.UserId
LEFT JOIN dbo.Comments cm
    ON cm.RecipeId = rt.RecipeId
    AND cm.UserId = rt.UserId

UNION ALL

-- Hiển thị bình luận không có đánh giá sao
SELECT
    cm.CommentId,
    cm.RecipeId,
    cm.UserId,
    u.DisplayName AS UserName,
    CAST(NULL AS TINYINT) AS Stars,
    cm.Content,
    cm.CreatedAt
FROM dbo.Comments cm
JOIN dbo.Users u ON u.UserId = cm.UserId
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Ratings rt
    WHERE rt.RecipeId = cm.RecipeId
      AND rt.UserId = cm.UserId
);
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
   DỮ LIỆU MẪU CHUẨN XÁC (TÍCH HỢP SẴN ẢNH & ĐÚNG TÁC GIẢ)
   ===================================================================== */
INSERT dbo.Users (Username, DisplayName, Email, PasswordHash, Role) VALUES
(N'sieudaubepnhi', N'Siêu đầu bếp nhí', N'owner@cookingbaby.demo', N'PBKDF2-SHA256$150000$fZZLS8tJTiroCkR1Nx1PpQ==$tnMSixQygliEOePt9mk8CT0z/mlSPU1TaKWnpM2YQWs=', N'User'),
 (N'thanhbinh',     N'Thanh Bình',       N'binh@cookingbaby.demo',  N'PBKDF2-SHA256$150000$5vXpfmOvn/4+/rEOuYom2Q==$DPfo7YR6AGgtAThX0wq9Ou041a3Ryl+cvYp4YV/OooQ=', N'User'),
 (N'quynhnhu',      N'Quỳnh Như',        N'nhu@cookingbaby.demo',   N'PBKDF2-SHA256$150000$p3sJnVOFB9hhNhY1d0SbVw==$eiBz3LWofqgug0OYq68CJGH8nSrncRcBH7AiErzviJw=', N'User'),
 (N'admin',         N'Quản trị viên',    N'admin@cookingbaby.demo', N'PBKDF2-SHA256$150000$BgXemfRte4i7HjziBh825Q==$zzhpf1zvN9Ly7bR3Dgj2ms1XPxp5MZHumFpA8mMelQ8=', N'Admin');

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

-- 1. Món Súp Bí Đỏ (Tác giả: Siêu đầu bếp nhí)
INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Súp Bí Đỏ Kem Tươi Hầm Kiểu Âu',
        N'Món súp sánh mịn, vàng óng giống hệt nồi súp của 4 em bé trong logo Cooking Baby. Vị ngọt tự nhiên từ bí đỏ quện cùng sữa tươi và bơ lạt béo thơm.',
        25, N'supbido.jpg', @catChinh, @owner, N'Đã duyệt', @admin, SYSDATETIME());
SET @r1 = SCOPE_IDENTITY();

-- 2. Món Salad Ức Gà (Tác giả: Thanh Bình)
INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Salad Ức Gà Áp Chảo & Cà Chua Bi Sốt Mè',
        N'Ức gà giữ được độ mềm mọng nước nhờ bí quyết áp chảo đậy nắp, ăn kèm rau xanh và cà chua bi tươi mát.',
        20, N'saladucga.jpg', @catHealth, @binh, N'Đã duyệt', @admin, SYSDATETIME());
SET @r2 = SCOPE_IDENTITY();

-- 3. Món Trứng Chần (Tác giả: Quỳnh Như)
INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Trứng Chần Nấm Đùi Gà Sốt Tương Nhật',
        N'Món ăn sáng siêu tốc giàu đạm với lòng đào béo ngậy quyện cùng nấm đùi gà áp chảo giòn ngọt.',
        12, N'trungchan.jpeg', @catSang, @nhu, N'Đã duyệt', @admin, SYSDATETIME());
SET @r3 = SCOPE_IDENTITY();

-- 4. Món Bánh Kếp Pancake (Tác giả: Siêu đầu bếp nhí)
INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
VALUES (N'Bánh Kếp (Pancake) Mật Ong Bơ Tươi',
        N'Những chiếc bánh pancake tròn xoe, bông xốp thơm lừng mùi bơ sữa, chỉ mất 15 phút chuẩn bị cho bữa sáng cuối tuần.',
        15, N'pancake.jpg', @catBanh, @owner, N'Đã duyệt', @admin, SYSDATETIME());
SET @r4 = SCOPE_IDENTITY();

-- 5. Món Tôm Xào Cà Chua Trứng (Tác giả: Siêu đầu bếp nhí - Chờ duyệt)
INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status)
VALUES (N'Tôm Tươi Xào Cà Chua Trứng Mềm Mượt',
        N'Sự kết hợp kinh điển giữa tôm tươi giòn ngọt và sốt trứng cà chua sánh mịn, cực kỳ bắt cơm.',
        18, N'tomxaotrungcachua.jpg', @catChinh, @owner, N'Chờ duyệt');
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

-- Hiển thị kết quả kiểm tra sau khi tạo xong
SELECT RecipeId, Title, ImageUrl, AuthorId, AuthorName, Status, Ingredients
FROM dbo.vw_RecipeCards
ORDER BY RecipeId;
GO

SELECT UserId, Username, Email, Role
FROM dbo.Users;




USE [CookingBabyDB];
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Kiem tra bang Users
    IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
        THROW 50001, N'Khong tim thay bang Users.', 1;

    -- Kiem tra du 4 tai khoan
    IF (
        SELECT COUNT(*)
        FROM dbo.Users
        WHERE Username IN
        (N'sieudaubepnhi', N'thanhbinh', N'quynhnhu', N'admin')
    ) <> 4
        THROW 50002, N'Thieu tai khoan. Dung cap nhat.', 1;

    -- Bo sung 4 cot neu chua co
    IF COL_LENGTH('dbo.Users', 'LastLoginAt') IS NULL
        ALTER TABLE dbo.Users
        ADD LastLoginAt DATETIME2(0) NULL;

    IF COL_LENGTH('dbo.Users', 'FailedLoginCount') IS NULL
        ALTER TABLE dbo.Users
        ADD FailedLoginCount INT NOT NULL
        CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0);

    IF COL_LENGTH('dbo.Users', 'LockoutEnd') IS NULL
        ALTER TABLE dbo.Users
        ADD LockoutEnd DATETIME2(0) NULL;

    IF COL_LENGTH('dbo.Users', 'PasswordChangedAt') IS NULL
        ALTER TABLE dbo.Users
        ADD PasswordChangedAt DATETIME2(0) NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Users')
          AND name = N'CK_Users_FailedLoginCount_NonNegative'
    )
        ALTER TABLE dbo.Users WITH CHECK
        ADD CONSTRAINT CK_Users_FailedLoginCount_NonNegative
            CHECK (FailedLoginCount >= 0);

    -- Khoi phuc mat khau 123456 cho 4 tai khoan
    UPDATE dbo.Users
    SET PasswordHash =
        CASE Username
            WHEN N'sieudaubepnhi' THEN
            N'PBKDF2-SHA256$150000$/Mtablc2faZ2aUAWt6CoaA==$4WMGeg/ELOE4fbQnEl3RNUktxLy6rF2Oc+uq6iHdYeY='

            WHEN N'thanhbinh' THEN
            N'PBKDF2-SHA256$150000$OoJ75ayt13ojYcpk4HACyw==$R+ECIJKXLH9KYBwrscWpzr2UW8NOutO1fyg97hw70zQ='

            WHEN N'quynhnhu' THEN
            N'PBKDF2-SHA256$150000$s5H9StznwQJRKNSPdVmYPQ==$fiJgWzVo+R9L4py5c7qNoSVeyQNCaQ5AMYOoI2s4H3w='

            WHEN N'admin' THEN
            N'PBKDF2-SHA256$150000$RvqVqHs1zBrzVXdIdbm6WA==$oNMjdZI9jy7dVmDPuui9J3WxPvLHVL+OTwxVIpQ02og='

            ELSE PasswordHash
        END
    WHERE Username IN
    (N'sieudaubepnhi', N'thanhbinh', N'quynhnhu', N'admin');

    IF @@ROWCOUNT <> 4
        THROW 50003, N'Khong cap nhat du 4 tai khoan.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- Kiem tra ket qua
SELECT
    Username,
    Role,
    IsActive,
    CASE
        WHEN PasswordHash LIKE 'PBKDF2-SHA256$150000$%'
        THEN 'Hash da cap nhat'
        ELSE 'Can kiem tra'
    END AS PasswordStatus
FROM dbo.Users
WHERE Username IN
(N'sieudaubepnhi', N'thanhbinh', N'quynhnhu', N'admin');
GO



/* ==================== 15 CONG THUC BO SUNG ==================== */
USE [CookingBabyDB];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Đảm bảo tài khoản quynhnhu tồn tại trong bảng Users (dùng đúng cột DisplayName)
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'quynhnhu')
    BEGIN
        INSERT INTO dbo.Users (Username, DisplayName, Email, PasswordHash, Role, IsActive, CreatedAt, FailedLoginCount)
        SELECT TOP (1) N'quynhnhu', N'Lê Kiều Quỳnh Như', N'quynhnhu@cookingbaby.vn', PasswordHash, Role, IsActive, SYSDATETIME(), 0
        FROM dbo.Users WHERE Username = N'thanhbinh';
    END

    -- 2. Khai báo ID của Admin và 3 tác giả
    DECLARE @AdminId      INT = (SELECT TOP (1) UserId FROM dbo.Users WHERE Username = N'admin' AND Role = N'Admin');
    DECLARE @IdSieuDauBep INT = (SELECT TOP (1) UserId FROM dbo.Users WHERE Username = N'sieudaubepnhi');
    DECLARE @IdThanhBinh  INT = (SELECT TOP (1) UserId FROM dbo.Users WHERE Username = N'thanhbinh');
    DECLARE @IdQuynhNhu   INT = (SELECT TOP (1) UserId FROM dbo.Users WHERE Username = N'quynhnhu');

    -- 3. Khai báo ID 4 danh mục gốc
    DECLARE @catSang    INT = (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE CategoryName = N'Ăn Sáng & Món Nhanh');
    DECLARE @catChinh   INT = (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE CategoryName = N'Món Chính & Súp Hầm');
    DECLARE @catBanh    INT = (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE CategoryName = N'Bánh & Tráng Miệng');
    DECLARE @catHealthy INT = (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE CategoryName = N'Healthy & Rau Củ');

    IF @IdSieuDauBep IS NULL OR @AdminId IS NULL OR @IdThanhBinh IS NULL OR @IdQuynhNhu IS NULL
        THROW 51001, N'Thieu tai khoan demo sieudaubepnhi, admin, thanhbinh hoac quynhnhu.', 1;
    IF @catSang IS NULL OR @catChinh IS NULL OR @catBanh IS NULL OR @catHealthy IS NULL
        THROW 51002, N'Thieu 1 trong 4 danh muc goc. Khong tu y them du lieu.', 1;

    DECLARE @RecipeId INT;

    -- =================================================================================
    -- CHIA ĐỀU 15 MÓN CHO 3 TÁC GIẢ (MỖI NGƯỜI 5 MÓN)
    -- =================================================================================

    -- Mon 6 [Tác giả: sieudaubepnhi | Đánh giá: thanhbinh]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Mì Ý Bít Tết Bò Mềm Sốt Tiêu Đen Đậm Đà Kèm Trứng Lòng Đào')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Mì Ý Bít Tết Bò Mềm Sốt Tiêu Đen Đậm Đà Kèm Trứng Lòng Đào',
                N'Sợi mì Ý dai ngon quyện sốt tiêu đen đậm đà, ăn kèm thịt bò thăn áp chảo mềm mọng và trứng ốp la lòng đào béo ngậy.',
                23, N'miybittetbotrung.jpg', @catSang, @IdSieuDauBep, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Mì Ý', 1),
            (@RecipeId, N'Thịt bò thăn', 2),
            (@RecipeId, N'Trứng gà', 3),
            (@RecipeId, N'Tiêu đen', 4),
            (@RecipeId, N'Muối', 5),
            (@RecipeId, N'Dầu ăn', 6),
            (@RecipeId, N'Sốt tiêu đen', 7);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Luộc mì Ý trong nước sôi có chút muối và dầu ăn khoảng 8 phút rồi vớt ra để ráo.'),
            (@RecipeId, 2, N'Ướp thịt bò thăn với muối, tiêu đen; áp chảo lửa lớn mỗi mặt 2 phút rồi gắp ra thái lát.'),
            (@RecipeId, 3, N'Chiên trứng ốp la lòng đào, nấu sôi sốt tiêu đen rồi rưới lên đĩa mì, thịt bò và trứng.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdThanhBinh, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdSieuDauBep WHERE Title = N'Mì Ý Bít Tết Bò Mềm Sốt Tiêu Đen Đậm Đà Kèm Trứng Lòng Đào';
        UPDATE rt SET rt.UserId = @IdThanhBinh FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Mì Ý Bít Tết Bò Mềm Sốt Tiêu Đen Đậm Đà Kèm Trứng Lòng Đào';
    END

    -- Mon 7 [Tác giả: thanhbinh | Đánh giá: quynhnhu]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Đậu Hủ Cuộn Rong Biển Giòn Rụm Sốt Teriyaki Óng Ả Chuẩn Nhật')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Đậu Hủ Cuộn Rong Biển Giòn Rụm Sốt Teriyaki Óng Ả Chuẩn Nhật',
                N'Đậu hũ chiên giòn rụm cuộn rong biển đậm vị, quyện cùng sốt teriyaki mặn ngọt óng ả và mè rang bùi bùi. Món ngon giản dị, giàu đạm thực vật.',
                15, N'dauhucuonrongbien.jpg', @catHealthy, @IdThanhBinh, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Đậu hũ trắng', 1),
            (@RecipeId, N'Lá rong biển khô', 2),
            (@RecipeId, N'Bột bắp', 3),
            (@RecipeId, N'Mè trắng rang', 4),
            (@RecipeId, N'Nước tương', 5),
            (@RecipeId, N'Dầu hào chay', 6),
            (@RecipeId, N'Mật ong', 7),
            (@RecipeId, N'Dầu ăn', 8),
            (@RecipeId, N'Tiêu xay', 9);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Cắt đậu hũ trắng thành thanh dài, cuộn chặt trong lá rong biển khô và lăn qua một lớp mỏng bột bắp.'),
            (@RecipeId, 2, N'Áp chảo vàng giòn các mặt cuộn đậu hũ với dầu ăn.'),
            (@RecipeId, 3, N'Pha nước tương, dầu hào chay, mật ong, tiêu xay và chút nước lọc; đổ vào chảo rim nhỏ lửa đến khi sốt sánh lại, rắc mè rang lên trên.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdQuynhNhu, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdThanhBinh WHERE Title = N'Đậu Hủ Cuộn Rong Biển Giòn Rụm Sốt Teriyaki Óng Ả Chuẩn Nhật';
        UPDATE rt SET rt.UserId = @IdQuynhNhu FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Đậu Hủ Cuộn Rong Biển Giòn Rụm Sốt Teriyaki Óng Ả Chuẩn Nhật';
    END

    -- Mon 8 [Tác giả: quynhnhu | Đánh giá: sieudaubepnhi]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Bánh Flan Caramel Sữa Tươi Mềm Mịn')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Bánh Flan Caramel Sữa Tươi Mềm Mịn',
                N'Bánh flan mềm mịn, tan ngay trong miệng với vị béo thơm của trứng sữa hòa quyện cùng lớp sốt caramel đắng nhẹ ngọt thanh.',
                25, N'banhflan.jpg', @catBanh, @IdQuynhNhu, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Trứng gà', 1),
            (@RecipeId, N'Sữa tươi không đường', 2),
            (@RecipeId, N'Sữa đặc có đường', 3),
            (@RecipeId, N'Đường cát', 4),
            (@RecipeId, N'Nước lọc', 5),
            (@RecipeId, N'Tinh chất vani hoặc rượu Rhum', 6);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Thắng đường cát với nước lọc đến khi chuyển màu cánh gián thì rót vào đáy khuôn.'),
            (@RecipeId, 2, N'Khuấy nhẹ trứng gà, đun ấm sữa tươi không đường và sữa đặc có đường rồi trộn đều cùng tinh chất vani, lọc qua rây cho mịn.'),
            (@RecipeId, 3, N'Rót hỗn hợp vào khuôn và hấp cách thủy lửa nhỏ khoảng 25 phút đến khi bánh chín mịn.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdSieuDauBep, 4);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdQuynhNhu WHERE Title = N'Bánh Flan Caramel Sữa Tươi Mềm Mịn';
        UPDATE rt SET rt.UserId = @IdSieuDauBep FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Bánh Flan Caramel Sữa Tươi Mềm Mịn';
    END

    -- Mon 9 [Tác giả: sieudaubepnhi | Đánh giá: thanhbinh]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Cá Hồi Áp Chảo Sốt Bơ Chanh Măng Tây')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Cá Hồi Áp Chảo Sốt Bơ Chanh Măng Tây',
                N'Phi lê cá hồi da giòn rụm, bên trong hồng đào béo ngậy kết hợp nước sốt bơ chanh thanh mát ăn kèm măng tây giòn ngọt.',
                20, N'cahoisotbochanh.jpeg', @catChinh, @IdSieuDauBep, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Cá hồi phi lê', 1),
            (@RecipeId, N'Măng tây', 2),
            (@RecipeId, N'Bơ lạt', 3),
            (@RecipeId, N'Chanh tươi', 4),
            (@RecipeId, N'Tỏi băm', 5),
            (@RecipeId, N'Dầu olive', 6),
            (@RecipeId, N'Muối biển', 7),
            (@RecipeId, N'Tiêu xay', 8);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Thấm khô cá hồi, ướp nhẹ hai mặt với muối biển, tiêu xay và dầu olive trong 10 phút.'),
            (@RecipeId, 2, N'Áp chảo mặt da cá hồi trước 4 phút cho giòn vàng, lật mặt áp thêm 2 phút rồi gắp ra.'),
            (@RecipeId, 3, N'Xào nhanh măng tây với bơ lạt; đun chảy bơ với tỏi băm và nước cốt chanh làm sốt rưới lên cá.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdThanhBinh, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdSieuDauBep WHERE Title = N'Cá Hồi Áp Chảo Sốt Bơ Chanh Măng Tây';
        UPDATE rt SET rt.UserId = @IdThanhBinh FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Cá Hồi Áp Chảo Sốt Bơ Chanh Măng Tây';
    END

    -- Mon 10 [Tác giả: thanhbinh | Đánh giá: quynhnhu]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Bánh Chuối Yến Mạch Nướng Mật Ong Hạt Óc Chó (Eat Clean Giảm Cân)')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Bánh Chuối Yến Mạch Nướng Mật Ong Hạt Óc Chó (Eat Clean Giảm Cân)',
                N'Bánh nướng lành mạnh không đường tinh luyện, tận dụng vị ngọt tự nhiên từ chuối chín và mật ong thơm bùi yến mạch.',
                35, N'banhchuoiyenmach.jpg', @catBanh, @IdThanhBinh, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Chuối chín', 1),
            (@RecipeId, N'Yến mạch', 2),
            (@RecipeId, N'Trứng gà', 3),
            (@RecipeId, N'Mật ong', 4),
            (@RecipeId, N'Sữa tươi không đường', 5),
            (@RecipeId, N'Hạt óc chó', 6),
            (@RecipeId, N'Bột nở', 7);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Nghiền nhuyễn chuối chín, trộn đều cùng trứng gà, mật ong và sữa tươi không đường.'),
            (@RecipeId, 2, N'Thêm yến mạch cán dẹt và chút bột nở trộn thành hỗn hợp sánh đặc, để nghỉ 10 phút.'),
            (@RecipeId, 3, N'Đổ khuôn, rắc hạt óc chó đập dập lên mặt và nướng ở 175°C trong 25 phút cho bánh chín vàng.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdQuynhNhu, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdThanhBinh WHERE Title = N'Bánh Chuối Yến Mạch Nướng Mật Ong Hạt Óc Chó (Eat Clean Giảm Cân)';
        UPDATE rt SET rt.UserId = @IdQuynhNhu FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Bánh Chuối Yến Mạch Nướng Mật Ong Hạt Óc Chó (Eat Clean Giảm Cân)';
    END

    -- Mon 11 [Tác giả: quynhnhu | Đánh giá: sieudaubepnhi]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Cơm Cuộn Rong Biển Kimbap Thanh Cua Trứng Cuộn Đầy Màu Sắc Thơm Lừng Dầu Mè')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Cơm Cuộn Rong Biển Kimbap Thanh Cua Trứng Cuộn Đầy Màu Sắc Thơm Lừng Dầu Mè',
                N'Những khoanh kimbap đầy màu sắc từ cà rốt, dưa leo, trứng chiên vàng ươm và thanh cua ngọt thanh thơm mùi dầu mè.',
                25, N'comcuonrongbien.jpg', @catSang, @IdQuynhNhu, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Cơm dẻo', 1),
            (@RecipeId, N'Rong biển khô', 2),
            (@RecipeId, N'Thanh cua', 3),
            (@RecipeId, N'Trứng gà', 4),
            (@RecipeId, N'Cà rốt', 5),
            (@RecipeId, N'Dưa leo', 6),
            (@RecipeId, N'Dầu mè', 7),
            (@RecipeId, N'Muối', 8),
            (@RecipeId, N'Dầu ăn', 9);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Trộn cơm nóng với 1 muỗng dầu mè và chút muối cho hạt cơm bóng dẻo.'),
            (@RecipeId, 2, N'Tráng trứng gà với chút dầu ăn rồi thái sợi dài; cà rốt và dưa leo rửa sạch, thái thanh dài vừa ăn.'),
            (@RecipeId, 3, N'Trải lá rong biển, dàn mỏng cơm, xếp nhân vào giữa, cuộn chặt tay và cắt khoanh vừa ăn.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdSieuDauBep, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdQuynhNhu WHERE Title = N'Cơm Cuộn Rong Biển Kimbap Thanh Cua Trứng Cuộn Đầy Màu Sắc Thơm Lừng Dầu Mè';
        UPDATE rt SET rt.UserId = @IdSieuDauBep FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Cơm Cuộn Rong Biển Kimbap Thanh Cua Trứng Cuộn Đầy Màu Sắc Thơm Lừng Dầu Mè';
    END

    -- Mon 12 [Tác giả: sieudaubepnhi | Đánh giá: thanhbinh]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Canh Kim Chi Thịt Ba Chỉ Đậu Hũ Non Chua Cay Ấm Bụng Chuẩn Hàn')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Canh Kim Chi Thịt Ba Chỉ Đậu Hũ Non Chua Cay Ấm Bụng Chuẩn Hàn',
                N'Nồi canh đỏ au chua cay ấm bụng chuẩn vị Hàn Quốc với kim chi cải thảo giòn ngon, thịt ba chỉ béo mềm và đậu hũ non.',
                25, N'canhkimchi.jpg', @catChinh, @IdSieuDauBep, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Kim chi cải thảo', 1),
            (@RecipeId, N'Thịt ba chỉ', 2),
            (@RecipeId, N'Đậu hũ non', 3),
            (@RecipeId, N'Hành boa-rô', 4),
            (@RecipeId, N'Ớt bột Hàn Quốc', 5),
            (@RecipeId, N'Tỏi băm', 6),
            (@RecipeId, N'Dầu ăn', 7);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Phi thơm tỏi băm với chút dầu ăn, xào săn thịt ba chỉ thái mỏng cùng kim chi cải thảo và 1 muỗng ớt bột Hàn Quốc.'),
            (@RecipeId, 2, N'Đổ 800ml nước dùng vào đun sôi khoảng 12 phút cho kim chi tiết vị chua ngọt.'),
            (@RecipeId, 3, N'Thả đậu hũ non cắt khoanh và hành boa-rô thái lát vào nấu thêm 3 phút, nêm vừa ăn rồi tắt bếp.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdThanhBinh, 4);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdSieuDauBep WHERE Title = N'Canh Kim Chi Thịt Ba Chỉ Đậu Hũ Non Chua Cay Ấm Bụng Chuẩn Hàn';
        UPDATE rt SET rt.UserId = @IdThanhBinh FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Canh Kim Chi Thịt Ba Chỉ Đậu Hũ Non Chua Cay Ấm Bụng Chuẩn Hàn';
    END

    -- Mon 13 [Tác giả: thanhbinh | Đánh giá: quynhnhu]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Phở Bò Tái Lăn Truyền Thống Nước Dùng Thanh Ngọt Thơm Mùi Quế Hồi')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Phở Bò Tái Lăn Truyền Thống Nước Dùng Thanh Ngọt Thơm Mùi Quế Hồi',
                N'Nước dùng trong veo ngọt thanh hầm từ xương bò cùng quế hồi thảo quả thơm lừng, ăn kèm bánh phở mềm dai, thịt bò tái.',
                55, N'phobotailan.jpg', @catChinh, @IdThanhBinh, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Bánh phở tươi', 1),
            (@RecipeId, N'Thịt thăn bò', 2),
            (@RecipeId, N'Xương ống bò', 3),
            (@RecipeId, N'Hành tây', 4),
            (@RecipeId, N'Gừng tươi', 5),
            (@RecipeId, N'Quế', 6),
            (@RecipeId, N'Hoa hồi', 7),
            (@RecipeId, N'Hành lá', 8),
            (@RecipeId, N'Ngò gai', 9),
            (@RecipeId, N'Nước mắm', 10),
            (@RecipeId, N'Đường phèn', 11),
            (@RecipeId, N'Muối', 12);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Chần sơ xương ống bò rồi ninh bằng nồi áp suất với nước lọc cùng hành tây, gừng nướng, quế và hoa hồi đã rang thơm trong 45 phút để lấy nước dùng ngọt thanh.'),
            (@RecipeId, 2, N'Nêm nước mắm, đường phèn và muối vào nồi nước dùng cho vừa miệng; thịt thăn bò thái lát mỏng.'),
            (@RecipeId, 3, N'Chần bánh phở qua nước sôi cho vào tô, xếp thịt bò, hành ngò lên trên rồi chan nước dùng đang sôi.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdQuynhNhu, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdThanhBinh WHERE Title = N'Phở Bò Tái Lăn Truyền Thống Nước Dùng Thanh Ngọt Thơm Mùi Quế Hồi';
        UPDATE rt SET rt.UserId = @IdQuynhNhu FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Phở Bò Tái Lăn Truyền Thống Nước Dùng Thanh Ngọt Thơm Mùi Quế Hồi';
    END

    -- Mon 14 [Tác giả: quynhnhu | Đánh giá: sieudaubepnhi]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Chè Đậu Đỏ Hạt Sen Đường Phèn')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Chè Đậu Đỏ Hạt Sen Đường Phèn',
                N'Đậu đỏ hầm bở tơi kết hợp hạt sen bùi bùi trong làn nước chè đường phèn thanh ngọt giải nhiệt cực tốt cho ngày nắng nóng.',
                50, N'chedaudohatsen.jpg', @catBanh, @IdQuynhNhu, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Đậu đỏ', 1),
            (@RecipeId, N'Hạt sen tươi', 2),
            (@RecipeId, N'Đường phèn', 3),
            (@RecipeId, N'Bột sắn dây', 4),
            (@RecipeId, N'Nước cốt dừa', 5),
            (@RecipeId, N'Lá dứa', 6);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Đậu đỏ ngâm nở, ninh mềm với nước và lá dứa; hạt sen tươi thông tâm rồi cho vào nấu cùng đậu đỏ thêm 15 phút cho chín bùi.'),
            (@RecipeId, 2, N'Cho đường phèn vào nấu tan hoàn toàn, hòa chút bột sắn dây với nước lạnh đổ từ từ vào khuấy nhẹ cho nước chè sánh mượt.'),
            (@RecipeId, 3, N'Múc chè ra chén, chan thêm nước cốt dừa béo ngậy và thưởng thức.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdSieuDauBep, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdQuynhNhu WHERE Title = N'Chè Đậu Đỏ Hạt Sen Đường Phèn';
        UPDATE rt SET rt.UserId = @IdSieuDauBep FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Chè Đậu Đỏ Hạt Sen Đường Phèn';
    END

    -- Mon 15 [Tác giả: sieudaubepnhi | Đánh giá: thanhbinh]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Gỏi Cuốn Tôm Thịt Rau Sống')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Gỏi Cuốn Tôm Thịt Rau Sống',
                N'Món cuốn truyền thống thanh đạm ít calo với bánh tráng mỏng dẻo bọc tôm tươi luộc đỏ au, thịt nạc mềm, bún tươi và đa dạng rau thơm.',
                25, N'goicuontomthit.jpg', @catHealthy, @IdSieuDauBep, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Bánh tráng', 1),
            (@RecipeId, N'Tôm tươi', 2),
            (@RecipeId, N'Thịt nạc heo', 3),
            (@RecipeId, N'Bún tươi', 4),
            (@RecipeId, N'Xà lách', 5),
            (@RecipeId, N'Rau thơm', 6),
            (@RecipeId, N'Hẹ', 7),
            (@RecipeId, N'Đậu phộng rang', 8);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Luộc chín tôm tươi bóc vỏ chẻ đôi; thịt nạc heo luộc chín thái lát mỏng.'),
            (@RecipeId, 2, N'Làm ẩm nhẹ bánh tráng, xếp rau xà lách, rau thơm, bún tươi và thịt luộc lên trên rồi cuộn nửa vòng.'),
            (@RecipeId, 3, N'Đặt tôm tươi và cọng lá hẹ lên mặt bánh tráng rồi cuộn chặt tay đến hết, chấm kèm tương đậu phộng hoặc nước mắm chua ngọt.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdThanhBinh, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdSieuDauBep WHERE Title = N'Gỏi Cuốn Tôm Thịt Rau Sống';
        UPDATE rt SET rt.UserId = @IdThanhBinh FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Gỏi Cuốn Tôm Thịt Rau Sống';
    END

    -- Mon 16 [Tác giả: thanhbinh | Đánh giá: quynhnhu]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Đậu Hũ Nhồi Thịt Sốt Cà Chua Đậm Đà')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Đậu Hũ Nhồi Thịt Sốt Cà Chua Đậm Đà',
                N'Miếng đậu hũ chiên vàng mềm béo ôm trọn nhân thịt bằm mộc nhĩ ngọt thơm, rim thấm đẫm nước sốt cà chua đỏ au cực kỳ đưa cơm.',
                30, N'dauhunhoithitsotcachua.jpg', @catChinh, @IdThanhBinh, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Đậu hũ trắng', 1),
            (@RecipeId, N'Thịt heo bằm', 2),
            (@RecipeId, N'Mộc nhĩ', 3),
            (@RecipeId, N'Cà chua', 4),
            (@RecipeId, N'Hành tím', 5),
            (@RecipeId, N'Hành lá', 6),
            (@RecipeId, N'Nước mắm', 7),
            (@RecipeId, N'Tiêu xay', 8),
            (@RecipeId, N'Dầu ăn', 9);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Trộn đều thịt heo bằm với mộc nhĩ thái nhỏ, hành tím băm, nước mắm và tiêu xay; cắt đôi miếng đậu hũ, rạch nhẹ giữa và nhồi nhân thịt vào.'),
            (@RecipeId, 2, N'Áp chảo vàng đều các mặt đậu hũ với dầu ăn cho thịt săn lại rồi gắp ra đĩa.'),
            (@RecipeId, 3, N'Xào nhuyễn cà chua với chút nước lọc và gia vị thành sốt sánh, xếp đậu hũ vào rim nhỏ lửa 10 phút, rắc hành lá.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdQuynhNhu, 4);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdThanhBinh WHERE Title = N'Đậu Hũ Nhồi Thịt Sốt Cà Chua Đậm Đà';
        UPDATE rt SET rt.UserId = @IdQuynhNhu FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Đậu Hũ Nhồi Thịt Sốt Cà Chua Đậm Đà';
    END

    -- Mon 17 [Tác giả: quynhnhu | Đánh giá: sieudaubepnhi]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Rau Câu Dừa Lá Dứa Ba Tầng Thơm Mát')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Rau Câu Dừa Lá Dứa Ba Tầng Thơm Mát',
                N'Món tráng miệng thanh mát ba tầng màu sắc bắt mắt, kết hợp vị ngọt thanh của nước dừa tươi, thơm lừng lá dứa và nước cốt dừa béo ngậy.',
                30, N'raucaudualadua.jpg', @catBanh, @IdQuynhNhu, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Bột rau câu giòn', 1),
            (@RecipeId, N'Nước dừa tươi', 2),
            (@RecipeId, N'Nước cốt dừa', 3),
            (@RecipeId, N'Lá dứa tươi', 4),
            (@RecipeId, N'Đường phèn', 5);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Xay nhuyễn lá dứa với nước lọc lấy nước cốt xanh; nấu tan bột rau câu giòn cùng nước dừa tươi và đường phèn trên lửa vừa.'),
            (@RecipeId, 2, N'Chia hỗn hợp rau câu thành 3 phần: một phần giữ nguyên nước dừa trong, một phần pha nước cốt dừa, một phần pha nước cốt lá dứa.'),
            (@RecipeId, 3, N'Đổ lần lượt từng lớp vào khuôn (đợi lớp dưới se mặt mới đổ lớp tiếp theo), để nguội rồi làm lạnh 2 tiếng trước khi cắt.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdSieuDauBep, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdQuynhNhu WHERE Title = N'Rau Câu Dừa Lá Dứa Ba Tầng Thơm Mát';
        UPDATE rt SET rt.UserId = @IdSieuDauBep FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Rau Câu Dừa Lá Dứa Ba Tầng Thơm Mát';
    END

    -- Mon 18 [Tác giả: sieudaubepnhi | Đánh giá: thanhbinh]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Canh Chua Cá Lóc Bạc Hà Kiểu Miền Tây')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Canh Chua Cá Lóc Bạc Hà Kiểu Miền Tây',
                N'Món canh chuẩn vị miền Tây với vị chua thanh từ me chín, cá lóc thịt trắng ngọt chắc nấu cùng thơm, cà chua, đậu bắp và bạc hà giòn sần sật.',
                28, N'canhchuacaloc.jpg', @catChinh, @IdSieuDauBep, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Cá lóc tươi', 1),
            (@RecipeId, N'Thơm (dứa)', 2),
            (@RecipeId, N'Cà chua', 3),
            (@RecipeId, N'Bạc hà (dọc mùng)', 4),
            (@RecipeId, N'Đậu bắp', 5),
            (@RecipeId, N'Giá đỗ', 6),
            (@RecipeId, N'Me chín', 7),
            (@RecipeId, N'Ngò om', 8),
            (@RecipeId, N'Ngò gai', 9),
            (@RecipeId, N'Nước mắm', 10),
            (@RecipeId, N'Tiêu', 11);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Cá lóc làm sạch cắt khứa, ướp nhẹ với chút nước mắm và tiêu; lọc lấy nước cốt me chín, cắt miếng thơm, cà chua, đậu bắp và bạc hà.'),
            (@RecipeId, 2, N'Đun sôi nước lọc cùng nước cốt me và thơm, thả cá lóc vào nấu khoảng 10 phút cho cá chín tới rồi vớt nhẹ ra đĩa để không bị nát.'),
            (@RecipeId, 3, N'Cho cà chua, đậu bắp, bạc hà và giá đỗ vào nấu chín tới, nêm nước mắm vừa ăn, xếp cá lại vào nồi và rắc rau thơm.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdThanhBinh, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdSieuDauBep WHERE Title = N'Canh Chua Cá Lóc Bạc Hà Kiểu Miền Tây';
        UPDATE rt SET rt.UserId = @IdThanhBinh FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Canh Chua Cá Lóc Bạc Hà Kiểu Miền Tây';
    END

    -- Mon 19 [Tác giả: thanhbinh | Đánh giá: quynhnhu]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Bánh Bông Lan Trứng Muối Sốt Phô Mai Tan Chảy Ngập Chà Bông')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Bánh Bông Lan Trứng Muối Sốt Phô Mai Tan Chảy Ngập Chà Bông',
                N'Cốt bánh bông lan mềm xốp thơm hương vani, phủ đẫm lớp sốt dầu trứng béo ngậy, phô mai Con Bò Cười mặn mà, chà bông và trứng muối.',
                45, N'bonglanphomaitrungmuoi.jpg', @catBanh, @IdThanhBinh, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Bột mì', 1),
            (@RecipeId, N'Trứng gà', 2),
            (@RecipeId, N'Trứng muối', 3),
            (@RecipeId, N'Phô mai Con Bò Cười', 4),
            (@RecipeId, N'Chà bông heo', 5),
            (@RecipeId, N'Sữa tươi không đường', 6),
            (@RecipeId, N'Đường cát', 7),
            (@RecipeId, N'Dầu ăn', 8),
            (@RecipeId, N'Bơ lạt', 9),
            (@RecipeId, N'Tinh chất vani', 10),
            (@RecipeId, N'Rượu gừng', 11);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Tách riêng lòng đỏ và lòng trắng trứng gà; đánh bông cứng lòng trắng với đường cát rồi trộn nhẹ nhàng cùng hỗn hợp lòng đỏ, sữa tươi, dầu ăn, tinh chất vani và bột mì rây mịn, đem nướng ở 165°C trong 30 phút.'),
            (@RecipeId, 2, N'Hấp chín lòng đỏ trứng muối đã ngâm rượu gừng cho thơm; đun cách thủy lòng đỏ trứng gà với đường, dầu ăn và bơ lạt làm sốt.'),
            (@RecipeId, 3, N'Cắt bánh, phết đẫm sốt dầu trứng, đặt phô mai, trứng muối và rắc chà bông lên trên.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdQuynhNhu, 5);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdThanhBinh WHERE Title = N'Bánh Bông Lan Trứng Muối Sốt Phô Mai Tan Chảy Ngập Chà Bông';
        UPDATE rt SET rt.UserId = @IdQuynhNhu FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Bánh Bông Lan Trứng Muối Sốt Phô Mai Tan Chảy Ngập Chà Bông';
    END

    -- Mon 20 [Tác giả: quynhnhu | Đánh giá: sieudaubepnhi]
    IF NOT EXISTS (SELECT 1 FROM dbo.Recipes WHERE Title = N'Cơm Trộn Thập Cẩm Hàn Quốc Chay')
    BEGIN
        INSERT dbo.Recipes (Title, Description, CookTime, ImageUrl, CategoryId, AuthorId, Status, ApprovedBy, ApprovedAt)
        VALUES (N'Cơm Trộn Thập Cẩm Hàn Quốc Chay',
                N'Thố cơm nóng hổi đầy ắp màu sắc từ nấm đùi gà áp chảo, cải bó xôi, cà rốt và giá đỗ quyện cùng sốt tương ớt Hàn Quốc đậm đà.',
                25, N'comtronchay.jpg', @catHealthy, @IdQuynhNhu, N'Đã duyệt', @AdminId, SYSDATETIME());
        SET @RecipeId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.RecipeIngredients (RecipeId, IngredientName, SortOrder)
        VALUES
            (@RecipeId, N'Cơm trắng (hoặc gạo lứt)', 1),
            (@RecipeId, N'Nấm đùi gà', 2),
            (@RecipeId, N'Cải bó xôi', 3),
            (@RecipeId, N'Cà rốt', 4),
            (@RecipeId, N'Giá đỗ', 5),
            (@RecipeId, N'Tương ớt Hàn Quốc (Gochujang)', 6),
            (@RecipeId, N'Dầu mè', 7),
            (@RecipeId, N'Mè trắng rang', 8),
            (@RecipeId, N'Nước tương', 9),
            (@RecipeId, N'Đường', 10),
            (@RecipeId, N'Muối', 11);

        INSERT dbo.RecipeSteps (RecipeId, StepNumber, Instruction)
        VALUES
            (@RecipeId, 1, N'Cà rốt bào sợi xào sơ; cải bó xôi và giá đỗ chần nước sôi rồi trộn chút dầu mè, muối; nấm đùi gà thái lát áp chảo vàng thơm.'),
            (@RecipeId, 2, N'Pha sốt trộn gồm tương ớt Hàn Quốc, nước tương, chút đường, dầu mè và nước lọc.'),
            (@RecipeId, 3, N'Xếp cơm vào thố nóng, bày các loại rau củ, nấm xung quanh, rưới sốt và rắc mè rang.');

        INSERT dbo.Ratings (RecipeId, UserId, Stars)
        VALUES (@RecipeId, @IdSieuDauBep, 4);
    END
    ELSE
    BEGIN
        UPDATE dbo.Recipes SET AuthorId = @IdQuynhNhu WHERE Title = N'Cơm Trộn Thập Cẩm Hàn Quốc Chay';
        UPDATE rt SET rt.UserId = @IdSieuDauBep FROM dbo.Ratings rt JOIN dbo.Recipes r ON rt.RecipeId = r.RecipeId WHERE r.Title = N'Cơm Trộn Thập Cẩm Hàn Quốc Chay';
    END

    COMMIT TRANSACTION;
    PRINT N'Hoan tat bo sung va phan chia tac gia (sieudaubepnhi, thanhbinh, quynhnhu) cho 15 mon demo.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Kiểm tra lại danh sách 15 món kèm theo Tên Tác Giả (Username & DisplayName) vừa gán
SELECT r.RecipeId, r.Title, u.Username AS TaiKhoan, u.DisplayName AS TenTacGia, c.CategoryName, r.CookTime, r.ImageUrl, r.Status,
       (SELECT COUNT(*) FROM dbo.RecipeIngredients i WHERE i.RecipeId = r.RecipeId) AS SoNguyenLieu,
       (SELECT COUNT(*) FROM dbo.RecipeSteps s WHERE s.RecipeId = r.RecipeId) AS SoBuoc,
       (SELECT COUNT(*) FROM dbo.Ratings t WHERE t.RecipeId = r.RecipeId) AS SoDanhGia
FROM dbo.Recipes r
JOIN dbo.Categories c ON c.CategoryId = r.CategoryId
JOIN dbo.Users u ON u.UserId = r.AuthorId
WHERE r.ImageUrl IN (
    N'miybittetbotrung.jpg',
    N'dauhucuonrongbien.jpg',
    N'banhflan.jpg',
    N'cahoisotbochanh.jpeg',
    N'banhchuoiyenmach.jpg',
    N'comcuonrongbien.jpg',
    N'canhkimchi.jpg',
    N'phobotailan.jpg',
    N'chedaudohatsen.jpg',
    N'goicuontomthit.jpg',
    N'dauhunhoithitsotcachua.jpg',
    N'raucaudualadua.jpg',
    N'canhchuacaloc.jpg',
    N'bonglanphomaitrungmuoi.jpg',
    N'comtronchay.jpg'
)
ORDER BY r.RecipeId;
GO

-- Thống kê số lượng món theo từng tác giả (dùng đúng cột DisplayName)
SELECT u.Username, u.DisplayName, COUNT(r.RecipeId) AS SoMonDang
FROM dbo.Users u
LEFT JOIN dbo.Recipes r ON u.UserId = r.AuthorId
GROUP BY u.Username, u.DisplayName;
GO

-- Phân bổ lại ngày đăng rải rác từ tháng 9, tháng 10 đến tháng 11/2026
UPDATE dbo.Recipes
SET CreatedAt = CASE
    WHEN RecipeId % 6 = 0 THEN '2026-09-12 08:30:00'
    WHEN RecipeId % 6 = 1 THEN '2026-09-25 14:15:00'
    WHEN RecipeId % 6 = 2 THEN '2026-10-05 10:20:00'
    WHEN RecipeId % 6 = 3 THEN '2026-10-18 16:45:00'
    WHEN RecipeId % 6 = 4 THEN '2026-11-02 09:10:00'
    ELSE '2026-11-15 11:30:00'
END;
GO

-- Kiểm tra lại kết quả phân bổ theo tháng
SELECT RecipeId, Title, CreatedAt, Status
FROM dbo.Recipes
ORDER BY CreatedAt DESC;
GO
