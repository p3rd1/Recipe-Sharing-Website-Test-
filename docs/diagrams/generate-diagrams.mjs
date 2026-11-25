import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const outDir = path.dirname(fileURLToPath(import.meta.url));
const C = {
  ink: '#17324d', muted: '#5e7184', line: '#d8e2ec', paper: '#f4f7fb', white: '#ffffff',
  blue: '#3478a8', blueSoft: '#eaf4fb', orange: '#e68132', orangeSoft: '#fff2e5',
  teal: '#25877e', tealSoft: '#e8f6f2', purple: '#7859a6', purpleSoft: '#f1edfa',
  rose: '#b54d74', roseSoft: '#fbedf2', green: '#397e59', greenSoft: '#edf7ef',
  slate: '#34495e', gold: '#a8711d', goldSoft: '#fff7df', red: '#a64444', redSoft: '#fff0ef'
};
const esc = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const font = "'Segoe UI', Arial, sans-serif";
const mono = "Consolas, 'Courier New', monospace";

function rect(x, y, w, h, fill = C.white, stroke = C.line, r = 16, sw = 1.5, extra = '') {
  return `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
}
function line(x1, y1, x2, y2, color = C.line, sw = 2, extra = '') {
  return `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="${sw}" ${extra}/>`;
}
function pathEl(d, color = C.muted, sw = 2, marker = true, extra = '') {
  return `<path d="${d}" fill="none" stroke="${color}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round" ${marker ? 'marker-end="url(#arrow)"' : ''} ${extra}/>`;
}
function text(x, y, value, size = 18, color = C.ink, weight = 400, family = font, extra = '') {
  return `<text x="${x}" y="${y}" font-family="${family}" font-size="${size}" fill="${color}" font-weight="${weight}" ${extra}>${esc(value)}</text>`;
}
function base(w, h, title, subtitle, figure) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" role="img" aria-labelledby="title desc">
<title id="title">${esc(title)}</title><desc id="desc">${esc(subtitle)}</desc>
<defs>
  <marker id="arrow" viewBox="0 0 10 10" refX="8.5" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0L10 5L0 10Z" fill="context-stroke"/></marker>
  <linearGradient id="header" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#183c5b"/><stop offset="1" stop-color="#28658f"/></linearGradient>
</defs>
<rect width="${w}" height="${h}" fill="${C.paper}"/>
<rect x="0" y="0" width="${w}" height="142" fill="url(#header)"/>
<rect x="52" y="34" width="66" height="28" rx="14" fill="#ffffff" fill-opacity=".16"/>
${text(85, 53, `HÌNH ${figure}`, 13, '#ffffff', 700, font, 'text-anchor="middle" letter-spacing="1.5"')}
${text(52, 94, title, 32, '#ffffff', 700)}
${text(52, 121, subtitle, 16, '#dceaf5', 400)}
`;
}
function save(name, svg) {
  fs.writeFileSync(path.join(outDir, name), `${svg}</svg>`, 'utf8');
}
function sectionLabel(x, y, label, color = C.blue) {
  return `${rect(x, y - 19, 9, 9, color, color, 3, 0)}${text(x + 18, y - 10, label, 16, color, 700)}`;
}
function card(x, y, w, h, title, subtitle, color = C.blue) {
  return `${rect(x, y, w, h, C.white, C.line, 18, 1.5)}${rect(x, y, w, 60, color, color, 18, 0)}<rect x="${x}" y="${y + 42}" width="${w}" height="18" fill="${color}"/>${text(x + 22, y + 29, title, 21, C.white, 700)}${subtitle ? text(x + 22, y + 49, subtitle, 12, '#ffffff', 500) : ''}`;
}
function pill(x, y, label, fill, color, w) {
  return `${rect(x, y, w, 28, fill, fill, 14, 0)}${text(x + w / 2, y + 19, label, 13, color, 700, font, 'text-anchor="middle"')}`;
}

