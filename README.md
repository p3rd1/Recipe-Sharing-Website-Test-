# CookingBaby – Hướng dẫn cài đặt CSDL và cấu hình kết nối (SQL Server Express)

Tài liệu này hướng dẫn:

1. Chạy file `Database/SQLQueryckb.sql` trên **SQL Server Management Studio (SSMS)** để tạo CSDL `CookingBabyDB`.
2. Chỉnh `data source` trong `CookingBaby/Web.config` cho máy dùng **SQL Server Express**.
3. Chạy thử website và xử lý lỗi thường gặp.

---

## 0. Yêu cầu môi trường

| Thành phần | Yêu cầu |
|---|---|
| SQL Server | **2017 trở lên** (script dùng `STRING_AGG`, `CREATE OR ALTER`, `DROP TABLE IF EXISTS`). Bản Express đều dùng được. |
| SSMS | Bản mới nhất (tải miễn phí từ Microsoft) |
| Visual Studio | 2019 hoặc 2022, đã cài workload **ASP.NET and web development** |
| .NET Framework | 4.8 (project ASP.NET MVC 5 + Entity Framework 6.5.1) |

Cấu trúc thư mục liên quan:

```
Recipe-Sharing-Website-Test-/
├── CookingBaby.sln
├── Database/
│   ├── SQLQueryckb.sql          <- file SQL duy nhất cần chạy trên SSMS
│   └── CookingBaby_Database.sql <- bản cũ, không cần chạy
└── CookingBaby/
    └── Web.config                   <- file cần chỉnh data source
```

---

## 1. Chạy `SQLQueryckb.sql` trên SSMS

### Bước 1.1 – Xác định tên instance SQL Express

Mặc định SQL Express cài với tên instance là `SQLEXPRESS`, nên tên server đầy đủ thường là:

```
.\SQLEXPRESS
```

(hoặc `localhost\SQLEXPRESS`, hoặc `TENMAY\SQLEXPRESS`).

Nếu không chắc, kiểm tra bằng một trong các cách sau:

- **Cách 1:** Mở *Services* (`Win + R` → `services.msc`) và tìm dịch vụ `SQL Server (SQLEXPRESS)`. Phần trong ngoặc là tên instance. Dịch vụ phải ở trạng thái **Running**.
- **Cách 2:** Mở PowerShell và chạy:
  ```powershell
  Get-Service | Where-Object { $_.Name -like 'MSSQL*' }
  ```
  Dịch vụ `MSSQL$SQLEXPRESS` nghĩa là instance tên `SQLEXPRESS`. Dịch vụ `MSSQLSERVER` nghĩa là instance mặc định (không có tên).

### Bước 1.2 – Kết nối vào SQL Server bằng SSMS

1. Mở **SSMS**. Hộp thoại **Connect to Server** hiện ra.
2. Điền:
   - **Server type:** `Database Engine`
   - **Server name:** `.\SQLEXPRESS` (hoặc tên bạn xác định ở bước 1.1)
   - **Authentication:** `Windows Authentication`
3. Với SSMS 19 trở lên, nếu báo lỗi chứng chỉ SSL, tick **Trust server certificate** (hoặc chọn *Encryption: Optional*).
4. Bấm **Connect**.

> Nếu bạn dùng SQL Authentication (tài khoản `sa`), chọn `SQL Server Authentication` rồi nhập Login/Password.

### Bước 1.3 – Mở file script

1. Trong SSMS: **File → Open → File...** (phím tắt `Ctrl + O`).
2. Chọn `Database/SQLQueryckb.sql`.
3. File mở trong cửa sổ query. Kiểm tra góc dưới bên phải đang kết nối đúng server (ví dụ `.\SQLEXPRESS`).

### Bước 1.4 – Chạy script

1. Không bôi đen đoạn nào, bấm **Execute** (hoặc `F5`) để chạy **toàn bộ** file.
2. Chờ tab **Messages** báo `Commands completed successfully` (hoặc không có dòng chữ đỏ).

Script thực hiện theo thứ tự:

| Phần | Nội dung |
|---|---|
| Đầu file | Tạo database `CookingBabyDB` (collation `Vietnamese_CI_AS`) nếu chưa có |
| Tiếp theo | `DROP TABLE IF EXISTS` rồi tạo lại 8 bảng: `Users`, `Categories`, `Recipes`, `RecipeIngredients`, `RecipeSteps`, `Ratings`, `Comments`, `SavedRecipes` |
| Tiếp theo | Tạo các view, stored procedure và dữ liệu mẫu (4 tài khoản, danh mục, 20 công thức, nguyên liệu, các bước, đánh giá, bình luận) |
| Cuối file | Thêm 4 cột vào `Users` (`LastLoginAt`, `FailedLoginCount`, `LockoutEnd`, `PasswordChangedAt`) và đặt lại mật khẩu cho 4 tài khoản mẫu |

