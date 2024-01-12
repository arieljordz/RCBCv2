using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RCBC.Interface;

namespace RCBC.Controllers
{
    public class ParametersController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;
        public int GlobalUserId { get; set; }

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
            ViewBag.EmployeeName = Request.Cookies["EmployeeName"];
            ViewBag.UserRole = Request.Cookies["UserRole"];

            if (Request.Cookies["Username"] != null)
            {
                GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;

                if (GlobalUserId != 0)
                {
                    var chkStatus = global.CheckUserStatus(GlobalUserId);

                    if (chkStatus)
                    {
                        ViewBag.Modules = global.GetModulesByUserId(GlobalUserId);
                        ViewBag.SubModules = global.GetSubModulesByUserId(GlobalUserId);
                        ViewBag.ChildModules = global.GetChildModulesByUserId(GlobalUserId);

                        var user = global.GetUserInformation().Where(x => x.Id == GlobalUserId).FirstOrDefault();
                        ViewBag.Department = user.GroupDept;
                        ViewBag.DashboardDetails = global.GetDashboardDetails(user.GroupDept, user.UserRole);

                        var UserRoles = global.GetUserRole();
                        ViewBag.cmbUserRoles = new SelectList(UserRoles, "UserRole", "UserRole");

                        var Departments = global.GetDepartment();
                        ViewBag.cmbDepartments = new SelectList(Departments, "GroupDept", "GroupDept");

                        var EmailTypes = global.GetEmailType();
                        ViewBag.cmbEmailTypes = new SelectList(EmailTypes, "EmailType", "EmailType");

                        var Contacts = global.GetContacts().OrderBy(x => x.Id);
                        ViewBag.cmbContacts = new SelectList(Contacts, "Id", "ContactPerson");

                        var CorporateNames = global.GetCorporateClient();
                        ViewBag.cmbCorporateNames = new SelectList(CorporateNames, "CorporateName", "CorporateName");

                        var CorporateCodes = global.GetCorporateClient();
                        ViewBag.cmbCorporateCodes = new SelectList(CorporateCodes, "PartnerCode", "CorporateCode");

                        return View();
                    }
                    else
                    {
                        return RedirectToAction("LogoutAccount", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
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
