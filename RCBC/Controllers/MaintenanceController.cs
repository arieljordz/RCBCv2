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

            string UserRole = Request.Cookies["UserRole"].ToString();

            ViewBag.Modules = global.GetModules(UserRole);
            ViewBag.SubModules = global.GetSubModules(UserRole);
            ViewBag.ChildModules = global.GetChildModules(UserRole);
            ViewBag.AccessModules = global.GetAccessModules();

            var UserRoles = global.GetUserRoles();
            ViewBag.cmbUserRoles = new SelectList(UserRoles, "UserRole", "UserRole");

            var Departments = global.GetDepartments();
            ViewBag.cmbDepartments = new SelectList(Departments, "GroupDept", "GroupDept");

            return View();
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

        public IActionResult SendForgotPassword(string Username)
        {
            string salt = ""; //read from database
            string HashedPass = ""; //read from database
            string PlainPass = "";
            bool result;
            string EmployeeName = "";
            int LoginAttempt = 0;
            string LastLogin = DateTime.Now.ToString("dd MMMM yyyy hh:mm tt");
            UserModel _userModel = new UserModel();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand("Select * from UsersInformation where UserId='" + Username + "'", con);
                con.Open();
                SqlDataReader sdr = cmd.ExecuteReader();
                while (sdr.Read())
                {
                    salt = sdr["Salt"].ToString();
                    HashedPass = sdr["HashPassword"].ToString();
                    EmployeeName = Convert.ToString(sdr["EmployeeName"]);
                    LoginAttempt = Convert.ToInt32(sdr["LoginAttempt"]);
                }
                con.Close();
                PlainPass = PlainPass + salt; // append salt key
                result = Crypto.VerifyHashedPassword(HashedPass, PlainPass); //verify password
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

        public IActionResult Register(UserModel user)
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
                            if (user.Id == 0)
                            {
                                // Insert into UsersInformation table
                                string insertUsersInfoQuery = @"
                                    INSERT INTO [RCBC].[dbo].[UsersInformation] (UserId, HashPassword, Salt, EmployeeName, Email, MobileNumber, GroupDept, UserRole, UserStatus, DateAdded, LoginAttempt)
                                    OUTPUT INSERTED.Id
                                    VALUES(@UserId, @HashedPassword, @Salt, @EmployeeName, @Email, @MobileNumber, @GroupDept, @UserRole, @UserStatus, @DateAdded, @LoginAttempt)";

                                var usersInfoParameters = new
                                {
                                    UserId = user.UserId.Replace("'", "''"),
                                    HashedPassword = hashedPassword,
                                    Salt = salt,
                                    EmployeeName = user.EmployeeName,
                                    Email = user.Email,
                                    MobileNumber = user.MobileNumber,
                                    GroupDept = user.GroupDept,
                                    UserRole = user.UserRole,
                                    UserStatus = "0",
                                    DateAdded = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"),
                                    LoginAttempt = "0"
                                };

                                // Execute the query and retrieve the inserted Id
                                int insertedId = con.QuerySingleOrDefault<int>(insertUsersInfoQuery, usersInfoParameters, transaction);


                                // Insert into UserRoleAccess table
                                string insertUserRoleAccessQuery = @"
                                INSERT INTO [RCBC].[dbo].[UserRoleAccess] (UserId, UserRole)
                                VALUES(@UserId, @UserRole)";

                                var userRoleAccessParameters = new
                                {
                                    UserId = insertedId,
                                    UserRole = user.UserRole,
                                };

                                con.Execute(insertUserRoleAccessQuery, userRoleAccessParameters, transaction);

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
                                    Id = user.Id,
                                    EmployeeName = user.EmployeeName,
                                    Email = user.Email,
                                    MobileNumber = user.MobileNumber,
                                    GroupDept = user.GroupDept,
                                    UserRole = user.UserRole,
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
                UserModel _userModel = new UserModel();
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    SqlCommand cmd = new SqlCommand("Select * from UsersInformation where UserId='" + Username + "'", con);
                    con.Open();
                    SqlDataReader sdr = cmd.ExecuteReader();
                    while (sdr.Read())
                    {
                        _userModel.UserId = Convert.ToString(sdr["UserId"]);
                        _userModel.EmployeeName = Convert.ToString(sdr["EmployeeName"]);
                        _userModel.Email = Convert.ToString(sdr["Email"]);
                        _userModel.MobileNumber = Convert.ToString(sdr["MobileNumber"]);
                        _userModel.GroupDept = Convert.ToString(sdr["GroupDept"]);
                        _userModel.UserRole = Convert.ToString(sdr["UserRole"]);
                    }
                    con.Close();
                }
                Trace.WriteLine(JsonConvert.SerializeObject(_userModel));
                return Json(new { success = true, data = _userModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult RegeneratePassword(string Username, string Password, string EmployeeName, string Email, string Mobileno, string GrpDept, string Role)
        {
            try
            {
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789./<>";
                var stringChars = new char[10];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    Trace.WriteLine(i);
                    stringChars[i] = chars[random.Next(chars.Length)];
                }
                var finalString = new string(stringChars);

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
                                "User ID: " + Username + "<br>" +
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

                try
                {
                    //update password in Database
                    var sql = "Update UsersInformation set HashPassword=@HashPassword, Salt=@Salt where UserId='" + Username + "'";
                    using (var connection = new SqlConnection(GetConnectionString()))
                    {
                        using (var command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@HashPassword", HashPassword);
                            command.Parameters.AddWithValue("@Salt", Salt);
                            // repeat for all variables....
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        connection.Close();
                    }
                    return Json(new { success = true, password = finalString });
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error: " + ex.Message);
                    return Json(new { success = false, message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadUsers()
        {

            List<UserModel> data = new List<UserModel>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand("Select * from UsersInformation", con);
                con.Open();
                SqlDataReader sdr = cmd.ExecuteReader();
                while (sdr.Read())
                {
                    UserModel _userModel = new UserModel();
                    _userModel.Id = Convert.ToInt32(sdr["Id"]);
                    _userModel.UserId = Convert.ToString(sdr["UserId"]);
                    _userModel.EmployeeName = Convert.ToString(sdr["EmployeeName"]);
                    _userModel.Email = Convert.ToString(sdr["Email"]);
                    _userModel.MobileNumber = Convert.ToString(sdr["MobileNumber"]);
                    _userModel.GroupDept = Convert.ToString(sdr["GroupDept"]);
                    _userModel.UserRole = Convert.ToString(sdr["UserRole"]);
                    data.Add(_userModel);
                }
                con.Close();
            }
            Trace.WriteLine(JsonConvert.SerializeObject(data));

            return Json(new { data = data });
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

                    string query = "DELETE FROM [RCBC].[dbo].[UsersInformation] WHERE Id = @Id";
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

        public IActionResult SaveUserRole(UserRoleModel role)
        {
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (role.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[UserRole] (UserRole)
                        VALUES(@UserRole)";

                        var parameters = new
                        {
                            UserRole = role.UserRole,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"UPDATE [RCBC].[dbo].[UserRole] SET UserRole = @UserRole WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = role.Id,
                            UserRole = role.UserRole,
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
                UserRoleModel data = new UserRoleModel();

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    string query = @"SELECT * FROM [RCBC].[dbo].[UserRole] WHERE Id = @Id";
                    data = con.QuerySingleOrDefault<UserRoleModel>(query, new { Id = Id });

                    con.Close();
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
                UserRoleModel data = new UserRoleModel();

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

        public IActionResult SaveDepartment(DepartmentModel dept)
        {
            try
            {
                string msg = string.Empty;
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    if (dept.Id == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[Department] (GroupDept)
                        VALUES(@GroupDept)";

                        var parameters = new
                        {
                            GroupDept = dept.GroupDept,
                        };

                        con.Execute(insertQuery, parameters);

                        msg = "Successfully saved.";
                    }
                    else
                    {
                        string updateQuery = @"UPDATE [RCBC].[dbo].[Department] SET GroupDept = @GroupDept WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = dept.Id,
                            GroupDept = dept.GroupDept,
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
                UserRoleModel data = new UserRoleModel();

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


    } //end

}
