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
            ViewBag.Username = Request.Cookies["rcbctellerlessusername"];
            ViewBag.UserId = Request.Cookies["rcbctellername"];

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
        public IActionResult ForgotPassword()
        {
            return LoadViews();
        }
        public IActionResult CreateNewRole()
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

        public IActionResult Register(string Username, string Password, string EmployeeName, string Email, string Mobileno, string GrpDept, string Role)
        {
            try
            {
                string Salt = Crypto.GenerateSalt();
                string password = Password + Salt;
                string HashPassword = Crypto.HashPassword(password);

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = con;
                        cmd.Parameters.Clear();
                        cmd.CommandText = "" +
                            "BEGIN " +
                                "IF NOT EXISTS (SELECT * FROM UsersInformation " +
                                "WHERE UserId = @UserId) " +
                            "BEGIN " +
                                "INSERT INTO UsersInformation (UserId, HashPassword, Salt, EmployeeName, Email, " +
                                "MobileNumber, GroupDept, UserRole, UserStatus, DateAdded, LoginAttempt) " +
                                "VALUES(@UserId, @HashPassword, @Salt, @EmployeeName, @Email, @MobileNumber, @GroupDept, @UserRole, @UserStatus, @DateAdded, @LoginAttempt)" +
                                "END " +
                            "END";
                        cmd.Parameters.AddWithValue("@UserId", Username.Replace("\'", "\''"));
                        cmd.Parameters.AddWithValue("@HashPassword", HashPassword);
                        cmd.Parameters.AddWithValue("@Salt", Salt);
                        cmd.Parameters.AddWithValue("@EmployeeName", EmployeeName);
                        cmd.Parameters.AddWithValue("@Email", Email);
                        cmd.Parameters.AddWithValue("@MobileNumber", Mobileno);
                        cmd.Parameters.AddWithValue("@GroupDept", GrpDept);
                        cmd.Parameters.AddWithValue("@UserRole", Role);
                        cmd.Parameters.AddWithValue("@UserStatus", "0");
                        cmd.Parameters.AddWithValue("@DateAdded", DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"));
                        cmd.Parameters.AddWithValue("@LoginAttempt", "0");
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }
                return Json(new { success = true });

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
                    return Json(new { success = true });
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

        public IActionResult UpdatePassword(UpdatePasswordModel obj)
        {
            try
            {
                string salt = Empty.ToString();
                string OldPassword = Empty.ToString();
                int LoginAttempt = 0;
                bool result;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    SqlCommand cmd = new SqlCommand("Select * from UsersInformation where UserId='" + obj.Username + "'", con);
                    con.Open();
                    SqlDataReader sdr = cmd.ExecuteReader();
                    while (sdr.Read())
                    {
                        salt = sdr["Salt"].ToString();
                        OldPassword = sdr["HashPassword"].ToString();
                        LoginAttempt = Convert.ToInt32(sdr["LoginAttempt"]);
                    }
                    con.Close();
                    obj.OldPassword = obj.OldPassword + salt;
                    result = Crypto.VerifyHashedPassword(OldPassword, obj.OldPassword);
                }

                if (result)
                {
                    bool accepted = global.IsStrongPassword(obj.NewPassword);
                    ModelState.AddModelError("", "Strong Password.");

                    if (accepted)
                    {
                        var finalString = new string(obj.NewPassword);

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
                                        "User ID: " + obj.Username + "<br>" +
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
                            var sql = "Update UsersInformation set HashPassword=@HashPassword, Salt=@Salt, LoginAttempt=@LoginAttempt where UserId='" + obj.Username + "'";
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
                            return RedirectToAction("Dashboard", "Home");
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("Error: " + ex.Message);
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Weak Password.");
                        return RedirectToAction("ChangePassword", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
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


    } //end
}
