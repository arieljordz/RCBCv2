using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using RCBC.Interface;
using RCBC.Models;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RCBC.Controllers
{
    public class CorporateManagementController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;
        public int GlobalUserId { get; set; }

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
                GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;

                if (GlobalUserId != 0)
                {
                    var chkStatus = global.CheckUserStatus(GlobalUserId);

                    if (chkStatus)
                    {
                        ViewBag.Modules = global.GetModulesByUserId(GlobalUserId);
                        ViewBag.SubModules = global.GetSubModulesByUserId(GlobalUserId);
                        ViewBag.ChildModules = global.GetChildModulesByUserId(GlobalUserId);
                        ViewBag.AccessLinks = global.GetAccessLinkByUserId(GlobalUserId);

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

        public IActionResult LoadClientDetails(string role)
        {
            try
            {
                var data = global.GetCorporateClient().OrderBy(x => x.Id).ToList();

                if (role.ToLower().Contains("approver"))
                {
                    data = global.GetCorporateClient().Where(x => x.IsApproved == null).OrderBy(x => x.Id).ToList();
                }

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveClientDetails(CorporateClientModel model)
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
                    var qry = global.GetCorporateClient().Where(x => x.Id == model.Id).FirstOrDefault();

                    if (model.Id == 0)
                    {
                        var parameters = new
                        {
                            CorporateGroup = model.CorporateGroup,
                            PartnerCode = model.PartnerCode,
                            CorporateName = model.CorporateName,
                            ContactPerson = model.ContactPerson,
                            Email = model.Email,
                            MobileNumber = model.MobileNumber,
                            GlobalAccount = model.GlobalAccount,
                            Active = model.Active,
                            IsApproved = model.IsApproved,
                            DateCreated = DateTime.Now,
                            CreatedBy = GlobalUserId,
                        };

                        model.Id = con.QuerySingle<int>("sp_saveCorporateClient", parameters, commandType: CommandType.StoredProcedure);

                        msg = "Successfully saved.";
                        action = "Add";
                        previousData = null;
                    }
                    else
                    {
                        if (model.ForApproval)
                        {
                            var forUpdate = global.GetApprovalUpdates().Where(x => x.TableId == model.Id).FirstOrDefault();

                            if (forUpdate != null)
                            {
                                con.Open();
                                using (var transaction = con.BeginTransaction())
                                {
                                    try
                                    {
                                        var obj = JsonConvert.DeserializeObject<CorporateClientModel>(forUpdate.JsonData);

                                        var clientParameters = new
                                        {
                                            Id = obj.Id,
                                            CorporateGroup = obj.CorporateGroup,
                                            PartnerCode = obj.PartnerCode,
                                            CorporateName = obj.CorporateName,
                                            ContactPerson = obj.ContactPerson,
                                            Email = obj.Email,
                                            MobileNumber = obj.MobileNumber,
                                            GlobalAccount = obj.GlobalAccount,
                                            Active = obj.Active,
                                            IsApproved = true,
                                            DateApproved = DateTime.Now,
                                            ApprovedBy = GlobalUserId,
                                        };

                                        con.Execute("sp_updateCorporateClient", clientParameters, commandType: CommandType.StoredProcedure, transaction: transaction);

                                        con.Execute("sp_deleteApprovalUpdates", new { Id = model.Id }, commandType: CommandType.StoredProcedure, transaction: transaction);

                                        transaction.Commit();
                                    }
                                    catch (Exception ex)
                                    {
                                        transaction.Rollback();
                                        return Json(new { success = false, message = ex.Message });
                                    }
                                }
                                con.Close();
                            }
                            else
                            {
                                var clientParameters = new
                                {
                                    Id = model.Id,
                                    CorporateGroup = model.CorporateGroup,
                                    PartnerCode = model.PartnerCode,
                                    CorporateName = model.CorporateName,
                                    ContactPerson = model.ContactPerson,
                                    Email = model.Email,
                                    MobileNumber = model.MobileNumber,
                                    GlobalAccount = model.GlobalAccount,
                                    Active = true,
                                    IsApproved = true,
                                    DateApproved = DateTime.Now,
                                    ApprovedBy = GlobalUserId,
                                };

                                con.Execute("sp_updateCorporateClient", clientParameters, commandType: CommandType.StoredProcedure);
                            }
                        }
                        else
                        {
                            var parameters = new
                            {
                                Id = model.Id,
                                CorporateGroup = model.CorporateGroup,
                                PartnerCode = model.PartnerCode,
                                CorporateName = model.CorporateName,
                                ContactPerson = model.ContactPerson,
                                Email = model.Email,
                                MobileNumber = model.MobileNumber,
                                GlobalAccount = model.GlobalAccount,
                                Active = model.Active,
                                IsApproved = model.IsApproved,
                                DateApproved = DateTime.Now,
                                ApprovedBy = GlobalUserId,
                            };

                            var approvalParameters = new
                            {
                                JsonData = JsonConvert.SerializeObject(parameters),
                                TableId = model.Id,
                                TableName = "CorporateClient",
                                ModifiedBy = GlobalUserId,
                                DateModified = DateTime.Now,
                            };
                            con.Execute("sp_saveApprovalUpdates", approvalParameters, commandType: CommandType.StoredProcedure);

                            var _status = global.UpdateApprovalStatus(model.Id, "client", null, null);
                        }

                        msg = "Successfully updated.";
                        action = model.ForApproval ? "Approved" : "Update";
                        previousData = JsonConvert.SerializeObject(qry);
                    }

                    var auditlogs = new AuditLogsModel
                    {
                        Module = "Corporate Management",
                        SubModule = "Create New Client",
                        ChildModule = null,
                        TableName = "CorporateClient",
                        TableId = model.Id,
                        Action = action,
                        PreviousData = previousData,
                        NewData = JsonConvert.SerializeObject(global.GetCorporateClient().Where(x => x.Id == model.Id).FirstOrDefault()),
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
                            con.Execute("sp_deleteCorporateClient", new { Id }, transaction, commandType: CommandType.StoredProcedure);

                            con.Execute("sp_deleteAccountCorporateClient", new { Id }, transaction, commandType: CommandType.StoredProcedure);

                            con.Execute("sp_deleteContactCorporateClient", new { Id }, transaction, commandType: CommandType.StoredProcedure);

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
                            LocationId = 0,
                            CorporateClientId = model.CorporateClientId,
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
                        Module = "Maintenance",
                        SubModule = model.CorporateClientId != 0 ? "Corporate Management" : "Pickup Location",
                        ChildModule = model.CorporateClientId != 0 ? "Create New Client" : "Add New Pickup Location",
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
                var data = global.GetContacts().Where(x => x.CorporateClientId != 0).OrderBy(x => x.Id).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public IActionResult SaveContactDetails(CorporateClientModel model)
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
                        var contact = global.GetContacts().Where(x => x.Id == model.Id && x.CorporateClientId == model.CorporateClientId).FirstOrDefault();

                        if (contact != null)
                        {
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
                        else
                        {
                            var contacts = global.GetContacts().Where(x => x.Id == model.Id).FirstOrDefault();

                            var parameters = new
                            {
                                LocationId = 0,
                                CorporateClientId = model.CorporateClientId == 0 ? contacts.CorporateClientId : model.CorporateClientId,
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
                        SubModule = model.CorporateClientId != 0 ? "Corporate Management" : "Pickup Location",
                        ChildModule = model.CorporateClientId != 0 ? "Create New Client" : "Add New Pickup Location",
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

    }
}
