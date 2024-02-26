using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RCBC.Models;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Web.Helpers;
using RCBC.Interface;
using System.Data;
using Dapper;
using System.Reflection;
using System.Threading;
using System.Data.Common;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using System.Text;
using System.Transactions;
using System;
using iTextSharp.text.pdf.qrcode;
using Microsoft.AspNetCore.Http.Extensions;

namespace RCBC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;
        private readonly string appSettingsPath;
        public int GlobalUserId { get; set; }

        public HomeController(IConfiguration _configuration, IGlobalRepository _global, IWebHostEnvironment environment)
        {
            Configuration = _configuration;
            global = _global;
            appSettingsPath = Path.Combine(environment.ContentRootPath, "appsettings.json");
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

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult PasswordReset()
        {
            return View();
        }

        public IActionResult ConfirmedReset(string Username)
        {
            using (var con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                var user = global.GetUserInformation().Where(x => x.Username == Username).FirstOrDefault();

                var finalString = global.GeneratePassword();

                string Salt = Crypto.GenerateSalt();
                string password = finalString + Salt;
                string HashPassword = Crypto.HashPassword(password);

                bool IsSuccess = global.SendEmail(finalString, user.EmployeeName, user.Email, "forgot");

                var parameters = new
                {
                    Id = user.Id,
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
                    IsFirstLogged = user.IsFirstLogged,
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
                    NewData = JsonConvert.SerializeObject(global.GetUserInformation().Where(x => x.Id == user.Id).FirstOrDefault()),
                    ModifiedBy = user.Id,
                    DateModified = DateTime.Now,
                    IP = global.GetLocalIPAddress(),
                };

                var logs = global.SaveAuditLogs(auditlogs);

            }
            return View("Index");
        }

        public IActionResult Dashboard()
        {
            return LoadViews();
        }

        public IActionResult FirstLogin()
        {
            return LoadViews();
        }

        public IActionResult ChangePassword()
        {
            return LoadViews();
        }

        public IActionResult ContinueLogin()
        {
            return LoadViews();
        }

        public IActionResult NoAccess()
        {
            return LoadViews();
        }

        public IActionResult LogoutAccount()
        {
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        public IActionResult Logout()
        {

            var auditlogs = new AuditLogsModel
            {
                Module = "Maintenance",
                SubModule = "Logout",
                ChildModule = "Logout",
                TableName = "UsersInformation",
                TableId = 0,
                Action = "Logout",
                PreviousData = string.Empty,
                NewData = string.Empty,
                ModifiedBy = Convert.ToInt32(Request.Cookies["UserId"]),
                DateModified = DateTime.Now,
                IP = global.GetLocalIPAddress(),
            };

            var logs = global.SaveAuditLogs(auditlogs);

            var parameters = new UserStatusModel
            {
                UserId = Convert.ToInt32(Request.Cookies["UserId"]),
                Status = false,
            };

            UpdateUserStatus(parameters);

            var param = new LoginAttemptModel
            {
                UserId = Convert.ToInt32(Request.Cookies["UserId"]),
                Attempt = 0,
            };

            global.UpdateLoginAttempt(param);

            RemoveCookies();

            return RedirectToAction("Index", "Home");
        }

        public void RemoveCookies()
        {
            Response.Cookies.Delete("Username");
            Response.Cookies.Delete("LastLogin");
            Response.Cookies.Delete("EmployeeName");
            Response.Cookies.Delete("UserRole");
            Response.Cookies.Delete("UserId");
        }

        public IActionResult LoginProceed()
        {
            int UserId = Convert.ToInt32(Request.Cookies["UserId"].ToString());

            if (UserId != 0)
            {
                var SubModules = global.GetSubModulesByUserId(UserId).FirstOrDefault();
                var ChildModules = global.GetChildModulesByUserId(UserId).FirstOrDefault();

                if (SubModules != null)
                {
                    string input = (SubModules != null && SubModules.SubModuleLink != null) ? SubModules.SubModuleLink : ChildModules.ChildModuleLink;
                    string[] Link = input.Split('/');

                    var user = global.GetUserInformation().Where(x => x.Id == UserId).FirstOrDefault();
                    ViewBag.DashboardDetails = global.GetDashboardDetails(user.GroupDept, user.UserRole);

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Maintenance",
                        SubModule = "Login",
                        ChildModule = "Login",
                        TableName = "UsersInformation",
                        TableId = 0,
                        Action = "Login",
                        PreviousData = string.Empty,
                        NewData = string.Empty,
                        ModifiedBy = user.Id,
                        DateModified = DateTime.Now,
                        IP = global.GetLocalIPAddress(),
                    };

                    var logs = global.SaveAuditLogs(auditlogs);

                    return RedirectToAction(Link[2], Link[1]);
                }
                else
                {
                    return RedirectToAction("NoAccess");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public void CreateCookies(UserModel user)
        {
            int sessionExpired = Convert.ToInt32(Configuration["SessionExpired"]);
            TimeSpan expirationTime = TimeSpan.FromMinutes(sessionExpired);

            DateTime expirationDate = DateTime.Now.Add(expirationTime);

            var cookieOptions = new CookieOptions
            {
                Expires = user.Id != 0 ? expirationDate : DateTime.Now,
                Secure = true,
                HttpOnly = true
            };

            if (user.Id != 0)
            {
                Response.Cookies.Append("Username", user.Username, cookieOptions);
                Response.Cookies.Append("LastLogin", DateTime.Now.ToString("dd MMMM yyyy hh:mm tt"), cookieOptions);
                Response.Cookies.Append("EmployeeName", user.EmployeeName, cookieOptions);
                Response.Cookies.Append("UserRole", user.UserRole, cookieOptions);
                Response.Cookies.Append("UserId", user.Id.ToString(), cookieOptions);
            }
        }

        [HttpGet("/ResetCookies")]
        public IActionResult ResetCookies()
        {
            bool Islogged = false;

            if (Convert.ToInt32(Request.Cookies["UserId"]) != 0)
            {
                var parameters = new UserStatusModel
                {
                    UserId = Convert.ToInt32(Request.Cookies["UserId"]),
                    Status = false,
                };

                UpdateUserStatus(parameters);

                RemoveCookies();

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now,
                    Secure = true,
                    HttpOnly = true
                };
                Islogged = true;
            }
            return Ok(Islogged);
        }

        [HttpGet("/GetTimeout")]
        public IActionResult GetTimeout()
        {
            int timeOut = 0;
            int milliseconds = 60000;
            int sessionExpired = Convert.ToInt32(Configuration["SessionExpired"]);
            timeOut = milliseconds * sessionExpired;

            if (Request.Cookies["Username"] != null)
            {
                UserModel user = new UserModel();
                user.Username = Request.Cookies["Username"];
                user.EmployeeName = Request.Cookies["EmployeeName"];
                user.UserRole = Request.Cookies["UserRole"];
                user.Id = Convert.ToInt32(Request.Cookies["UserId"]);
                CreateCookies(user);
            }

            return Ok(new { timeOut });
        }

        public void UpdateUserStatus(UserStatusModel model)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new UserStatusModel
                    {
                        UserId = model.UserId,
                        Status = model.Status,
                    };
                    con.Execute("sp_updateUserStatus", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public IActionResult Login(string Username, string Password)
        {
            var user = global.GetUserInformation().Where(x => x.Username == Username).FirstOrDefault();

            try
            {
                if (user != null)
                {
                    var chkStatus = global.CheckUserStatus(user.Id);

                    if (!chkStatus)
                    {
                        if (user.LoginAttempt >= 3)
                        {
                            return Json(new { success = false, action = "Index", controller = "Home", message = "User account has been locked." });
                        }
                        else
                        {
                            string PlainPass = Password + user.Salt;

                            bool result = Crypto.VerifyHashedPassword(user.HashPassword, PlainPass);

                            if (result)
                            {
                                var parameters = new UserStatusModel
                                {
                                    UserId = user.Id,
                                    Status = true,
                                };

                                UpdateUserStatus(parameters);

                                CreateCookies(user);

                                var SubModules = global.GetSubModulesByUserId(user.Id).FirstOrDefault();
                                var ChildModules = global.GetChildModulesByUserId(user.Id).FirstOrDefault();

                                if (user.IsFirstLogged == true)
                                {
                                    return Json(new { success = true, action = "FirstLogin", controller = "Home" });
                                }
                                else
                                {
                                    if (SubModules != null)
                                    {
                                        string input = (SubModules != null && SubModules.SubModuleLink != null) ? SubModules.SubModuleLink : ChildModules.ChildModuleLink;
                                        string[] Link = input.Split('/');

                                        var auditlogs = new AuditLogsModel
                                        {
                                            Module = "Maintenance",
                                            SubModule = "Login",
                                            ChildModule = "Login",
                                            TableName = "UsersInformation",
                                            TableId = 0,
                                            Action = "Login",
                                            PreviousData = string.Empty,
                                            NewData = string.Empty,
                                            ModifiedBy = user.Id,
                                            DateModified = DateTime.Now,
                                            IP = global.GetLocalIPAddress(),
                                        };

                                        var logs = global.SaveAuditLogs(auditlogs);

                                        var paramSuccess = new LoginAttemptModel
                                        {
                                            UserId = user.Id,
                                            Attempt = 0,
                                        };

                                        global.UpdateLoginAttempt(paramSuccess);

                                        return Json(new { success = true, action = Link[2], controller = Link[1] });
                                    }
                                    else
                                    {
                                        var paramSubmodules = new LoginAttemptModel
                                        {
                                            UserId = user.Id,
                                            Attempt = user.LoginAttempt + 1,
                                        };

                                        global.UpdateLoginAttempt(paramSubmodules);

                                        return Json(new { success = true, action = "Index", controller = "Home" });
                                    }
                                }
                            }
                            else
                            {
                                var param = new LoginAttemptModel
                                {
                                    UserId = user.Id,
                                    Attempt = user.LoginAttempt + 1,
                                };

                                global.UpdateLoginAttempt(param);

                                return Json(new { success = false, action = "Index", controller = "Home", message = "Invalid login attempt." });
                            }
                        }

                    }
                    else
                    {
                        return Json(new { success = true, action = "LogoutAccount", controller = "Home" });
                    }
                }
                else
                {
                    return Json(new { success = false, action = "Index", controller = "Home", message = "Username is not registered." });
                }

            }
            catch (Exception ex)
            {
                var paramCatch = new LoginAttemptModel
                {
                    UserId = user.Id,
                    Attempt = user.LoginAttempt + 1,
                };

                global.UpdateLoginAttempt(paramCatch);

                return Json(new { success = true, action = "Index", controller = "Home", message = ex.Message });
            }
        }

        public IActionResult SendResetPasswordLink(string UserID)
        {
            string originalUrl = HttpContext.Request.GetDisplayUrl();

            Uri originalUri = new Uri(originalUrl);
            UriBuilder newUriBuilder = new UriBuilder(originalUri)
            {
                Path = "/Home/PasswordReset"
            };

            string newUrl = newUriBuilder.Uri.ToString();

            var user = global.GetUserInformation().Where(x => x.Username == UserID).FirstOrDefault();

            if (user != null && !string.IsNullOrEmpty(newUrl))
            {
                bool IsSuccess = global.SendEmail(newUrl, user.EmployeeName, user.Email, "reset");
            }

            return RedirectToAction("Logout", "Home");
        }

        public IActionResult UpdatePassword(UpdatePasswordModel model)
        {
            GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;

            try
            {
                string? previousData = string.Empty;
                string salt = Empty.ToString();
                string OldPassword = Empty.ToString();
                int LoginAttempt = 0;
                bool result;

                var user = global.GetUserInformation().Where(x => x.Username == model.Username).FirstOrDefault();

                salt = user.Salt;
                OldPassword = user.HashPassword;
                LoginAttempt = user.LoginAttempt + 1;

                model.OldPassword = model.OldPassword + salt;
                result = Crypto.VerifyHashedPassword(OldPassword, model.OldPassword);

                if (result)
                {
                    bool accepted = global.IsStrongPassword(model.NewPassword);

                    if (accepted)
                    {
                        var finalString = new string(model.NewPassword);

                        string Salt = Crypto.GenerateSalt();
                        string password = finalString + Salt;
                        string HashPassword = Crypto.HashPassword(password);

                        //bool IsSuccess = global.SendEmail(finalString, user.Username, user.Email, "create");

                        using (var con = new SqlConnection(GetConnectionString()))
                        {
                            con.Open();
                            using (var transaction = con.BeginTransaction())
                            {
                                try
                                {
                                    var parameters = new
                                    {
                                        Id = user.Id,
                                        HashPassword = HashPassword,
                                        Salt = Salt,
                                        Username = user.Username,
                                        EmployeeName = user.EmployeeName,
                                        Email = user.Email,
                                        MobileNumber = user.MobileNumber,
                                        GroupDept = user.GroupDept,
                                        UserRole = user.UserRole,
                                        Active = user.Active,
                                        LoginAttempt = LoginAttempt,
                                        IsApproved = user.IsApproved,
                                        IsFirstLogged = false,
                                    };

                                    con.Execute("sp_updateUsersInformation", parameters, commandType: CommandType.StoredProcedure, transaction: transaction);

                                    previousData = JsonConvert.SerializeObject(user);

                                    transaction.Commit();

                                    con.Close();

                                    var auditlogs = new AuditLogsModel
                                    {
                                        Module = "Maintenance",
                                        SubModule = "Change Password",
                                        ChildModule = "Change Password",
                                        TableName = "UsersInformation",
                                        TableId = user.Id,
                                        Action = "Change Password",
                                        PreviousData = previousData,
                                        NewData = JsonConvert.SerializeObject(global.GetUserInformation().FirstOrDefault(x => x.Id == user.Id)),
                                        ModifiedBy = GlobalUserId,
                                        DateModified = DateTime.Now,
                                        IP = global.GetLocalIPAddress(),
                                    };

                                    var logs = global.SaveAuditLogs(auditlogs);

                                    return Json(new { success = true });
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "Error in changing password." });
                                }
                            }
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Weak Password." });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Old password is wrong!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error in changing password." });
            }
        }

        [HttpPost]
        public IActionResult UpdateTimeout(int newValue)
        {
            try
            {
                int timeout = newValue == 0 ? 1 : newValue;

                string json = System.IO.File.ReadAllText(appSettingsPath);

                var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                settings["SessionExpired"] = timeout;

                string updatedJson = JsonConvert.SerializeObject(settings);

                System.IO.File.WriteAllText(appSettingsPath, updatedJson);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult UpdateAllStatus()
        {
            try
            {
                var parameters = new UserStatusModel
                {
                    UserId = 0,
                    Status = false,
                };

                UpdateUserStatus(parameters);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    } //end
}