// Figure 1 — actual repository layout, omitting ignored deployment artifacts.
{
  const w = 1700, h = 1110;
  let s = base(w, h, 'Cấu trúc tổng quan của project CookingBaby', 'Bản đồ repository hiện tại — không gồm thư mục build và artifact triển khai bị Git bỏ qua', 1);
  s += rect(52, 176, 1076, 864, C.white, C.line, 18);
  s += text(82, 218, 'Recipe-Sharing-Website-Test-/', 23, C.ink, 700, mono);
  s += line(82, 237, 1096, 237, C.line, 1.2);
  const rows = [
    [0, 'CookingBaby.sln', 'Solution Visual Studio'],
    [0, 'README.md', 'Hướng dẫn cài đặt và kết nối SQL Server'],
    [0, 'Database/SQLQueryckb.sql', 'Script schema, dữ liệu mẫu, views và stored procedures'],
    [0, 'packages/', '15 gói NuGet: MVC 5, EF 6, Bootstrap, jQuery…'],
    [0, 'CookingBaby/', 'Ứng dụng ASP.NET MVC 5 · .NET Framework 4.8'],
    [1, 'App_Start/', 'RouteConfig, BundleConfig, FilterConfig'],
    [1, 'Areas/Admin/', 'Khu vực quản trị · Dashboard và Users'],
    [1, 'Controllers/', 'Account, Categories, Home, Recipe, Stats'],
    [1, 'Infrastructure/', 'SessionAuthorizeAttribute'],
    [1, 'Models/', 'EDMX/EF Database First, entity, metadata, ViewModels'],
    [1, 'Services/', 'AccountService, PasswordHasher, RecipeQueryService'],
    [1, 'Views/', 'Razor views: Account, Categories, Home, Recipe, Shared'],
    [1, 'Content/', 'CSS, Bootstrap và ảnh món ăn / logo'],
    [1, 'Scripts/', 'cookingbaby.js, jQuery, validation, Bootstrap'],
    [1, 'fonts/', 'Glyphicons'],
    [1, 'Global.asax(.cs)', 'Đăng ký Area, route, filter và bundle'],
    [1, 'Web.config', 'Cấu hình ASP.NET và entity connection string'],
    [1, 'CookingBaby.csproj', 'Danh sách source, assets và reference'],
  ];
  let y = 275;
  for (let i = 0; i < rows.length; i++, y += 40) {
    const [depth, name, desc] = rows[i];
    if (i % 2 === 0) s += `<rect x="70" y="${y - 24}" width="1038" height="34" rx="6" fill="#f8fafc"/>`;
    const px = 91 + depth * 30;
    s += text(px, y, depth ? '└─' : '├─', 18, depth ? C.muted : C.blue, 500, mono);
    s += text(px + 30, y, name, 17, depth ? (name.endsWith('/') ? C.teal : C.slate) : C.blue, depth ? 600 : 700, mono);
    s += text(690, y, desc, 16, C.muted, 400);
  }
  s += rect(1160, 176, 488, 864, '#fdfefe', C.line, 18);
  s += text(1194, 220, 'Thông tin kỹ thuật', 22, C.ink, 700);
  const facts = [
    ['Web framework', 'ASP.NET MVC 5.2.9'],
    ['Runtime', '.NET Framework 4.8'],
    ['Data access', 'Entity Framework 6.5.1'],
    ['Mô hình dữ liệu', 'Database First · EDMX'],
    ['View engine', 'Razor'],
    ['Database', 'SQL Server · CookingBabyDB'],
  ];
  let fy = 276;
  for (const [k, v] of facts) {
    s += text(1194, fy, k, 14, C.muted, 600);
    s += text(1194, fy + 25, v, 18, C.ink, 700);
    s += line(1194, fy + 42, 1610, fy + 42, '#e7edf3', 1);
    fy += 77;
  }
  s += sectionLabel(1194, 760, 'CSDL hiện tại', C.teal);
  s += text(1194, 787, '8 bảng · 3 views · 4 stored procedures', 15, C.ink, 600);
  s += sectionLabel(1194, 844, 'Các tầng ứng dụng', C.purple);
  s += pill(1194, 870, 'Controller', C.blueSoft, C.blue, 104);
  s += pill(1310, 870, 'Model / EF6', C.purpleSoft, C.purple, 120);
  s += pill(1442, 870, 'Razor View', C.roseSoft, C.rose, 112);
  s += rect(1194, 930, 416, 78, C.goldSoft, '#efdcae', 12);
  s += text(1212, 960, 'Gói publish, file ZIP và script Somee được lưu', 14, C.gold, 600);
  s += text(1212, 984, 'trong artifacts/ và không nằm trong Git.', 14, C.gold, 600);
  s += text(52, 1080, 'Nguồn: cấu trúc thư mục và CookingBaby.csproj trong repository.', 13, C.muted, 400);
  save('01-project-structure.svg', s);
}

