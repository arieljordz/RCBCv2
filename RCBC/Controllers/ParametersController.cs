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

            if (Request.Cookies["Username"] != null)
            {
                string UserRole = Request.Cookies["UserRole"].ToString();

                ViewBag.Modules = global.GetModulesByRole(UserRole);
                ViewBag.SubModules = global.GetSubModulesByRole(UserRole);
                ViewBag.ChildModules = global.GetChildModulesRole(UserRole);
                ViewBag.ActiveAccess = global.GetActiveAccess(UserRole);

                var UserRoles = global.GetUserRoles();
                ViewBag.cmbUserRoles = new SelectList(UserRoles, "UserRole", "UserRole");

                var Departments = global.GetDepartments();
                ViewBag.cmbDepartments = new SelectList(Departments, "GroupDept", "GroupDept");

                return View();
            }
            else
            {
                return View("Signout");
            }
        }


        public IActionResult Parameters()
        {
            return LoadViews();
        }
    }
}