> ⚠️ **Cảnh báo mất dữ liệu:** Phần đầu script **xóa toàn bộ 8 bảng rồi tạo lại**. Nếu `CookingBabyDB` đã có dữ liệu thật, hãy **backup trước** (chuột phải database → *Tasks → Back Up...*). Chạy lại script sẽ đưa dữ liệu về trạng thái mẫu.

### Bước 1.5 – Kiểm tra kết quả

1. Trong **Object Explorer**, chuột phải **Databases → Refresh**. Phải thấy `CookingBabyDB`.
2. Mở rộng `CookingBabyDB → Tables`, kiểm tra có đủ 8 bảng `dbo.*`.
3. Chạy thử:
   ```sql
   USE CookingBabyDB;
   SELECT UserId, Username, Role, IsActive FROM dbo.Users;
   SELECT COUNT(*) AS SoCongThuc FROM dbo.Recipes;
   ```
   Kết quả mong đợi: 4 tài khoản và 20 công thức.

### Tài khoản mẫu để đăng nhập website

Theo ghi chú trong script (đoạn "Khôi phục mật khẩu 123456"), cả 4 tài khoản dùng mật khẩu **`123456`**:

| Username | Tên hiển thị | Email |
|---|---|---|
| `admin` | Quản trị viên | admin@cookingbaby.demo |
| `sieudaubepnhi` | Siêu đầu bếp nhí | owner@cookingbaby.demo |
| `thanhbinh` | Thanh Bình | binh@cookingbaby.demo |
| `quynhnhu` | Quỳnh Như | nhu@cookingbaby.demo |

---

## 2. Chỉnh `data source` trong `Web.config` cho SQL Express

### 2.1 – Tìm chuỗi kết nối

Mở `CookingBaby/Web.config`, kéo xuống cuối file, tìm thẻ `<connectionStrings>`. Hiện tại là:

```xml
<connectionStrings>
  <add name="CookingBabyDBEntities"
       connectionString="metadata=res://*/Models.CookingBabyDB.csdl|res://*/Models.CookingBabyDB.ssdl|res://*/Models.CookingBabyDB.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=.;initial catalog=CookingBabyDB;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True&quot;"
       providerName="System.Data.EntityClient" />
</connectionStrings>
```

Giá trị `data source=.` chỉ trỏ tới **instance mặc định** (`MSSQLSERVER`). Máy dùng SQL Express thường chỉ có instance tên `SQLEXPRESS`, nên cần sửa.

### 2.2 – Sửa thành

Chỉ đổi đúng phần `data source=.` thành `data source=.\SQLEXPRESS`:

```xml
<connectionStrings>
  <add name="CookingBabyDBEntities"
       connectionString="metadata=res://*/Models.CookingBabyDB.csdl|res://*/Models.CookingBabyDB.ssdl|res://*/Models.CookingBabyDB.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=.\SQLEXPRESS;initial catalog=CookingBabyDB;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True&quot;"
       providerName="System.Data.EntityClient" />
</connectionStrings>
```

Lưu ý khi sửa:

- Trong `Web.config` (XML) chỉ cần **một dấu `\`** (`.\SQLEXPRESS`), không cần gấp đôi như trong code C#.
- **Giữ nguyên** `&quot;` quanh phần `provider connection string`. Không thay bằng dấu `"` thật, nếu không file XML sẽ lỗi.
- **Không sửa** phần `metadata=res://*/...`.
- `initial catalog=CookingBabyDB` phải trùng tên database tạo ở bước 1.

### 2.3 – Các cách viết `data source` tương đương

Dùng đúng tên server mà bạn đã nhập khi kết nối SSMS:

| Tình huống | Giá trị `data source` |
|---|---|
| SQL Express, instance mặc định `SQLEXPRESS` | `.\SQLEXPRESS` |
| Cách viết khác của cùng instance | `localhost\SQLEXPRESS` hoặc `(local)\SQLEXPRESS` |
| Dùng tên máy | `TENMAY\SQLEXPRESS` (xem tên máy trong *Settings → System → About*) |
| Instance đặt tên khác lúc cài | `.\TEN_INSTANCE` |
| SQL Server bản đầy đủ, instance mặc định | `.` (giữ nguyên như file gốc) |
| SQL Server LocalDB | `(LocalDB)\MSSQLLocalDB` |