// Figure 2 — three MVC layers mapped to real source files.
{
  const w = 1700, h = 1120;
  let s = base(w, h, 'Chi tiết ba tầng Controllers, Models và Views', 'Tên thư mục và file được đối chiếu trực tiếp với project hiện tại', 2);
  const x0 = 52, gap = 20, cw = 518, top = 176, ch = 862;
  const xs = [x0, x0 + cw + gap, x0 + 2 * (cw + gap)];
  s += card(xs[0], top, cw, ch, 'Controllers', 'NHẬN REQUEST · KIỂM TRA · GỌI NGHIỆP VỤ', C.blue);
  s += card(xs[1], top, cw, ch, 'Models & Services', 'DỮ LIỆU · VIEWMODEL · NGHIỆP VỤ', C.purple);
  s += card(xs[2], top, cw, ch, 'Views (Razor)', 'GIAO DIỆN HTML ĐƯỢC RENDER TỪ .CSHTML', C.rose);
  const drawRows = (x, y, rows, rowH = 30, nameW = 232) => {
    let out = '';
    for (const r of rows) {
      if (r.type === 'section') {
        out += sectionLabel(x + 22, y + 2, r.label, r.color || C.blue);
        y += r.after || 24;
      } else {
        out += text(x + 26, y, r.name, r.nameSize || 15, r.color || C.ink, 600, mono);
        out += text(x + nameW, y, r.desc, 14, C.muted, 400);
        y += rowH;
      }
    }
    return out;
  };
  s += drawRows(xs[0], 266, [
    {type:'section',label:'Controllers/'},
    {name:'AccountController.cs',desc:'Đăng nhập / đăng ký'},
    {name:'CategoriesController.cs',desc:'CRUD danh mục'},
    {name:'HomeController.cs',desc:'Trang chủ · chi tiết · đánh giá'},
    {name:'RecipeController.cs',desc:'CRUD món · lưu · nhận xét'},
    {name:'StatsController.cs',desc:'Thống kê'},
    {type:'section',label:'Areas/Admin/Controllers/',color:C.orange,after:28},
    {name:'DashboardController.cs',desc:'Duyệt món · dashboard',color:C.orange},
    {name:'UsersController.cs',desc:'Quản lý tài khoản',color:C.orange},
    {type:'section',label:'Infrastructure/',color:C.teal,after:28},
    {name:'SessionAuthorizeAttribute.cs',desc:'Bảo vệ route theo session',color:C.teal,nameSize:13},
  ], 38, 259);
  s += drawRows(xs[1], 266, [
    {type:'section',label:'Models/ · EF Database First'},
    {name:'CookingBabyDB.edmx',desc:'Mô hình CSDL'},
    {name:'CookingBabyDB.Context.cs',desc:'DbContext / DbSet'},
    {name:'CookingBabyDB.cs',desc:'Entity classes sinh từ EDMX'},
    {type:'section',label:'Entity classes',color:C.teal},
    {name:'User · Category · Recipe',desc:'Tài khoản / món / danh mục',color:C.teal},
    {name:'RecipeIngredient · RecipeStep',desc:'Nguyên liệu / các bước',color:C.teal,nameSize:13},
    {name:'Rating · Comment · SavedRecipe',desc:'Đánh giá / bình luận / lưu',color:C.teal},
    {type:'section',label:'ViewModels & validation',color:C.orange},
    {name:'RecipeHomeViewModel.cs',desc:'Danh sách trang chủ',color:C.orange},
    {name:'RecipeCardViewModel.cs',desc:'Thẻ công thức',color:C.orange},
    {name:'RecipeQueryModels.cs',desc:'Điều kiện truy vấn',color:C.orange},
    {name:'RecipeReviewViewModel.cs',desc:'Đánh giá & bình luận',color:C.orange},
    {name:'CookingBabyMetadata.cs',desc:'Metadata validation',color:C.orange},
    {type:'section',label:'Services/',color:C.purple},
    {name:'RecipeQueryService.cs',desc:'Tìm kiếm / lọc',color:C.purple},
    {name:'AccountService.cs',desc:'+ PasswordHasher.cs',color:C.purple},
  ], 34, 280);
  s += drawRows(xs[2], 266, [
    {type:'section',label:'Account/'},
    {name:'Login.cshtml · SignUp.cshtml',desc:'Đăng nhập / tạo tài khoản'},
    {name:'AccessDenied.cshtml',desc:'Thông báo thiếu quyền'},
    {type:'section',label:'Home/'},
    {name:'Index.cshtml',desc:'Khám phá và lọc công thức'},
    {name:'About.cshtml · Contact.cshtml',desc:'Giới thiệu / liên hệ'},
    {type:'section',label:'Categories/'},
    {name:'Index / Details / Create',desc:'Danh sách / xem / thêm'},
    {name:'Edit / Delete',desc:'Sửa / xóa danh mục'},
    {type:'section',label:'Recipe/'},
    {name:'Create.cshtml',desc:'Tạo công thức'},
    {name:'Details.cshtml',desc:'Chi tiết món + đánh giá'},
    {name:'EditRecipe.cshtml',desc:'Sửa công thức'},
    {name:'MyRecipes.cshtml · SavedRecipes.cshtml',desc:'Món của tôi / đã lưu'},
    {type:'section',label:'Shared/'},
    {name:'_Layout.cshtml',desc:'Khung trang chung'},
    {name:'_NavbarTabs.cshtml',desc:'Điều hướng'},
    {name:'_CreateRecipeModal.cshtml',desc:'Popup tạo công thức'},
    {name:'_RecipeDetailModal.cshtml',desc:'Popup chi tiết + đánh giá',nameSize:13},
    {type:'section',label:'Areas/Admin/Views/',color:C.orange},
    {name:'Dashboard/Index.cshtml · Users/Index.cshtml',desc:'Quản trị dashboard / users',color:C.orange},
  ], 32, 280);
  s += rect(52, 1055, 1594, 34, '#eaf1f7', '#d5e2ed', 10);
  s += text(72, 1078, 'Luồng MVC: Controller chọn dữ liệu từ Model/Service rồi trả về View phù hợp.', 15, C.slate, 600);
  save('02-mvc-layers.svg', s);
}

