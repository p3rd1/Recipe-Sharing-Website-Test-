using System.Web.Mvc;
using CookingBaby.Models;
using CookingBaby.Services;

namespace CookingBaby.Controllers
{
    /// <summary>
    /// Các điểm cuối JSON để xem thống kê (mở trực tiếp trên trình duyệt):
    ///   /Stats/Overview     /Stats/Categories     /Stats/TopRated     /Stats/TopIngredients     /Stats/TopAuthors
    /// TODO: project chưa có đăng nhập; khi có thì giới hạn controller này cho Admin.
    /// </summary>
    public class StatsController : Controller
    {
        private readonly CookingBabyDBEntities db = new CookingBabyDBEntities();

        private RecipeQueryService Service
        {
            get { return new RecipeQueryService(db); }
        }

        public JsonResult Overview()
        {
            return Json(Service.GetOverview(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult Categories()
        {
            return Json(Service.GetCategoryStats(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult TopRated(int take = 5, int minRatings = 1)
        {
            return Json(Service.GetTopRated(take, minRatings), JsonRequestBehavior.AllowGet);
        }

        public JsonResult TopIngredients(int take = 10)
        {
            return Json(Service.GetPopularIngredientStats(take), JsonRequestBehavior.AllowGet);
        }

        public JsonResult TopAuthors(int take = 5)
        {
            return Json(Service.GetTopAuthors(take), JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
