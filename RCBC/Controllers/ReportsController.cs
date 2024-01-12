using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RCBC.Interface;
using System.Diagnostics;

namespace RCBC.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;

        public ReportsController(IConfiguration _configuration, IGlobalRepository _global)
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
                int UserId = Convert.ToInt32(Request.Cookies["UserId"].ToString());

                if (UserId != 0)
                {
                    var chkStatus = global.CheckUserStatus(UserId);

                    if (chkStatus)
                    {
                        ViewBag.Modules = global.GetModulesByUserId(UserId);
                        ViewBag.SubModules = global.GetSubModulesByUserId(UserId);
                        ViewBag.ChildModules = global.GetChildModulesByUserId(UserId);

                        var user = global.GetUserInformation().Where(x => x.Id == UserId).FirstOrDefault();
                        ViewBag.DashboardDetails = global.GetDashboardDetails(user.GroupDept);

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

        public IActionResult AuditLogs()
        {
            return LoadViews();
        }

        public IActionResult ElectronicJournal()
        {
            return LoadViews();
        }

        public IActionResult DPUStatus()
        {
            return LoadViews();
        }

        public IActionResult AuditTrailSummary()
        {
            return LoadViews();
        }

        public IActionResult LoadAuditLogs()
        {
            var data = global.GetAuditLogs().ToList();

            return Json(new { data });
        }

    }
}
