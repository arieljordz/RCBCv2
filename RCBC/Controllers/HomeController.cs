using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RCBC.Models;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Web.Helpers;

namespace RCBC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration Configuration;
        public HomeController(IConfiguration _configuration)
        {
            Configuration = _configuration;
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

            return View();
        }

        public IActionResult Index()
        {
            return LoadViews();
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
        public IActionResult Logout()
        {
            Response.Cookies.Delete("Username");
            Response.Cookies.Delete("LastLogin");
            Response.Cookies.Delete("EmployeeName");
            return RedirectToAction("Index");
        }
        public IActionResult LoginProceed()
        {
            var UserRole = Request.Cookies["UserRole"];

            if (UserRole == "System Admin")
            {
                return RedirectToAction("Dashboard", "Home");
            }
            else if (UserRole == "Admin (User)")
            {
                return RedirectToAction("ViewAllUsers", "Maintenance");
            }
            else if (UserRole == "Maker/Approver")
            {
                return RedirectToAction("CreateNewRole", "Maintenance");
            }
            else if (UserRole == "Operations")
            {
                return RedirectToAction("Reports", "Reports");
            }
            else //IT Support
            {
                return RedirectToAction("Reports", "Reports");
            }
        }

        public IActionResult Login(string Username, string Password)
        {
            string salt = ""; //read from database
            string HashedPass = ""; //read from database
            string PlainPass = Password;
            bool result;
            string EmployeeName = "";
            string UserRole = "";
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
                    UserRole = Convert.ToString(sdr["UserRole"]);
                    LoginAttempt = Convert.ToInt32(sdr["LoginAttempt"]);
                }
                con.Close();
                PlainPass = PlainPass + salt; // append salt key
                result = Crypto.VerifyHashedPassword(HashedPass, PlainPass); //verify password
            }
            Trace.WriteLine(result);

            var cookieOptions = new CookieOptions
            {
                // Set additional options if needed
                Expires = DateTime.Now.AddMinutes(30),
                Secure = true,
                HttpOnly = true
            };

            // Add the cookie to the response
            Response.Cookies.Append("Username", Username, cookieOptions);
            Response.Cookies.Append("LastLogin", LastLogin, cookieOptions);
            Response.Cookies.Append("EmployeeName", EmployeeName, cookieOptions);
            Response.Cookies.Append("UserRole", UserRole, cookieOptions);

            if (result == true)
            {
                if (LoginAttempt == 0)
                {
                    return RedirectToAction("FirstLogin", "Home");
                }
                else
                {
                    if (UserRole == "System Admin")
                    {
                        return RedirectToAction("Dashboard", "Home");
                    }
                    else if (UserRole == "Admin (User)")
                    {
                        return RedirectToAction("ViewAllUsers", "Maintenance");
                    }
                    else if (UserRole == "Maker/Approver")
                    {
                        return RedirectToAction("CreateNewRole", "Maintenance");
                    }
                    else if (UserRole == "Operations")
                    {
                        return RedirectToAction("Reports", "Reports");
                    }
                    else //IT Support
                    {
                        return RedirectToAction("Reports", "Reports");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View("Index");
            }
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

    } //end
}