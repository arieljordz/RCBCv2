using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using RCBC.Interface;
using RCBC.Models;
using System.Data;
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

                        var Contacts = global.GetContacts().OrderBy(x => x.Id);
                        ViewBag.cmbContacts = new SelectList(Contacts, "Id", "ContactPerson");

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

                        model.Id = con.QuerySingle<int>("sp_saveCorporateClient", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
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

                        con.Execute("sp_updateCorporateClient", parameters, commandType: CommandType.StoredProcedure);

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
                        var parameters = new
                        {
                            LocationId = 0,
                            CorporateClientId = model.CorporateClientId,
                            AccountNumber = model.AccountNumber,
                            AccountName = model.AccountName,
                            CurrencyId = model.CurrencyId,
                            AccountTypeId = model.AccountTypeId,
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
                            LocationId = 0,
                            CorporateClientId = model.CorporateClientId,
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
                var data = global.GetContacts().Where(x => x.CorporateClientId != 0).ToList();

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
                        var parameters = new
                        {
                            LocationId = 0,
                            CorporateClientId = model.CorporateClientId,
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
                        var contact = global.GetContacts().Where(x => x.Id == model.Id).FirstOrDefault();

                        var parameters = new
                        {
                            Id = model.Id,
                            LocationId = 0,
                            CorporateClientId = model.CorporateClientId == 0 ? contact.CorporateClientId : model.CorporateClientId,
                            ContactPerson = model.ContactPerson == null ? contact.ContactPerson : model.ContactPerson,
                            Email = model.Email == null ? contact.Email : model.Email,
                            MobileNumber = model.MobileNumber == null ? contact.MobileNumber : model.MobileNumber,
                        };

                        con.Execute("sp_updateContact", parameters, commandType: CommandType.StoredProcedure);

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

    }
}