> Mẹo: tên server hiển thị trong SSMS Object Explorer, hoặc chạy `SELECT @@SERVERNAME;`, chính là thứ cần điền vào `data source`.

### 2.4 – Nếu dùng SQL Authentication thay vì Windows Authentication

Thay `integrated security=True` bằng `user id` và `password`:

```
data source=.\SQLEXPRESS;initial catalog=CookingBabyDB;user id=sa;password=MatKhauCuaBan;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True
```

(Vẫn đặt trong cặp `&quot;...&quot;` như ví dụ ở 2.2.)

Điều kiện: instance phải bật chế độ **Mixed Mode** (chuột phải server trong SSMS → *Properties → Security → SQL Server and Windows Authentication mode*, sau đó restart dịch vụ SQL Server) và tài khoản `sa` đã được bật.

> Không commit mật khẩu thật lên Git. Nếu làm việc nhóm, mỗi người tự giữ một bản `Web.config` riêng hoặc dùng `Windows Authentication`.

---

## 3. Chạy thử website

1. Mở `CookingBaby.sln` bằng Visual Studio.
2. Chuột phải Solution → **Restore NuGet Packages** (nếu thư mục `packages` thiếu).
3. **Build → Rebuild Solution** (`Ctrl + Shift + B`), đảm bảo không có lỗi.
4. Nhấn **F5** hoặc **Ctrl + F5**. Website chạy bằng IIS Express, địa chỉ mặc định `http://localhost:51566/`.
5. Đăng nhập thử bằng `admin` / `123456`.

Với `integrated security=True`, website kết nối bằng tài khoản Windows đang chạy Visual Studio. Tài khoản này cần có quyền trên `CookingBabyDB`. Thông thường người cài SQL Express đã là sysadmin nên không cần làm gì thêm.

---

## 4. Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân và cách xử lý |
|---|---|
| `A network-related or instance-specific error occurred... (provider: SQL Network Interfaces, error: 26/40)` | Sai tên instance trong `data source`, hoặc dịch vụ chưa chạy. Kiểm tra lại bước 1.1 và bật dịch vụ `SQL Server (SQLEXPRESS)`. Nếu vẫn lỗi, bật dịch vụ **SQL Server Browser**. |
| `Cannot open database "CookingBabyDB" requested by the login` | Chưa chạy script (chưa có database) hoặc `initial catalog` sai tên. Chạy lại bước 1. |
| `Login failed for user 'DOMAIN\user'` | Tài khoản Windows chưa có quyền. Trong SSMS: *Security → Logins → New Login*, thêm tài khoản, gán role `db_owner` cho `CookingBabyDB`. Hoặc chuyển sang SQL Authentication (mục 2.4). |
| SSMS báo `The certificate chain was issued by an authority that is not trusted` | Tick **Trust server certificate** ở màn hình đăng nhập SSMS. Website đã có sẵn `TrustServerCertificate=True`. |
| `'STRING_AGG' is not a recognized built-in function name` hoặc `Incorrect syntax near 'OR'` (ở `CREATE OR ALTER`) | Phiên bản SQL Server quá cũ (dưới 2017). Cài SQL Server Express 2017 trở lên. |
| `Msg 50002: Thieu tai khoan. Dung cap nhat.` | Phần cuối script không thấy đủ 4 tài khoản mẫu, thường do phần chèn dữ liệu phía trên bị lỗi. Xem lỗi đầu tiên trong tab Messages và chạy lại toàn bộ file. |
| Tiếng Việt hiển thị sai dấu | Mở file bằng SSMS với encoding UTF-8 (script đã lưu UTF-8). Không copy script qua trình soạn thảo đổi encoding. |
| Website báo `The underlying provider failed on Open` | Gần như luôn là lỗi `data source`. Kiểm tra lại mục 2 rồi **Rebuild** và chạy lại. |
| Sửa `Web.config` nhưng không có tác dụng | Dừng IIS Express (biểu tượng khay hệ thống → Exit), rồi chạy lại website. |

---

## 5. Tóm tắt nhanh

1. SSMS → kết nối `.\SQLEXPRESS` → mở `Database/SQLQueryckb.sql` → **F5**.
2. Sửa `Web.config`: `data source=.` thành `data source=.\SQLEXPRESS`.
3. Mở `CookingBaby.sln` → Rebuild → F5 → đăng nhập `admin` / `123456`.