// Figure 3 — MVC / EF6 request and response flow.
{
  const w = 1700, h = 1050;
  let s = base(w, h, 'Kiến trúc ASP.NET MVC 5 + Entity Framework 6', 'CookingBaby chạy trên .NET Framework 4.8; truy cập dữ liệu theo mô hình Database First', 3);
  // outer application boundary and browser / database
  s += rect(338, 182, 900, 730, '#fbfdff', '#bfd1df', 24, 2);
  s += pill(370, 158, 'ỨNG DỤNG COOKINGBABY', C.blue, C.white, 250);
  s += text(635, 179, 'ASP.NET MVC 5 · Razor · .NET Framework 4.8', 15, C.muted, 600);
  // connectors behind nodes
  s += pathEl('M292 510 H326 V320 H380', C.blue, 3);
  s += pathEl('M600 320 H670', C.blue, 3);
  s += pathEl('M910 320 H980', C.purple, 3);
  s += pathEl('M790 420 V500', C.teal, 2.5);
  s += pathEl('M600 620 H670', C.teal, 2.5);
  s += pathEl('M910 620 H980', C.teal, 3);
  s += pathEl('M1220 620 H1285', C.orange, 3);
  s += pathEl('M1095 235 V160 H166 V402', C.rose, 2.5, true);
  // Browser node
  s += rect(52, 402, 240, 220, C.white, '#aac5d8', 20, 2);
  s += rect(52, 402, 240, 54, C.blue, C.blue, 20, 0);
  s += `<rect x="52" y="439" width="240" height="17" fill="${C.blue}"/>`;
  s += text(76, 435, 'Trình duyệt', 21, C.white, 700);
  s += text(76, 492, 'HTML · CSS · JavaScript', 16, C.ink, 600);
  s += text(76, 523, 'CookingBaby UI', 15, C.muted, 400);
  s += text(76, 566, 'Gửi HTTP request', 14, C.blue, 700);
  s += text(76, 592, 'Nhận HTML response', 14, C.rose, 700);
  // Router
  s += rect(380, 250, 220, 140, C.goldSoft, '#e2c779', 18, 1.6);
  s += text(402, 283, '1 · Định tuyến', 18, C.gold, 700);
  s += text(402, 316, 'RouteConfig.cs', 17, C.ink, 700, mono);
  s += text(402, 345, '{controller}/{action}/{id}', 14, C.muted, 500, mono);
  s += text(402, 371, 'Mặc định: Home / Index', 13, C.muted, 500);
  // Controllers
  s += rect(670, 226, 240, 194, C.blueSoft, '#b5d2e5', 18, 1.6);
  s += text(692, 259, '2 · Controllers', 19, C.blue, 700);
  s += text(692, 292, 'Home · Recipe · Categories', 14, C.ink, 600);
  s += text(692, 319, 'Account · Stats', 14, C.ink, 600);
  s += text(692, 346, 'Admin: Dashboard · Users', 14, C.orange, 600);
  s += text(692, 389, 'Xử lý request, quyền và luồng nghiệp vụ', 12, C.muted, 500);
  // Views
  s += rect(980, 226, 240, 194, C.roseSoft, '#e6bed0', 18, 1.6);
  s += text(1002, 259, '6 · Razor Views', 19, C.rose, 700);
  s += text(1002, 292, 'Views/{Controller}/*.cshtml', 14, C.ink, 600, mono);
  s += text(1002, 321, 'Shared layout + modal', 14, C.ink, 600);
  s += text(1002, 350, 'Render HTML từ Model/ViewModel', 13, C.muted, 500);
  s += text(1002, 392, 'Trả response về trình duyệt', 13, C.rose, 700);
  // Services
  s += rect(380, 500, 220, 242, C.tealSoft, '#aed8ce', 18, 1.6);
  s += text(402, 535, '3 · Nghiệp vụ', 19, C.teal, 700);
  s += text(402, 572, 'RecipeQueryService', 14, C.ink, 600, mono);
  s += text(402, 602, 'AccountService', 14, C.ink, 600, mono);
  s += text(402, 632, 'PasswordHasher', 14, C.ink, 600, mono);
  s += text(402, 662, 'SessionAuthorizeAttribute', 13, C.ink, 600, mono);
  s += text(402, 708, 'Tìm kiếm · mật khẩu · session', 12, C.muted, 500);
  // Models
  s += rect(670, 500, 240, 242, C.purpleSoft, '#d0c4e7', 18, 1.6);
  s += text(692, 535, '4 · Models', 19, C.purple, 700);
  s += text(692, 572, 'Entity classes từ EDMX', 14, C.ink, 600);
  s += text(692, 602, 'ViewModels + Metadata', 14, C.ink, 600);
  s += text(692, 632, 'CookingBabyDBEntities', 13, C.ink, 600, mono);
  s += text(692, 666, 'User · Recipe · Category…', 13, C.muted, 500);
  s += text(692, 710, 'Model chuyển dữ liệu giữa các tầng', 12, C.muted, 500);
  // EF
  s += rect(980, 500, 240, 242, '#eef4f8', '#becfdb', 18, 1.6);
  s += text(1002, 535, '5 · Data access', 19, C.slate, 700);
  s += text(1002, 572, 'Entity Framework 6', 15, C.ink, 700);
  s += text(1002, 602, 'Database First · .edmx', 14, C.ink, 600);
  s += text(1002, 632, 'DbContext / DbSet', 14, C.ink, 600, mono);
  s += text(1002, 675, 'LINQ + SQL Server provider', 13, C.muted, 500);
  // SQL Server outside app
  s += rect(1285, 500, 360, 242, C.orangeSoft, '#e9c5a0', 18, 1.8);
  s += text(1310, 538, 'SQL Server', 21, C.orange, 700);
  s += text(1310, 576, 'CookingBabyDB', 18, C.ink, 700, mono);
  s += text(1310, 614, '8 bảng quan hệ', 15, C.ink, 600);
  s += text(1310, 644, '3 views · 4 stored procedures', 14, C.ink, 600);
  s += text(1310, 698, 'Dữ liệu tài khoản và công thức', 13, C.muted, 500);
  // flow labels
  s += pill(308, 432, 'HTTP', '#ffffff', C.blue, 62);
  s += pill(612, 276, 'Route', '#ffffff', C.blue, 54);
  s += pill(922, 276, 'View', '#ffffff', C.purple, 54);
  s += pill(1228, 574, 'SQL', '#ffffff', C.orange, 46);
  s += text(542, 153, '7 · HTML response', 13, C.rose, 700);
  s += rect(380, 795, 1265, 76, '#f1f5f9', '#d6e0e8', 14);
  s += text(405, 827, 'Luồng dữ liệu:', 15, C.slate, 700);
  s += text(520, 827, 'HTTP → Route → Controller → Service / Model → EF6 → SQL Server → Razor View → HTML', 15, C.slate, 600);
  s += text(405, 852, 'Đăng nhập lưu trạng thái trong Session; SessionAuthorizeAttribute bảo vệ các action yêu cầu xác thực.', 13, C.muted, 500);
  save('03-mvc-ef6-architecture.svg', s);
}

