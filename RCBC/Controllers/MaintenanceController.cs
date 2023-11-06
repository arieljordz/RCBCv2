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

namespace RCBC.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;

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

                return View();
            }
            else
            {
                return View("_Signout");
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
                var user = con.QueryFirstOrDefault<UserModel>(@"SELECT * FROM [RCBC].[dbo].[UsersInformation] WHERE UserId = @Username", new { Username });

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
            try
            {
                string salt = Crypto.GenerateSalt();
                string password = "Pass1234." + salt;
                string hashedPassword = Crypto.HashPassword(password);
                string msg = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            if (model.Id == 0)
                            {
                                // Insert into UsersInformation table
                                string insertUsersInfoQuery = @"
                                    INSERT INTO [RCBC].[dbo].[UsersInformation] (UserId, HashPassword, Salt, EmployeeName, Email, MobileNumber, GroupDept, UserRole, UserStatus, DateAdded, LoginAttempt)
                                    OUTPUT INSERTED.Id
                                    VALUES(@UserId, @HashedPassword, @Salt, @EmployeeName, @Email, @MobileNumber, @GroupDept, @UserRole, @UserStatus, @DateAdded, @LoginAttempt)";

                                var usersInfoParameters = new
                                {
                                    UserId = model.UserId.Replace("'", "''"),
                                    HashedPassword = hashedPassword,
                                    Salt = salt,
                                    EmployeeName = model.EmployeeName,
                                    Email = model.Email,
                                    MobileNumber = model.MobileNumber,
                                    GroupDept = model.GroupDept,
                                    UserRole = model.UserRole,
                                    UserStatus = true,
                                    DateAdded = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"),
                                    LoginAttempt = 0
                                };

                                // Execute the query and retrieve the inserted Id
                                int insertedId = con.QuerySingleOrDefault<int>(insertUsersInfoQuery, usersInfoParameters, transaction);

                                if (model.ModuleIds != null)
                                {
                                    string[] moduleIdsArray = model.ModuleIds.Split(',');

                                    foreach (var Id in moduleIdsArray)
                                    {
                                        int SubModuleId = Convert.ToInt32(Id);
                                        var Roles = global.GetUserRoles().Where(x => x.UserRole == model.UserRole).FirstOrDefault();
                                        var Modules = global.GetAllSubModules().Where(x => x.SubModuleId == SubModuleId).FirstOrDefault();

                                        string insertQuery = @"
                                        INSERT INTO [RCBC].[dbo].[UserAccessModules] (UserId, RoleId, ModuleId, SubModuleId)
                                        VALUES(@UserId, @RoleId, @ModuleId, @SubModuleId)";

                                        var insertParameters = new
                                        {
                                            UserId = insertedId,
                                            RoleId = Roles?.Id ?? 0,
                                            ModuleId = Modules?.ModuleId ?? 0,
                                            SubModuleId = SubModuleId,
                                        };
                                        con.Execute(insertQuery, insertParameters, transaction);
                                    }
                                }
                                msg = "Successfully Saved.";
                            }
                            else
                            {
                                // Update user information
                                string updateUsersInfoQuery = @"
                                UPDATE [RCBC].[dbo].[UsersInformation]
                                SET EmployeeName = @EmployeeName, Email = @Email, MobileNumber = @MobileNumber, GroupDept = @GroupDept, UserRole = @UserRole
                                WHERE Id = @Id";

                                var usersInfoParameters = new
                                {
                                    Id = model.Id,
                                    EmployeeName = model.EmployeeName,
                                    Email = model.Email,
                                    MobileNumber = model.MobileNumber,
                                    GroupDept = model.GroupDept,
                                    UserRole = model.UserRole,
                                };

                                con.Execute(updateUsersInfoQuery, usersInfoParameters, transaction);

                                msg = "Successfully Updated.";
                            }

                            transaction.Commit();

                            return Json(new { success = true, message = msg });
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

        public IActionResult SearchUser(string Username)
        {
            try
            {
                UserModel _user;
                UserAccessModel _access;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    _user = con.QueryFirstOrDefault<UserModel>(@"SELECT * FROM [RCBC].[dbo].[UsersInformation] WHERE UserStatus = 1 AND UserId LIKE '%' + @Username + '%'", new { Username });

                    int UserId = _user != null ? _user.Id : 0;

                    _access = con.QueryFirstOrDefault<UserAccessModel>(@"SELECT * FROM [RCBC].[dbo].[UserAccessModules] WHERE UserId = @UserId", new { UserId });

                    con.Close();
                }

                if (_user != null)
                {
                    return Json(new { success = true, data = _user, access = _access });
                }
                else
                {
                    return Json(new { success = false, message = "User not found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RegeneratePassword(UserModel model)
        {
            try
            {
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
                                "User ID: " + model.UserId + "<br>" +
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
                Trace.WriteLine("Email Sent Successfully.");

                using (var con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string sql = "UPDATE [RCBC].[dbo].[UsersInformation] SET HashPassword = @HashPassword, Salt = @Salt WHERE UserId = @Username";
                    var parameters = new
                    {
                        HashPassword = HashPassword,
                        Salt = Salt,
                        Username = model.UserId
                    };
                    con.Execute(sql, parameters);

                    con.Close();
                }

                return Json(new { success = true, password = finalString });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadUsers()
        {
            List<UserModel> data;

            using (IDbConnection con = new SqlConnection(GetConnectionString()))
            {
                data = con.Query<UserModel>("SELECT * FROM [RCBC].[dbo].[UsersInformation] WHERE UserStatus = 1").ToList();
            }

            return Json(new { data });
        }

        public IActionResult UpdateUser(int Id)
        {
            try
            {
                UserModel data = new UserModel();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[UsersInformation] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<UserModel>(query, new { Id = Id });

                    con.Close();
                }

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
                UserModel data = new UserModel();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = "UPDATE [RCBC].[dbo].[UsersInformation] SET UserStatus = @UserStatus WHERE Id = @Id";
                    con.Execute(query, new { Id = Id, UserStatus = false });

                    con.Close();
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
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (model.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[UserRole] (UserRole)
                        VALUES(@UserRole)";

                        var parameters = new
                        {
                            UserRole = model.UserRole,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"UPDATE [RCBC].[dbo].[UserRole] SET UserRole = @UserRole WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            UserRole = model.UserRole,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                    }
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
                UserRoleModel data;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[UserRole] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<UserRoleModel>(query, new { Id = Id });
                }

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
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = "DELETE FROM [RCBC].[dbo].[UserRole] WHERE Id = @Id";
                    con.Execute(query, new { Id = Id });

                    con.Close();
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
                IEnumerable<UserRoleModel> data = new List<UserRoleModel>();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[UserRole]";
                    data = con.Query<UserRoleModel>(query);

                    con.Close();
                }

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveUserAccess(int userId, string userRole, int[] moduleIds)
        {
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        string msg = string.Empty;

                        if (moduleIds.Length > 0)
                        {
                            foreach (var SubModuleId in moduleIds)
                            {
                                var Roles = global.GetUserRoles().Where(x => x.UserRole == userRole).FirstOrDefault();
                                var Modules = global.GetAllSubModules().Where(x => x.SubModuleId == SubModuleId).FirstOrDefault();

                                var existingAccess = con.QueryFirstOrDefault<UserAccessModel>(
                                    @"SELECT * FROM [RCBC].[dbo].[UserAccessModules] WHERE UserId = @userId AND SubModuleId = @SubModuleId",
                                    new { userId, SubModuleId },
                                    transaction
                                );

                                if (existingAccess == null)
                                {
                                    string insertQuery = @"
                                        INSERT INTO [RCBC].[dbo].[UserAccessModules] (UserId, RoleId, ModuleId, SubModuleId, Active)
                                        VALUES(@UserId, @RoleId, @ModuleId, @SubModuleId, @Active)";

                                    var insertParameters = new
                                    {
                                        UserId = userId,
                                        RoleId = Roles?.Id ?? 0,
                                        ModuleId = Modules?.ModuleId ?? 0,
                                        SubModuleId = SubModuleId,
                                        Active = true,
                                    };

                                    con.Execute(insertQuery, insertParameters, transaction);
                                    msg = "Successfully saved.";
                                }
                                else
                                {
                                    string updateQuery = @"UPDATE [RCBC].[dbo].[UserAccessModules] SET Active = @Active, ModuleId = @ModuleId, SubModuleId = @SubModuleId WHERE SubModuleId = @SubModuleId AND UserId = @UserId";

                                    var updateParameters = new
                                    {
                                        UserId = userId,
                                        ModuleId = Modules?.ModuleId ?? 0,
                                        SubModuleId = SubModuleId,
                                        Active = true,
                                    };

                                    con.Execute(updateQuery, updateParameters, transaction);

                                    msg = "Successfully saved.";
                                }
                            }

                            string updateQueryFalse = @"UPDATE [RCBC].[dbo].[UserAccessModules] SET Active = @Active WHERE SubModuleId NOT IN @SubModuleIds AND UserId = @UserId";

                            var updateParametersFalse = new
                            {
                                UserId = userId,
                                Active = false,
                                SubModuleIds = moduleIds
                            };

                            con.Execute(updateQueryFalse, updateParametersFalse, transaction);

                            transaction.Commit();

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
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (model.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[Department] (GroupDept)
                        VALUES(@GroupDept)";

                        var parameters = new
                        {
                            GroupDept = model.GroupDept,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"UPDATE [RCBC].[dbo].[Department] SET GroupDept = @GroupDept WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            GroupDept = model.GroupDept,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                    }
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
                DepartmentModel data = new DepartmentModel();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[Department] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<DepartmentModel>(query, new { Id = Id });

                    con.Close();
                }

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
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = "DELETE FROM [RCBC].[dbo].[Department] WHERE Id = @Id";
                    con.Execute(query, new { Id = Id });

                    con.Close();
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
                IEnumerable<DepartmentModel> data = new List<DepartmentModel>();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[Department]";
                    data = con.Query<DepartmentModel>(query);

                    con.Close();
                }

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult LoadUserAccessById(int UserId)
        {
            List<AccessModuleModel> data;

            data = global.GetUserAccessById(UserId).ToList();

            return Json(new { data });
        }

        public IActionResult LoadPartnerVendors()
        {
            try
            {
                IEnumerable<PartnerVendorModel> data = new List<PartnerVendorModel>();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[PartnerVendor]";
                    data = con.Query<PartnerVendorModel>(query);

                    con.Close();
                }

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SavePartnerVendor(PartnerVendorModel model)
        {
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (model.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[PartnerVendor] (VendorName, VendorCode, AssignedGL, Email, Active, IsApproved)
                        VALUES(@VendorName, @VendorCode, @AssignedGL, @Email, @Active, @IsApproved)";

                        var parameters = new
                        {
                            VendorName = model.VendorName,
                            VendorCode = model.VendorCode,
                            AssignedGL = model.AssignedGL,
                            Email = model.Email,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"UPDATE [RCBC].[dbo].[PartnerVendor] SET VendorName = @VendorName,
                        VendorCode = @VendorCode, AssignedGL = @AssignedGL, Email = @Email, Active = @Active, IsApproved = @IsApproved
                        WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            VendorName = model.VendorName,
                            VendorCode = model.VendorCode,
                            AssignedGL = model.AssignedGL,
                            Email = model.Email,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                    }
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
                PartnerVendorModel data = new PartnerVendorModel();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[PartnerVendor] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<PartnerVendorModel>(query, new { Id = Id });

                    con.Close();
                }

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
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = "DELETE FROM [RCBC].[dbo].[PartnerVendor] WHERE Id = @Id";
                    con.Execute(query, new { Id = Id });

                    con.Close();
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
                IEnumerable<PickupLocationModel> data = new List<PickupLocationModel>();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[PickupLocation]";
                    data = con.Query<PickupLocationModel>(query);

                    con.Close();
                }

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SavePickupLocation(PickupLocationModel model)
        {
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (model.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[PickupLocation] (CorporateName, Site, SiteAddress, PartnerCode, SOLID, IsApproved)
                        VALUES(@CorporateName, @Site, @SiteAddress, @PartnerCode, @SOLID, @IsApproved)";

                        var parameters = new
                        {
                            CorporateName = model.CorporateName,
                            Site = model.Site,
                            SiteAddress = model.SiteAddress,
                            PartnerCode = model.PartnerCode,
                            SOLID = model.SOLID,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"UPDATE [RCBC].[dbo].[PickupLocation] SET CorporateName = @CorporateName,
                        Site = @Site, SiteAddress = @SiteAddress, PartnerCode = @PartnerCode, SOLID = @SOLID, Active = @Active, IsApproved = @IsApproved
                        WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            CorporateName = model.CorporateName,
                            Site = model.Site,
                            SiteAddress = model.SiteAddress,
                            PartnerCode = model.PartnerCode,
                            SOLID = model.SOLID,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                    }
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
                PickupLocationModel data = new PickupLocationModel();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[PickupLocation] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<PickupLocationModel>(query, new { Id = Id });

                    con.Close();
                }

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
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = "DELETE FROM [RCBC].[dbo].[PickupLocation] WHERE Id = @Id";
                    con.Execute(query, new { Id = Id });

                    con.Close();
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

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[Accounts] WHERE LocationId IS NOT NULL";
                    data = con.Query<AccountModel>(query);

                    con.Close();
                }

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
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (model.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[Accounts] (LocationId, AccountNumber, AccountName, CurrencyId, AccountTypeId)
                        VALUES(@LocationId, @AccountNumber, @AccountName, @CurrencyId, @AccountTypeId)";

                        var parameters = new
                        {
                            LocationId = model.LocationId,
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            CurrencyId = model.CurrencyId,
                            AccountTypeId = model.AccountTypeId,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"
                        UPDATE [RCBC].[dbo].[Accounts] 
                        SET LocationId = @LocationId, AccountNumber = @AccountNumber, AccountName = @AccountName, CurrencyId = @CurrencyId, AccountTypeId = @AccountTypeId
                        WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            LocationId = model.LocationId,
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            CurrencyId = model.CurrencyId,
                            AccountTypeId = model.AccountTypeId,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                    }
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
                PickupLocationModel data;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[Accounts] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<PickupLocationModel>(query, new { Id = Id });
                }

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
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = "DELETE FROM [RCBC].[dbo].[Accounts] WHERE Id = @Id";
                    con.Execute(query, new { Id = Id });

                    con.Close();
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
                IEnumerable<ContactModel> data = new List<ContactModel>();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[Contacts] WHERE LocationId IS NOT NULL";
                    data = con.Query<ContactModel>(query);

                    con.Close();
                }

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveContactDetails(PickupLocationModel model)
        {
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (model.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[Contacts] (LocationId, ContactPerson, Email, MobileNumber)
                        VALUES(@LocationId, @ContactPerson, @Email, @MobileNumber)";

                        var parameters = new
                        {
                            LocationId = model.LocationId,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"
                        UPDATE [RCBC].[dbo].[Contacts] 
                        SET LocationId = @LocationId, ContactPerson = @ContactPerson, Email = @Email, MobileNumber = @MobileNumber
                        WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            LocationId = model.LocationId,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                    }
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
                PickupLocationModel data;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[Contacts] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<PickupLocationModel>(query, new { Id = Id });
                }

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
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = "DELETE FROM [RCBC].[dbo].[Contacts] WHERE Id = @Id";
                    con.Execute(query, new { Id = Id });

                    con.Close();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    } //end

}
