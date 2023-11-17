using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using RCBC.Interface;
using RCBC.Models;
using System.Data.SqlClient;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RCBC.Controllers
{
    public class CorporateManagementController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;

        public CorporateManagementController(IConfiguration _configuration, IGlobalRepository _global)
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

                        var UserRoles = global.GetUserRole();
                        ViewBag.cmbUserRoles = new SelectList(UserRoles, "UserRole", "UserRole");

                        var Departments = global.GetDepartment();
                        ViewBag.cmbDepartments = new SelectList(Departments, "GroupDept", "GroupDept");

                        var EmailTypes = global.GetEmailType();
                        ViewBag.cmbEmailTypes = new SelectList(EmailTypes, "EmailType", "EmailType");

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

        public IActionResult ViewClientDetails()
        {
            return LoadViews();
        }

        public IActionResult CreateNewClient()
        {
            return LoadViews();
        }

        public IActionResult ClientApproval()
        {
            return LoadViews();
        }

        public IActionResult LoadClientDetails()
        {
            try
            {
                var data = global.GetCorporateClient().ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveClientDetails(CorporateClientModel model)
        {
            try
            {
                string msg = string.Empty;
                string action = string.Empty;
                string? previousData = string.Empty;
                string? newData = string.Empty;

                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    var qry = global.GetCorporateClient().Where(x => x.Id == model.Id).FirstOrDefault();

                    if (model.Id == 0)
                    {
                        string insertQuery = @"
                            INSERT INTO [RCBC].[dbo].[CorporateClient] (CorporateGroup, CorporateCode, CorporateName, ContactPerson, Email, MobileNumber, GlobalAccount, Active, IsApproved)
                            VALUES(@CorporateGroup, @CorporateCode, @CorporateName, @ContactPerson, @Email, @MobileNumber, @GlobalAccount, @Active, @IsApproved)
                            SELECT CAST(SCOPE_IDENTITY() AS INT)";

                        var parameters = new
                        {
                            CorporateGroup = model.CorporateGroup,
                            CorporateCode = model.CorporateCode,
                            CorporateName = model.CorporateName,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                            GlobalAccount = model.GlobalAccount,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                        };

                        model.Id = con.QuerySingleOrDefault<int>(insertQuery, parameters);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        string updateQuery = @"
                        UPDATE [RCBC].[dbo].[CorporateClient] 
                        SET CorporateGroup = @CorporateGroup, CorporateCode = @CorporateCode, CorporateName = @CorporateName,
                        ContactPerson = @ContactPerson, Email = @Email, MobileNumber = @MobileNumber, GlobalAccount = @GlobalAccount, Active = @Active, IsApproved = @IsApproved
                        WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            CorporateGroup = model.CorporateGroup,
                            CorporateCode = model.CorporateCode,
                            CorporateName = model.CorporateName,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                            GlobalAccount = model.GlobalAccount,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                        action = "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        SystemName = "RCBC",
                        Module = "Corporate Management",
                        SubModule = "Create New Client",
                        ChildModule = null,
                        TableName = "CorporateClient",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetCorporateClient().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = Convert.ToInt32(Request.Cookies["UserId"]),
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

        public IActionResult UpdateClientDetails(int Id)
        {
            try
            {
                var data = global.GetCorporateClient().Where(x => x.Id == Id).FirstOrDefault();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult RemoveCorporateClient(int Id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                {
                    con.Open();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            string corp = "DELETE FROM [RCBC].[dbo].[CorporateClient] WHERE Id = @Id";
                            con.Execute(corp, new { Id }, transaction);

                            string acc = "DELETE FROM [RCBC].[dbo].[Accounts] WHERE CorporateClientId = @Id";
                            con.Execute(acc, new { Id }, transaction);

                            string cont = "DELETE FROM [RCBC].[dbo].[Contacts] WHERE CorporateClientId = @Id";
                            con.Execute(cont, new { Id }, transaction);

                            transaction.Commit();

                            return Json(new { success = true });
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

                data = global.GetAccounts().Where(x => x.CorporateClientId != 0).ToList();

                List<AccountModel> result = data.Select(account => new AccountModel
                {
                    Id = account.Id,
                    CorporateClientId = account.CorporateClientId,
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

        public IActionResult SaveAccountDetails(CorporateClientModel model)
        {
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
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[Accounts] (CorporateClientId, AccountNumber, AccountName, CurrencyId, AccountTypeId)
                        VALUES(@CorporateClientId, @AccountNumber, @AccountName, @CurrencyId, @AccountTypeId)
                        SELECT CAST(SCOPE_IDENTITY() AS INT)";

                        var parameters = new
                        {
                            CorporateClientId = model.CorporateClientId,
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            CurrencyId = model.CurrencyId,
                            AccountTypeId = model.AccountTypeId,
                        };

                        //con.Execute(insertQuery, parameters);
                        model.Id = con.QuerySingleOrDefault<int>(insertQuery, parameters);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        string updateQuery = @"
                        UPDATE [RCBC].[dbo].[Accounts] 
                        SET CorporateClientId = @CorporateClientId, AccountNumber = @AccountNumber, AccountName = @AccountName, CurrencyId = @CurrencyId, AccountTypeId = @AccountTypeId
                        WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            CorporateClientId = model.CorporateClientId,
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            CurrencyId = model.CurrencyId,
                            AccountTypeId = model.AccountTypeId,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                        action = "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        SystemName = "RCBC",
                        Module = "Maintenance",
                        SubModule = model.CorporateClientId != 0 ? "Corporate Management" : "Pickup Location",
                        ChildModule = model.CorporateClientId != 0 ? "Create New Client" : "Add New Pickup Location",
                        TableName = "Accounts",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetAccounts().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = Convert.ToInt32(Request.Cookies["UserId"]),
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
                var data = global.GetContacts().Where(x => x.LocationId != 0).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveContactDetails(CorporateClientModel model)
        {
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
                        string insertQuery = @"
                        INSERT INTO [RCBC].[dbo].[Contacts] (CorporateClientId, ContactPerson, Email, MobileNumber)
                        VALUES(@CorporateClientId, @ContactPerson, @Email, @MobileNumber)
                        SELECT CAST(SCOPE_IDENTITY() AS INT)";

                        var parameters = new
                        {
                            CorporateClientId = model.CorporateClientId,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                        };

                        //con.Execute(insertQuery, parameters);
                        model.Id = con.QuerySingleOrDefault<int>(insertQuery, parameters);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        string updateQuery = @"
                        UPDATE [RCBC].[dbo].[Contacts] 
                        SET CorporateClientId = @CorporateClientId, ContactPerson = @ContactPerson, Email = @Email, MobileNumber = @MobileNumber
                        WHERE Id = @Id";

                        var parameters = new
                        {
                            Id = model.Id,
                            CorporateClientId = model.CorporateClientId,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                        };

                        con.Execute(updateQuery, parameters);

                        msg = "Successfully updated.";
                        action = "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        SystemName = "RCBC",
                        Module = "Maintenance",
                        SubModule = model.CorporateClientId != 0 ? "Corporate Management" : "Pickup Location",
                        ChildModule = model.CorporateClientId != 0 ? "Create New Client" : "Add New Pickup Location",
                        TableName = "Accounts",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetContacts().Where(x => x.Id == model.Id).FirstOrDefault()),
                        ModifiedBy = Convert.ToInt32(Request.Cookies["UserId"]),
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

    }
}
