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

namespace RCBC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;

        public HomeController(IConfiguration _configuration, IGlobalRepository _global)
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
                return View("Signout");
            }
        }

        public IActionResult Index()
        {
            return View();
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

        public IActionResult Signout()
        {
            Response.Cookies.Delete("Username");
            Response.Cookies.Delete("LastLogin");
            Response.Cookies.Delete("EmployeeName");
            return RedirectToAction("Index");
        }
        
        public IActionResult Logout()
        {
            Response.Cookies.Delete("Username");
            Response.Cookies.Delete("LastLogin");
            Response.Cookies.Delete("EmployeeName");
            return RedirectToAction("Index");
        }
       
        public IActionResult LoginProceed()
        {
            int UserId = Convert.ToInt32(Request.Cookies["UserId"].ToString());

            if (UserId != 0)
            {
                var SubModules = global.GetSubModulesByUserId(UserId).FirstOrDefault();
                var ChildModules = global.GetChildModulesByUserId(UserId).FirstOrDefault();

                if (SubModules == null || ChildModules == null)
                {
                    return RedirectToAction("NoAccess");
                }
                else
                {
                    string input = (SubModules != null && SubModules.Link != null) ? SubModules.Link : ChildModules.Link;
                    string[] Link = input.Split('/');
                    return RedirectToAction(Link[2], Link[1]);
                }  
            }
            else
            {
                return View("Signout");
            }
        }

        public IActionResult Login(string Username, string Password)
        {
            UserModel _userModel;

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                _userModel = con.QueryFirstOrDefault<UserModel>(@"SELECT * FROM [RCBC].[dbo].[UsersInformation] WHERE UserId LIKE '%' + @Username + '%'", new { Username });

                if (_userModel != null)
                {
                    // Combine the provided password with the salt
                    string PlainPass = Password + _userModel.Salt;

                    // Verify the hashed password
                    bool result = Crypto.VerifyHashedPassword(_userModel.HashPassword, PlainPass);

                    if (result)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            // Set additional options if needed
                            Expires = DateTime.Now.AddMinutes(30),
                            Secure = true,
                            HttpOnly = true
                        };

                        // Add the cookie to the response
                        Response.Cookies.Append("Username", Username, cookieOptions);
                        Response.Cookies.Append("LastLogin", DateTime.Now.ToString("dd MMMM yyyy hh:mm tt"), cookieOptions);
                        Response.Cookies.Append("EmployeeName", _userModel.EmployeeName, cookieOptions);
                        Response.Cookies.Append("UserRole", _userModel.UserRole, cookieOptions);
                        Response.Cookies.Append("UserId", _userModel.Id.ToString(), cookieOptions);

                        // Fetch SubModules and ChildModules as needed
                        var SubModules = global.GetSubModulesByUserId(_userModel.Id).FirstOrDefault();
                        var ChildModules = global.GetChildModulesByUserId(_userModel.Id).FirstOrDefault();

                        if (_userModel.LoginAttempt == 0)
                        {
                            return RedirectToAction("FirstLogin", "Home");
                        }
                        else
                        {
                            string input = (SubModules != null && SubModules.Link != null) ? SubModules.Link : ChildModules.Link;
                            string[] Link = input.Split('/');

                            return RedirectToAction(Link[2], Link[1]);
                        }
                    }
                }
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View("Index");
        }

        public IActionResult SendResetPasswordLink(string UserID)
        {
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
                                  "User ID: " + UserID + "<br>" +
                                  "Link: <a href='http://www.example.com'>Reset Password here</a> <br>" +
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

            return RedirectToAction("Logout", "Home");
        }

        public IActionResult UpdatePassword(UpdatePasswordModel model)
        {
            try
            {
                string salt = Empty.ToString();
                string OldPassword = Empty.ToString();
                int LoginAttempt = 0;
                bool result;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    SqlCommand cmd = new SqlCommand("Select * from UsersInformation where UserId='" + model.Username + "'", con);
                    con.Open();
                    SqlDataReader sdr = cmd.ExecuteReader();
                    while (sdr.Read())
                    {
                        salt = sdr["Salt"].ToString();
                        OldPassword = sdr["HashPassword"].ToString();
                        LoginAttempt = Convert.ToInt32(sdr["LoginAttempt"]);
                    }
                    con.Close();
                    model.OldPassword = model.OldPassword + salt;
                    result = Crypto.VerifyHashedPassword(OldPassword, model.OldPassword);
                }

                if (result)
                {
                    bool accepted = global.IsStrongPassword(model.NewPassword);

                    if (accepted)
                    {
                        var finalString = new string(model.NewPassword);

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
                                        "User ID: " + model.Username + "<br>" +
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
                            var sql = "Update UsersInformation set HashPassword=@HashPassword, Salt=@Salt, LoginAttempt=@LoginAttempt where UserId='" + model.Username + "'";
                            using (var connection = new SqlConnection(GetConnectionString()))
                            {
                                using (var command = new SqlCommand(sql, connection))
                                {
                                    command.Parameters.AddWithValue("@HashPassword", HashPassword);
                                    command.Parameters.AddWithValue("@Salt", Salt);
                                    command.Parameters.AddWithValue("@LoginAttempt", LoginAttempt + 1);
                                    // repeat for all variables....
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }
                                connection.Close();
                            }
                            return Json(new { success = true });
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("Error: " + ex.Message);
                            return RedirectToAction("Index", "Home");
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

    } //end
}