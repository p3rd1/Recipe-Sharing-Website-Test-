# Sơ đồ project CookingBaby

Bộ sơ đồ được vẽ lại từ mã nguồn trong `CookingBaby/` và script `Database/SQLQueryckb.sql`.

| File | Nội dung |
|---|---|
| `01-project-structure.svg` | Cấu trúc repository và thông tin kỹ thuật |
| `02-mvc-layers.svg` | Các controller, model/service và Razor view hiện có |
| `03-mvc-ef6-architecture.svg` | Luồng request/response MVC qua EF6 tới SQL Server |
| `04-cookingbabydb-erd.svg` | ERD 8 bảng, 11 khóa ngoại và các ràng buộc chính |
| `generate-diagrams.mjs` | Source tạo lại bốn SVG |

Các SVG là đồ họa vector, có thể phóng to và chèn vào Word, PowerPoint hoặc tài liệu web. Chỉnh source rồi chạy:

```powershell
node .\docs\diagrams\generate-diagrams.mjs
```

Không đưa thư mục `artifacts/`, cấu hình kết nối Somee hoặc thông tin đăng nhập host vào bộ sơ đồ.
