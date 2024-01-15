using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RCBC.Models;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Web.Helpers;
using RCBC.Interface;
using System.Reflection;
using Dapper;
using System.Transactions;
using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Sockets;
using System;
using static System.Collections.Specialized.BitVector32;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualBasic;

namespace RCBC.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;
        public int GlobalUserId { get; set; }

        public MaintenanceController(IConfiguration _configuration, IGlobalRepository _global)
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

                        var Accounts = global.GetAccounts().OrderBy(x => x.Id);
                        ViewBag.cmbAccounts = new SelectList(Accounts, "Id", "AccountNumber");

                        var CorporateNames = global.GetCorporateClient();
                        ViewBag.cmbCorporateNames = new SelectList(CorporateNames, "Id", "CorporateName");

                        var PartnerCodes = global.GetCorporateClient();
                        ViewBag.cmbPartnerCodes = new SelectList(PartnerCodes, "Id", "PartnerCode");

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

        public IActionResult ViewAllUsers()
        {
            return LoadViews();
        }

        public IActionResult CreateNewUser()
        {
            return LoadViews();
        }

        public IActionResult UserApproval()
        {
            return LoadViews();
        }

        public IActionResult ResetPassword()
        {
            return LoadViews();
        }

        public IActionResult UserAccess()
        {
            return LoadViews();
        }

        public IActionResult ForgotPassword()
        {
            return LoadViews();
        }

        public IActionResult CreateNewRole()
        {
            return LoadViews();
        }

        public IActionResult CreateNewDepartment()
        {
            return LoadViews();
        }

        public IActionResult ViewAllPartnerVendor()
        {
            return LoadViews();
        }

        public IActionResult AddNewPartnerVendor()
        {
            return LoadViews();
        }

        public IActionResult PartnerVendorApproval()
        {
            return LoadViews();
        }

        public IActionResult ViewAllPickupLocations()
        {
            return LoadViews();
        }

        public IActionResult AddNewPickupLocation()
        {
            return LoadViews();
        }

        public IActionResult PickupLocationApproval()
        {
            return LoadViews();
        }

        public IActionResult EmailTemplate()
        {
            return LoadViews();
        }

        public IActionResult SendForgotPassword(string Username)
        {
            string salt = string.Empty;
            string HashedPass = string.Empty;
            string PlainPass = string.Empty;
            bool result;
            string EmployeeName = string.Empty;
            int LoginAttempt = 0;
            string LastLogin = DateTime.Now.ToString("dd MMMM yyyy hh:mm tt");

            UserModel _userModel = new UserModel();

            using (IDbConnection con = new SqlConnection(GetConnectionString()))
            {
                var user = global.GetUserInformation().Where(x => x.Username == Username).FirstOrDefault();

                if (user != null)
                {
                    salt = user.Salt;
                    HashedPass = user.HashPassword;
                    EmployeeName = user.EmployeeName;
                    LoginAttempt = user.LoginAttempt;
                }

                PlainPass = PlainPass + salt;
                result = Crypto.VerifyHashedPassword(HashedPass, PlainPass);
            }

            Trace.WriteLine(result);

            if (result == true)
            {
                return View("Index");
            }
            else
            {
                return View("Index");
            }
        }

        public IActionResult Register(UserModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;

            try
            {
                string salt = Crypto.GenerateSalt();
                string password = "Pass1234." + salt;
                string hashedPassword = Crypto.HashPassword(password);
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;
                bool? active = null;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            if (model.ModuleIds != null && model.ChildModuleIds != null)
                            {

                                if (model.Id == 0)
                                {
                                    var usersInfoParameters = new
                                    {
                                        Username = model.Username.Replace("'", "''"),
                                        HashedPassword = hashedPassword,
                                        Salt = salt,
                                        EmployeeName = model.EmployeeName,
                                        Email = model.Email,
                                        MobileNumber = model.MobileNumber,
                                        GroupDept = model.GroupDept,
                                        UserRole = model.UserRole,
                                        UserStatus = false,
                                        DateCreated = DateTime.Now,
                                        LoginAttempt = 0,
                                        Deactivated = false,
                                        Active = true
                                    };

                                    model.Id = con.QuerySingle<int>("sp_saveUsersInformation", usersInfoParameters, commandType: CommandType.StoredProcedure, transaction: transaction);

                                    var allAccess = global.GetModulesAndSubModules();

                                    foreach (var item in allAccess)
                                    {
                                        int SubModuleId = Convert.ToInt32(item.SubModuleId);
                                        var Roles = global.GetUserRole().FirstOrDefault(x => x.UserRole == model.UserRole);
                                        var Modules = global.GetSubModule().FirstOrDefault(x => x.SubModuleId == SubModuleId);

                                        string[] moduleIdsArray = model.ModuleIds.Split(',');

                                        string[] childModuleIdsArray = model.ChildModuleIds.Split(',');

                                        bool isExistsModule = Array.Exists<string>(moduleIdsArray, x => x.Equals(SubModuleId.ToString()));
                                        bool isExistsChild = Array.Exists<string>(childModuleIdsArray, x => x.Equals(item.ChildModuleId.ToString()));

                                        bool? isActive = null;

                                        if (isExistsModule)
                                        {
                                            isActive = false;

                                            if (item.ChildModuleId != 0)
                                            {
                                                if (isExistsChild)
                                                {
                                                    isActive = false;
                                                }
                                                else
                                                {
                                                    isActive = null;
                                                }
                                            }
                                        }

                                        var insertParameters = new
                                        {
                                            UserId = model.Id,
                                            RoleId = Roles?.Id ?? 0,
                                            ModuleId = Modules?.ModuleId ?? 0,
                                            SubModuleId = SubModuleId,
                                            ChildModuleId = item.ChildModuleId,
                                            Active = isActive
                                        };

                                        con.Execute("sp_saveUserAccessModules", insertParameters, commandType: CommandType.StoredProcedure, transaction: transaction);

                                    }

                                    msg = "Successfully Saved.";
                                    action = "Add";
                                    previousData = null;
                                }
                                else
                                {
                                    var userInfo = global.GetUserInformation().FirstOrDefault(x => x.Id == model.Id);

                                    var usersInfoParameters = new
                                    {
                                        Id = model.Id,
                                        HashPassword = userInfo.HashPassword,
                                        Salt = userInfo.Salt,
                                        Username = model.Username,
                                        EmployeeName = model.EmployeeName,
                                        Email = model.Email,
                                        MobileNumber = model.MobileNumber,
                                        GroupDept = model.GroupDept,
                                        UserRole = model.UserRole,
                                        Active = model.Active,
                                        LoginAttempt = userInfo.LoginAttempt,
                                        IsApproved = userInfo.IsApproved,
                                    };

                                    con.Execute("sp_updateUsersInformation", usersInfoParameters, commandType: CommandType.StoredProcedure, transaction: transaction);

                                    var allAccess = global.GetModulesAndSubModules();

                                    var userAccesss = global.GetUserAccessModules().Where(x => x.UserId == model.Id).ToList();

                                    foreach (var item in allAccess)
                                    {
                                        int SubModuleId = Convert.ToInt32(item.SubModuleId);
                                        var Roles = global.GetUserRole().FirstOrDefault(x => x.UserRole == model.UserRole);
                                        var Modules = global.GetSubModule().FirstOrDefault(x => x.SubModuleId == SubModuleId);

                                        string[] moduleIdsArray = model.ModuleIds.Split(',');
                                        string[] childModuleIdsArray = model.ChildModuleIds.Split(',');

                                        bool isExistsModule = Array.Exists<string>(moduleIdsArray, x => x.Equals(SubModuleId.ToString()));
                                        bool isExistsChild = Array.Exists<string>(childModuleIdsArray, x => x.Equals(item.ChildModuleId.ToString()));

                                        bool? isActive = null;

                                        if (isExistsModule && userAccesss != null)
                                        {
                                            var access = userAccesss.Where(x => x.SubModuleId == SubModuleId && x.ChildModuleId == item.ChildModuleId).FirstOrDefault();
                                            if (access != null)
                                            {
                                                isActive = access.IsActive;

                                                if (access.IsActive == true)
                                                {
                                                    if (item.ChildModuleId != 0)
                                                    {
                                                        if (isExistsChild)
                                                        {
                                                            isActive = true;
                                                        }
                                                        else
                                                        {
                                                            isActive = null;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        isActive = true;
                                                    }
                                                }
                                                else
                                                {
                                                    if (item.ChildModuleId != 0)
                                                    {
                                                        if (isExistsChild)
                                                        {
                                                            isActive = false;
                                                        }
                                                        else
                                                        {
                                                            isActive = null;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        isActive = false;
                                                    }
                                                }
                                            }
                                        }

                                        var updateParameters = new
                                        {
                                            UserId = model.Id,
                                            RoleId = Roles?.Id ?? 0,
                                            ModuleId = Modules?.ModuleId ?? 0,
                                            SubModuleId = SubModuleId,
                                            ChildModuleId = item.ChildModuleId,
                                            Active = isActive
                                        };

                                        con.Execute("sp_updateUserAccessModules", updateParameters, commandType: CommandType.StoredProcedure, transaction: transaction);
                                    }

                                    msg = "Successfully Updated.";
                                    action = "Update";
                                    previousData = JsonConvert.SerializeObject(userInfo);
                                }

                                transaction.Commit();

                                var auditlogs = new AuditLogsModel
                                {
                                    Module = "Maintenance",
                                    SubModule = "User Maintenance",
                                    ChildModule = "View All Users",
                                    TableName = "UsersInformation",
                                    TableId = model.Id,
                                    Action = action,
                                    PreviousData = previousData,
                                    NewData = JsonConvert.SerializeObject(global.GetUserInformation().FirstOrDefault(x => x.Id == model.Id)),
                                    ModifiedBy = GlobalUserId,
                                    DateModified = DateTime.Now,
                                    IP = global.GetLocalIPAddress(),
                                };

                                var logs = global.SaveAuditLogs(auditlogs);

                                return Json(new { success = true, message = msg });

                            }
                            else
                            {
                                return Json(new { success = false, message = "Please select access for user." });
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { success = false, message = ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RegeneratePassword(int Id)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;

            try
            {
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    var user = global.GetUserInformation().Where(x => x.Id == Id).FirstOrDefault();

                    var random = new Random();
                    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789./<>";
                    var finalString = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());

                    string Salt = Crypto.GenerateSalt();
                    string password = finalString + Salt;
                    string HashPassword = Crypto.HashPassword(password);

                    string bodyMsg = "<head>" +
                                    "<style>" +
                                    "body{" +
                                    "font-family: calibri;" +
                                    "}" +
                                    "</style>" +
                                    "</head>" +
                                    "<body>" +
                                    "<p>Good Day!<br>" +
                                    "<br>" +
                                    "Username: " + user.Username + "<br>" +
                                    "New Password: <font color=red>" + finalString + "</font> <br>" +
                                    "<br>" +
                                    "<font color=red>*Note: This is a system generated e-mail.Please do not reply.</font>" +
                                    "</p>" +
                                    "</body>";

                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("arlene@yuna.somee.com");
                    mailMessage.To.Add("arlene.lunar11@gmail.com");
                    mailMessage.Subject = "Subject";
                    mailMessage.Body = bodyMsg;
                    mailMessage.IsBodyHtml = true;

                    SmtpClient smtpClient = new SmtpClient();
                    smtpClient.Host = "smtp.yuna.somee.com";
                    smtpClient.Port = 26;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("arlene@yuna.somee.com", "12345678");
                    smtpClient.EnableSsl = false;
                    smtpClient.Send(mailMessage);

                    var parameters = new
                    {
                        Id = Id,
                        HashPassword = HashPassword,
                        Salt = Salt,
                        Username = user.Username,
                        EmployeeName = user.EmployeeName,
                        Email = user.Email,
                        MobileNumber = user.MobileNumber,
                        GroupDept = user.GroupDept,
                        UserRole = user.UserRole,
                        Active = user.Active,
                        LoginAttempt = user.LoginAttempt,
                        IsApproved = user.IsApproved,
                    };

                    con.Execute("sp_updateUsersInformation", parameters, commandType: CommandType.StoredProcedure);

                    con.Close();

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = "User Maintenance",
                        ChildModule = "User Password Reset",
                        TableName = "UsersInformation",
                        Action = "Update",
                        PreviousData = JsonConvert.SerializeObject(user),
                        NewData = JsonConvert.SerializeObject(global.GetUserInformation().Where(x => x.Id == Id).FirstOrDefault()),
                        ModifiedBy = GlobalUserId,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return Json(new { success = true, password = finalString });
                }


            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadUsers()
        {
            var data = global.GetUserInformation().Where(x => x.Deactivated == false).OrderBy(x => x.Id).ToList();

            return Json(new { data });
        }

        public IActionResult UpdateUser(int Id)
        {
            try
            {
                var data = global.GetUserInformation().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemoveUser(int Id)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    con.Execute("sp_deleteUser", new { Id = Id }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult SaveUserRole(UserRoleModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;
            try
            {
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    var qry = global.GetUserRole().Where(x => x.Id == model.Id).FirstOrDefault();

                    if (model.Id == 0)
                    {
                        var parameters = new
                        {
                            UserRole = model.UserRole,
                            DateCreated = DateTime.Now,
                            CreatedBy = GlobalUserId,
                            DateApproved = DateTime.Now,
                            ApprovedBy = GlobalUserId,
                        };

                        model.Id = con.QuerySingle<int>("sp_saveUserRole", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        var parameters = new
                        {
                            Id = model.Id,
                            UserRole = model.UserRole,
                        };

                        con.Execute("sp_updateUserRole", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully updated.";
                        action = "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = "Role Maintenance",
                        ChildModule = "Create New Role",
                        TableName = "UserRole",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetUserRole().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = GlobalUserId,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return Json(new { success = true, message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdateUserRoles(int Id)
        {
            try
            {
                var data = global.GetUserRole().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemoveUserRole(int Id)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    con.Execute("sp_deleteUserRole", new { Id = Id }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadUserRoles()
        {
            try
            {
                var data = global.GetUserRole().OrderBy(x => x.Id).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult LoadUsersForApproval()
        {
            var data = global.GetUserInformation().OrderBy(x => x.Id).ToList();

            return Json(new { data });
        }

        public IActionResult SaveUserAccess(int userId, string[] moduleIds, string[] childModuleIds)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        string msg = string.Empty;
                        string? previousData = string.Empty;

                        var user = global.GetUserInformation().Where(x => x.Id == userId).FirstOrDefault();

                        if (moduleIds.Length > 0 && childModuleIds.Length > 0)
                        {
                            var allAccess = global.GetModulesAndSubModules();

                            var userAccesss = global.GetUserAccessModules().Where(x => x.UserId == userId).ToList();

                            foreach (var item in allAccess)
                            {
                                int SubModuleId = Convert.ToInt32(item.SubModuleId);
                                var Roles = global.GetUserRole().FirstOrDefault(x => x.UserRole == user.UserRole);
                                var Modules = global.GetSubModule().FirstOrDefault(x => x.SubModuleId == SubModuleId);

                                bool isExistsModule = Array.Exists<string>(moduleIds, x => x.Equals(SubModuleId.ToString()));
                                bool isExistsChild = Array.Exists<string>(childModuleIds, x => x.Equals(item.ChildModuleId.ToString()));

                                bool? isActive = null;

                                if (isExistsModule && userAccesss != null)
                                {
                                    var access = userAccesss.Where(x => x.SubModuleId == SubModuleId && x.ChildModuleId == item.ChildModuleId).FirstOrDefault();
                                    if (access != null)
                                    {
                                        isActive = access.IsActive;

                                        if (item.ChildModuleId != 0)
                                        {
                                            if (isExistsChild)
                                            {
                                                isActive = true;
                                            }
                                            else
                                            {
                                                isActive = null;
                                            }
                                        }
                                        else
                                        {
                                            isActive = true;
                                        }
                                    }
                                }

                                var updateParameters = new
                                {
                                    UserId = userId,
                                    RoleId = Roles?.Id ?? 0,
                                    ModuleId = Modules?.ModuleId ?? 0,
                                    SubModuleId = SubModuleId,
                                    ChildModuleId = item.ChildModuleId,
                                    Active = isActive
                                };

                                con.Execute("sp_updateUserAccessModules", updateParameters, commandType: CommandType.StoredProcedure, transaction: transaction);

                                msg = "Successfully saved.";

                                previousData = JsonConvert.SerializeObject(user);
                            }

                            var usersInfoParameters = new
                            {
                                Id = user.Id,
                                HashPassword = user.HashPassword,
                                Salt = user.Salt,
                                Username = user.Username,
                                EmployeeName = user.EmployeeName,
                                Email = user.Email,
                                MobileNumber = user.MobileNumber,
                                GroupDept = user.GroupDept,
                                UserRole = user.UserRole,
                                Active = user.Active,
                                LoginAttempt = user.LoginAttempt,
                                IsApproved = true,
                            };

                            con.Execute("sp_updateUsersInformation", usersInfoParameters, commandType: CommandType.StoredProcedure, transaction: transaction);

                            transaction.Commit();

                            var auditlogs = new AuditLogsModel
                            {
                                Module = "Maintenance",
                                SubModule = "User Maintenance",
                                ChildModule = "User Approval",
                                TableName = "UsersInformation",
                                TableId = user.Id,
                                Action = "Approved",
                                PreviousData = previousData,
                                NewData = JsonConvert.SerializeObject(global.GetUserInformation().FirstOrDefault(x => x.Id == user.Id)),
                                ModifiedBy = GlobalUserId,
                                DateModified = DateTime.Now,
                                IP = global.GetLocalIPAddress(),
                            };

                            var logs = global.SaveAuditLogs(auditlogs);

                            return Json(new { success = true, message = msg, userId = userId });
                        }
                        else
                        {
                            return Json(new { success = false, message = "No selected Modules.", userId = userId });
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = ex.Message, userId = userId });
                    }
                }
            }
        }

        public IActionResult SaveDepartment(DepartmentModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;
            try
            {
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;
                string? newData = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    var qry = global.GetDepartment().Where(x => x.Id == model.Id).FirstOrDefault();

                    if (model.Id == 0)
                    {
                        var parameters = new
                        {
                            GroupDept = model.GroupDept,
                            DateCreated = DateTime.Now,
                            CreatedBy = GlobalUserId,
                            DateApproved = DateTime.Now,
                            ApprovedBy = GlobalUserId,
                        };

                        model.Id = con.QuerySingle<int>("sp_saveDepartment", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        var parameters = new
                        {
                            Id = model.Id,
                            GroupDept = model.GroupDept,
                        };

                        con.Execute("sp_updateDepartment", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully updated.";
                        action = "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = "Group/Department",
                        ChildModule = "Create New Department",
                        TableName = "Department",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetDepartment().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = GlobalUserId,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return Json(new { success = true, message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdateDepartment(int Id)
        {
            try
            {
                var data = global.GetDepartment().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemoveDepartment(int Id)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    con.Execute("sp_deleteDepartment", new { Id = Id }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadDepartment()
        {
            try
            {
                var data = global.GetDepartment().OrderBy(x => x.Id).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult LoadUserAccessById(int UserId)
        {
            List<AccessModuleModel> data = new List<AccessModuleModel>();

            data = UserId == 0 ? data : global.GetUserAccessById(UserId).ToList();

            return Json(new { data });
        }

        public IActionResult LoadUserAccess()
        {
            var data = global.GetUserAccessById(0).ToList();

            return Json(new { data });
        }

        public IActionResult LoadPartnerVendors()
        {
            try
            {
                var data = global.GetPartnerVendor().OrderBy(x => x.Id).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SavePartnerVendor(PartnerVendorModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;
            try
            {
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;
                string? newData = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    var qry = global.GetPartnerVendor().Where(x => x.Id == model.Id).FirstOrDefault();

                    if (model.Id == 0)
                    {
                        var parameters = new
                        {
                            VendorName = model.VendorName,
                            VendorCode = model.VendorCode,
                            AssignedGL = model.AssignedGL,
                            Email = model.Email,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                            DateCreated = DateTime.Now,
                            CreatedBy = GlobalUserId,
                        };

                        model.Id = con.QuerySingle<int>("sp_savePartnerVendor", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        var parameters = new
                        {
                            Id = model.Id,
                            VendorName = model.VendorName,
                            VendorCode = model.VendorCode,
                            AssignedGL = model.AssignedGL,
                            Email = model.Email,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                            DateApproved = DateTime.Now,
                            ApprovedBy = GlobalUserId,
                        };

                        con.Execute("sp_updatePartnerVendor", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully updated.";
                        action = model.ForApproval ? "Approved" : "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = "Partner Vendor",
                        ChildModule = "Add New Partner Vendor",
                        TableName = "PartnerVendor",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetPartnerVendor().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = GlobalUserId,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return Json(new { success = true, message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdatePartnerVendor(int Id)
        {
            try
            {
                var data = global.GetPartnerVendor().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemovePartnerVendor(int Id)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    con.Execute("sp_deletePartnerVendor", new { Id = Id }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadPickupLocation()
        {
            try
            {
                var data = global.GetPickupLocation().OrderBy(x => x.Id).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SavePickupLocation(PickupLocationModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;
            try
            {
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;
                string? newData = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    var qry = global.GetPickupLocation().Where(x => x.Id == model.Id).FirstOrDefault();

                    var AccountNumber = global.GetAccounts().Where(x => x.Id == model.AccountNumberId).FirstOrDefault().AccountNumber;
                    var CorporateName = global.GetCorporateClient().Where(x => x.Id == model.CorporateNameId).FirstOrDefault().CorporateName;
                    var PartnerCode = global.GetCorporateClient().Where(x => x.Id == model.PartnerCodeId).FirstOrDefault().PartnerCode;

                    if (model.Id == 0)
                    {
                        var parameters = new
                        {
                            CorporateName = CorporateName,
                            Site = model.Site,
                            SiteAddress = model.SiteAddress,
                            PartnerCode = PartnerCode,
                            Location = model.Location,
                            SOLID = model.SOLID,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                            AccountNumber = AccountNumber,
                            AccountNumberId = model.AccountNumberId,
                            CorporateNameId = model.CorporateNameId,
                            PartnerCodeId = model.PartnerCodeId,
                            DateCreated = DateTime.Now,
                            CreatedBy = GlobalUserId,
                        };

                        model.Id = con.QuerySingle<int>("sp_savePickupLocation", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        var parameters = new
                        {
                            Id = model.Id,
                            CorporateName = CorporateName,
                            Site = model.Site,
                            SiteAddress = model.SiteAddress,
                            PartnerCode = PartnerCode,
                            Location = model.Location,
                            SOLID = model.SOLID,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                            AccountNumber = AccountNumber,
                            AccountNumberId = model.AccountNumberId,
                            CorporateNameId = model.CorporateNameId,
                            PartnerCodeId = model.PartnerCodeId,
                            DateApproved = DateTime.Now,
                            ApprovedBy = GlobalUserId,
                        };

                        con.Execute("sp_updatePickupLocation", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully updated.";
                        action = model.ForApproval ? "Approved" : "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = "Pickup Location",
                        ChildModule = "Add New Pickup Locations",
                        TableName = "PickupLocation",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetPickupLocation().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = GlobalUserId,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return Json(new { success = true, message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdatePickupLocation(int Id)
        {
            try
            {
                var data = global.GetPickupLocation().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemovePickupLocation(int Id)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    con.Execute("sp_deletePickupLocation", new { Id = Id }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadAccountDetails()
        {
            try
            {
                IEnumerable<AccountModel> data = new List<AccountModel>();

                List<CurrencyModel> currencies = new List<CurrencyModel>
                {
                new CurrencyModel { Id = 1, Currency = "PHP" },
                new CurrencyModel { Id = 2, Currency = "USD" },
                new CurrencyModel { Id = 3, Currency = "EUR" },
                new CurrencyModel { Id = 4, Currency = "JPY" },
                };

                List<AccountTypeModel> accountTypes = new List<AccountTypeModel>
                {
                new AccountTypeModel { Id = 1, AccountType = "CA" },
                new AccountTypeModel { Id = 2, AccountType = "SA" },
                };

                data = global.GetAccounts().Where(x => x.LocationId != 0).ToList();

                List<AccountModel> result = data.Select(account => new AccountModel
                {
                    Id = account.Id,
                    LocationId = account.LocationId,
                    AccountNumber = account.AccountNumber,
                    AccountName = account.AccountName,
                    Currency = currencies.FirstOrDefault(c => c.Id == account.CurrencyId)?.Currency,
                    AccountType = accountTypes.FirstOrDefault(a => a.Id == account.AccountTypeId)?.AccountType
                }).ToList();

                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveAccountDetails(PickupLocationModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;
            try
            {
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;
                string? newData = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    var qry = global.GetAccounts().Where(x => x.Id == model.Id).FirstOrDefault();

                    if (model.Id == 0)
                    {
                        var parameters = new
                        {
                            LocationId = model.LocationId,
                            CorporateClientId = 0,
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            CurrencyId = model.CurrencyId,
                            AccountTypeId = model.AccountTypeId,
                            DateCreated = DateTime.Now,
                            CreatedBy = GlobalUserId,
                            DateApproved = DateTime.Now,
                            ApprovedBy = GlobalUserId,
                            IsActive = true
                        };

                        model.Id = con.QuerySingle<int>("sp_saveAccount", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        var parameters = new
                        {
                            Id = model.Id,
                            LocationId = model.LocationId,
                            CorporateClientId = 0,
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            CurrencyId = model.CurrencyId,
                            AccountTypeId = model.AccountTypeId,
                        };

                        con.Execute("sp_updateAccount", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully updated.";
                        action = "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = model.LocationId == 0 ? "Corporate Management" : "Pickup Location",
                        ChildModule = model.LocationId == 0 ? "Create New Client" : "Add New Pickup Location",
                        TableName = "Accounts",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetAccounts().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = GlobalUserId,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return Json(new { success = true, message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdateAccountDetails(int Id)
        {
            try
            {
                var data = global.GetAccounts().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemoveAccountDetails(int Id)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    con.Execute("sp_deleteAccount", new { Id = Id }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadContactDetails()
        {
            try
            {
                var data = global.GetContacts().Where(x => x.LocationId != 0).OrderBy(x => x.Id).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveContactDetails(PickupLocationModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;
            try
            {
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;
                string? newData = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    var qry = global.GetContacts().Where(x => x.Id == model.Id).FirstOrDefault();

                    if (model.Id == 0)
                    {
                        var parameters = new
                        {
                            LocationId = model.LocationId,
                            CorporateClientId = 0,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                        };

                        model.Id = con.QuerySingle<int>("sp_saveContact", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        var contact = global.GetContacts().Where(x => x.Id == model.Id && x.LocationId == model.LocationId).FirstOrDefault();

                        if (contact != null)
                        {
                            var parameters = new
                            {
                                Id = model.Id,
                                LocationId = model.LocationId == 0 ? contact.LocationId : model.LocationId,
                                CorporateClientId = 0,
                                ContactPerson = model.ContactPerson == null ? contact.ContactPerson : model.ContactPerson,
                                Email = model.Email == null ? contact.Email : model.Email,
                                MobileNumber = model.MobileNumber == null ? contact.MobileNumber : model.MobileNumber,
                            };

                            con.Execute("sp_updateContact", parameters, commandType: CommandType.StoredProcedure);

                            msg = "Successfully updated.";
                            action = "Update";
                            previousData = JsonConvert.SerializeObject(qry);
                        }
                        else
                        {
                            var contacts = global.GetContacts().Where(x => x.Id == model.Id).FirstOrDefault();

                            var parameters = new
                            {
                                LocationId = model.LocationId == 0 ? contacts.LocationId : model.LocationId,
                                CorporateClientId = 0,
                                ContactPerson = model.ContactPerson == null ? contacts.ContactPerson : model.ContactPerson,
                                Email = model.Email == null ? contacts.Email : model.Email,
                                MobileNumber = model.MobileNumber == null ? contacts.MobileNumber : model.MobileNumber,
                            };

                            model.Id = con.QuerySingle<int>("sp_saveContact", parameters, commandType: CommandType.StoredProcedure);

                            msg = "Successfully saved.";
                            action = "Add";
                            previousData = null;
                        }
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = model.LocationId == 0 ? "Corporate Management" : "Pickup Location",
                        ChildModule = model.LocationId == 0 ? "Create New Client" : "Add New Pickup Location",
                        TableName = "Accounts",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetContacts().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = GlobalUserId,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return Json(new { success = true, message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdateContactDetails(int Id)
        {
            try
            {
                var data = global.GetContacts().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemoveContactDetails(int Id)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    con.Execute("sp_deleteContact", new { Id = Id }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult GetEmailType(string EmailType)
        {
            try
            {
                var data = global.GetEmailType().Where(x => x.EmailType == EmailType).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdateEmailType(string Subject, string Content, string EmailType)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    var parameters = new
                    {
                        EmailType = EmailType,
                        Subject = Subject,
                        Content = Content,
                    };

                    con.Execute("sp_updateEmailType", parameters, commandType: CommandType.StoredProcedure);

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadChangesDetails(int Id, string TableName)
        {
            try
            {
                List<DataChangesModel> data = new List<DataChangesModel>();

                data = global.GetChangesDetails(Id, TableName);

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    } //end

}
