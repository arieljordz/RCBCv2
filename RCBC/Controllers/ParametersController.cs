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
                int UserId = Convert.ToInt32(Request.Cookies["UserId"].ToString());

                ViewBag.Modules = global.GetModulesByUserId(UserId);
                ViewBag.SubModules = global.GetSubModulesByUserId(UserId);
                ViewBag.ChildModules = global.GetChildModulesByUserId(UserId);

                var UserRoles = global.GetUserRoles();
                ViewBag.cmbUserRoles = new SelectList(UserRoles, "UserRole", "UserRole");

                var Departments = global.GetDepartments();
                ViewBag.cmbDepartments = new SelectList(Departments, "GroupDept", "GroupDept");

                var EmailTypes = global.GetEmailTypes();
                ViewBag.cmbEmailTypes = new SelectList(EmailTypes, "EmailType", "EmailType");

                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Parameters()
        {
            return LoadViews();
        }
    }
}