// Figure 4 — database ERD drawn from SQLQueryckb.sql.
{
  const w = 1900, h = 1330;
  let s = base(w, h, 'Sơ đồ ERD của cơ sở dữ liệu CookingBabyDB', '8 bảng · 11 khóa ngoại · đánh giá sao và bình luận là hai dữ liệu độc lập', 4);
  const fields = [];
  const table = (x, y, width, title, sub, color, rows) => {
    const rowH = 25, headH = 53, height = headH + rows.length * rowH + 9;
    fields.push(rect(x, y, width, height, C.white, C.line, 12, 1.4));
    fields.push(rect(x, y, width, headH, color, color, 12, 0));
    fields.push(`<rect x="${x}" y="${y + headH - 12}" width="${width}" height="12" fill="${color}"/>`);
    fields.push(text(x + 15, y + 24, title, 17, C.white, 700, mono));
    fields.push(text(x + 15, y + 43, sub, 11, '#ffffff', 500));
    rows.forEach((r, i) => {
      const ry = y + headH + i * rowH;
      if (i % 2 === 1) fields.push(`<rect x="${x + 1}" y="${ry}" width="${width - 2}" height="${rowH}" fill="#f7f9fb"/>`);
      const badgeW = r.key === 'PK' ? 29 : r.key === 'FK' ? 29 : r.key === 'UQ' ? 31 : r.key === 'PK, FK' ? 54 : 0;
      if (r.key) {
        const colorKey = r.key.includes('PK') ? C.blue : r.key === 'FK' ? C.teal : C.gold;
        fields.push(rect(x + 9, ry + 4, badgeW, 17, colorKey, colorKey, 4, 0));
        fields.push(text(x + 9 + badgeW / 2, ry + 17, r.key, 10, C.white, 700, font, 'text-anchor="middle"'));
      }
      fields.push(text(x + (r.key ? 48 : 12), ry + 17, r.name, 12, C.ink, r.key ? 600 : 500, mono));
      fields.push(text(x + width - 12, ry + 17, r.type, 11, C.muted, 500, mono, 'text-anchor="end"'));
    });
    return {x, y, width, height};
  };
  // Relationships are drawn behind table cards.
  s += `<g fill="none" stroke="#71879a" stroke-width="2.2" marker-end="url(#arrow)" stroke-linecap="round" stroke-linejoin="round">
    <path d="M420 340 C495 340 525 500 610 520"/>
    <path d="M420 500 C510 500 525 680 610 700"/>
    <path d="M420 290 H500 V125 H1280 V218 H1310"/>
    <path d="M420 410 H1310"/>
    <path d="M380 597 V780 H1260 V590 H1310"/>
    <path d="M420 935 C505 935 530 830 610 785"/>
    <path d="M1080 515 C1180 470 1205 265 1310 235"/>
    <path d="M1080 555 C1170 540 1210 500 1310 500"/>
    <path d="M1080 595 C1190 620 1210 655 1310 660"/>
    <path d="M1080 635 C1180 700 1200 825 1310 835"/>
    <path d="M1080 675 C1185 780 1210 1050 1310 1070"/>
  </g>`;
  // relationship labels placed in open space
  s += text(466, 330, 'AuthorId · 1:N', 11, C.muted, 600);
  s += text(468, 510, 'ApprovedBy · 1:N', 11, C.muted, 600);
  s += text(815, 118, 'UserId · 1:N', 11, C.muted, 600);
  s += text(760, 398, 'UserId · 1:N', 11, C.muted, 600);
  s += text(770, 773, 'UserId · 1:N', 11, C.muted, 600);
  s += text(1089, 461, 'RecipeId', 11, C.muted, 600);
  s += text(1150, 547, 'RecipeId', 11, C.muted, 600);
  s += text(1130, 622, 'RecipeId', 11, C.muted, 600);
  s += text(1145, 718, 'RecipeId', 11, C.muted, 600);
  s += text(1144, 848, 'RecipeId', 11, C.muted, 600);
  // cards
  const cards = [];
  cards.push(table(50, 235, 370, 'Users', 'TÀI KHOẢN · AUTHENTICATION', C.blue, [
    {key:'PK',name:'UserId',type:'int IDENTITY'}, {key:'UQ',name:'Username',type:'nvarchar(50)'},
    {name:'DisplayName',type:'nvarchar(100)'}, {key:'UQ',name:'Email',type:'nvarchar(150)'},
    {name:'PasswordHash',type:'nvarchar(255)'}, {name:'Role',type:'nvarchar(20)'},
    {name:'IsActive',type:'bit'}, {name:'CreatedAt',type:'datetime2'},
    {name:'LastLoginAt',type:'datetime2 NULL'}, {name:'FailedLoginCount',type:'int'},
    {name:'LockoutEnd',type:'datetime2 NULL'}, {name:'PasswordChangedAt',type:'datetime2 NULL'}
  ]));
  cards.push(table(50, 850, 370, 'Categories', 'DANH MỤC CÔNG THỨC', C.orange, [
    {key:'PK',name:'CategoryId',type:'int IDENTITY'}, {key:'UQ',name:'CategoryName',type:'nvarchar(100)'},
    {name:'ThemeColor',type:'char(7) NULL'}, {name:'DisplayOrder',type:'tinyint'}
  ]));
  cards.push(table(610, 440, 470, 'Recipes', 'CÔNG THỨC · APPROVAL WORKFLOW', C.teal, [
    {key:'PK',name:'RecipeId',type:'int IDENTITY'}, {name:'Title',type:'nvarchar(200)'},
    {name:'Description',type:'nvarchar(500)'}, {name:'CookTime',type:'smallint'},
    {name:'ImageUrl',type:'nvarchar(500) NULL'}, {key:'FK',name:'CategoryId',type:'int'},
    {key:'FK',name:'AuthorId',type:'int'}, {name:'Status',type:'nvarchar(20)'},
    {key:'FK',name:'ApprovedBy',type:'int NULL'}, {name:'ApprovedAt',type:'datetime2 NULL'},
    {name:'CreatedAt',type:'datetime2'}, {name:'UpdatedAt',type:'datetime2 NULL'}
  ]));
  cards.push(table(1310, 145, 500, 'Ratings', 'ĐIỂM SAO · MỖI USER / RECIPE MỘT LƯỢT', C.rose, [
    {key:'PK',name:'RatingId',type:'int IDENTITY'}, {key:'FK',name:'RecipeId',type:'int'},
    {key:'FK',name:'UserId',type:'int'}, {name:'Stars',type:'tinyint · 1..5'}, {name:'CreatedAt',type:'datetime2'}
  ]));
  cards.push(table(1310, 360, 500, 'Comments', 'NỘI DUNG BÌNH LUẬN · CÓ THỂ BỎ TRỐNG KHI GỬI SAO', C.rose, [
    {key:'PK',name:'CommentId',type:'int IDENTITY'}, {key:'FK',name:'RecipeId',type:'int'},
    {key:'FK',name:'UserId',type:'int'}, {name:'Content',type:'nvarchar(1000)'}, {name:'CreatedAt',type:'datetime2'}
  ]));
  cards.push(table(1310, 575, 500, 'SavedRecipes', 'BẢNG LIÊN KẾT USER ↔ RECIPE', C.purple, [
    {key:'PK, FK',name:'UserId',type:'int'}, {key:'PK, FK',name:'RecipeId',type:'int'}, {name:'SavedAt',type:'datetime2'}
  ]));
  cards.push(table(1310, 760, 500, 'RecipeIngredients', 'NGUYÊN LIỆU THEO THỨ TỰ', C.teal, [
    {key:'PK',name:'IngredientId',type:'int IDENTITY'}, {key:'FK',name:'RecipeId',type:'int'},
    {name:'IngredientName',type:'nvarchar(100)'}, {name:'SortOrder',type:'tinyint'}
  ]));
  cards.push(table(1310, 965, 500, 'RecipeSteps', 'CÁC BƯỚC THỰC HIỆN THEO THỨ TỰ', C.teal, [
    {key:'PK',name:'StepId',type:'int IDENTITY'}, {key:'FK',name:'RecipeId',type:'int'},
    {name:'StepNumber',type:'tinyint'}, {name:'Instruction',type:'nvarchar(1000)'}
  ]));
  s += fields.join('');
  s += rect(610, 855, 650, 372, '#ffffff', C.line, 16, 1.4);
  s += text(636, 891, 'Quy tắc quan hệ', 20, C.ink, 700);
  s += sectionLabel(636, 934, 'Khóa ngoại', C.teal);
  s += text(654, 960, 'Recipe → Category; Recipe → User (Author / Approver).', 14, C.muted, 500);
  s += text(654, 984, 'Recipe → Ingredients, Steps, Ratings, Comments, SavedRecipes.', 14, C.muted, 500);
  s += sectionLabel(636, 1030, 'Đánh giá và bình luận độc lập', C.rose);
  s += text(654, 1056, 'Rating có thể không kèm Comment; mỗi người dùng chỉ có một', 14, C.muted, 500);
  s += text(654, 1080, 'Rating cho mỗi công thức. Comment cũng lưu độc lập.', 14, C.muted, 500);
  s += sectionLabel(636, 1126, 'Ràng buộc & xóa dây chuyền', C.orange);
  s += text(654, 1152, 'SavedRecipes có khóa chính ghép (UserId, RecipeId).', 14, C.muted, 500);
  s += text(654, 1176, 'Ingredient, Step, Rating, Comment, SavedRecipe cascade theo Recipe.', 14, C.muted, 500);
  s += rect(50, 1215, 1760, 58, '#eaf1f7', '#d5e2ed', 12);
  s += text(72, 1251, 'PK khóa chính  ·  FK khóa ngoại  ·  UQ duy nhất  ·  1:N một-nhiều  ·  Các đường nối chỉ biểu diễn quan hệ khai báo trong SQLQueryckb.sql.', 14, C.slate, 600);
  save('04-cookingbabydb-erd.svg', s);
}

console.log(`Generated 4 SVG diagrams in ${outDir}`);
