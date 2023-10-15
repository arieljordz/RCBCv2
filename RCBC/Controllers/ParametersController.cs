using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RCBC.Interface;

namespace RCBC.Controllers
{
    public class ParametersController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;

        public ParametersController(IConfiguration _configuration, IGlobalRepository _global)
        {
            Configuration = _configuration;
            global = _global;
        }
        private string GetConnectionString()
        {
            return Configuration.GetConnectionString("DefaultConnection");
        }
        public IActionResult LoadViews()
        {

            ViewBag.DateNow = DateTime.Now;
            ViewBag.Username = Request.Cookies["Username"];
            ViewBag.UserId = Request.Cookies["EmployeeName"];
            ViewBag.UserRole = Request.Cookies["UserRole"];

            string UserRole = Request.Cookies["UserRole"].ToString();

            ViewBag.Modules = global.GetModules(UserRole);
            ViewBag.SubModules = global.GetSubModules(UserRole);
            ViewBag.ChildModules = global.GetChildModules(UserRole);
            ViewBag.AccessModules = global.GetAccessModules();

            return View();
        }

        public IActionResult Parameters()
        {
            return LoadViews();
        }
    }
}
