using RCBC.Interface;
using RCBC.Models;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Sockets;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Transactions;
using Newtonsoft.Json.Linq;
using static iTextSharp.text.pdf.PdfSigLockDictionary;

namespace RCBC.Repository
{
    public class GlobalRepository : IGlobalRepository
    {
        private readonly IConfiguration Configuration;

        public GlobalRepository(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        private string GetConnectionString()
        {
            return Configuration.GetConnectionString("DefaultConnection");
        }

        public List<AccessModuleModel> GetModulesByUserId(int UserId)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        UserId = UserId,
                    };
                    List<AccessModuleModel> data = con.Query<AccessModuleModel>("sp_getModulesByUserId", parameters, commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<AccessModuleModel>();
            }
        }

        public List<AccessModuleModel> GetSubModulesByUserId(int UserId)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        UserId = UserId,
                    };
                    List<AccessModuleModel> data = con.Query<AccessModuleModel>("sp_getSubModulesByUserId", parameters, commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<AccessModuleModel>();
            }
        }

        public List<AccessModuleModel> GetChildModulesByUserId(int UserId)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        UserId = UserId,
                    };
                    List<AccessModuleModel> data = con.Query<AccessModuleModel>("sp_getChildModulesByUserId", parameters, commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<AccessModuleModel>();
            }
        }

        public List<AccessModuleModel> GetModulesAndSubModules()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<AccessModuleModel> data = con.Query<AccessModuleModel>("sp_getModulesAndSubModules", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<AccessModuleModel>();
            }
        }

        public List<AccessModuleModel> GetUserAccessById(int UserId)
        {
            List<AccessModuleModel> modules = new List<AccessModuleModel>();

            var AllModules = GetModulesAndSubModules();

            foreach (var module in AllModules)
            {
                if (UserId != 0)
                {
                    var IsActive = GetUserAccessModules().Where(x => x.UserId == UserId && x.SubModuleId == module.SubModuleId && x.ChildModuleId == module.ChildModuleId).FirstOrDefault();

                    AccessModuleModel access = new AccessModuleModel();

                    if (IsActive != null)
                    {
                        access.IsActive = IsActive.IsActive;
                    }
                    else
                    {
                        access.IsActive = false;
                    }
                    access.Module = module.Module;
                    access.SubModule = module.SubModule;
                    access.ChildModule = module.ChildModule;
                    access.SubModuleId = module.SubModuleId;
                    access.ChildModuleId = module.ChildModuleId;
                    access.ModuleOrder = module.ModuleOrder;
                    access.Link = module.Link;
                    modules.Add(access);
                }
                else
                {
                    AccessModuleModel access = new AccessModuleModel();
                    access.IsActive = false;
                    access.Module = module.Module;
                    access.SubModule = module.SubModule;
                    access.ChildModule = module.ChildModule;
                    access.SubModuleId = module.SubModuleId;
                    access.ChildModuleId = module.ChildModuleId;
                    access.ModuleOrder = module.ModuleOrder;
                    access.Link = module.Link;
                    modules.Add(access);
                }

            }

            return modules.OrderBy(x => x.ModuleOrder).ToList();
        }

        public List<UserModel> GetUserInformation()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<UserModel> data = con.Query<UserModel>("sp_getUserInformation", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<UserModel>();
            }
        }

        public List<UserRoleModel> GetUserRole()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<UserRoleModel> data = con.Query<UserRoleModel>("sp_getUserRole", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<UserRoleModel>();
            }
        }

        public List<AccessModuleModel> GetUserAccessModules()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<AccessModuleModel> data = con.Query<AccessModuleModel>("sp_getUserAccessModules", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<AccessModuleModel>();
            }
        }

        public List<SubModuleModel> GetSubModule()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<SubModuleModel> data = con.Query<SubModuleModel>("sp_getSubModule", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<SubModuleModel>();
            }
        }

        public List<PickupLocationModel> GetPickupLocation()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<PickupLocationModel> data = con.Query<PickupLocationModel>("sp_getPickupLocation", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<PickupLocationModel>();
            }
        }

        public List<PartnerVendorModel> GetPartnerVendor()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<PartnerVendorModel> data = con.Query<PartnerVendorModel>("sp_getPartnerVendor", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<PartnerVendorModel>();
            }
        }

        public List<ModuleModel> GetModule()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<ModuleModel> data = con.Query<ModuleModel>("sp_getModule", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ModuleModel>();
            }
        }

        public List<EmailTypeModel> GetEmailType()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<EmailTypeModel> data = con.Query<EmailTypeModel>("sp_getEmailType", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<EmailTypeModel>();
            }
        }

        public List<DepartmentModel> GetDepartment()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<DepartmentModel> data = con.Query<DepartmentModel>("sp_getDepartment", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<DepartmentModel>();
            }
        }

        public List<CorporateClientModel> GetCorporateClient()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<CorporateClientModel> data = con.Query<CorporateClientModel>("sp_getCorporateClient", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<CorporateClientModel>();
            }
        }

        public List<ContactModel> GetContacts()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<ContactModel> data = con.Query<ContactModel>("sp_getContacts", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ContactModel>();
            }
        }

        public List<ChildModuleModel> GetChildModule()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<ChildModuleModel> data = con.Query<ChildModuleModel>("sp_getChildModule", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ChildModuleModel>();
            }
        }

        public List<AuditLogsModel> GetAuditLogs()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<AuditLogsModel> data = con.Query<AuditLogsModel>("sp_getAuditLogs", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<AuditLogsModel>();
            }
        }

        public List<AccountModel> GetAccounts()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<AccountModel> data = con.Query<AccountModel>("sp_getAccounts", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<AccountModel>();
            }
        }
        public List<TransmittalDetailModel> GetTransmittalDetails()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<TransmittalDetailModel> data = con.Query<TransmittalDetailModel>("sp_getTransmittalDetails", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<TransmittalDetailModel>();
            }
        }

        public List<AuditLogsModel> GetAuditlogsReport(DateTime? DateFrom, DateTime? DateTo, string? EmployeeName, string? GroupDept, string? UserRole, string? Status)
        {
            try
            {
                List<AuditLogsModel> qry = new List<AuditLogsModel>();

                qry = GetAuditLogs()
                .Where(x =>
                    (!DateFrom.HasValue || x.DateModified.Date >= DateFrom.Value.Date) &&
                    (!DateTo.HasValue || x.DateModified.Date <= DateTo.Value.Date) &&
                    (EmployeeName == null || x.EmployeeName.ToString().ToLower().Contains(EmployeeName.ToLower())) &&
                    (GroupDept == null || x.GroupDept.ToLower().Contains(GroupDept.ToLower())) &&
                    (UserRole == null || x.UserRole.ToString().ToLower().Contains(UserRole)) &&
                    (Status == null || x.Action.ToLower().Contains(Status.ToLower()))).OrderBy(x => x.Id)
                .ToList();

                List<AuditLogsModel> data = new List<AuditLogsModel>();

                int count = 1;
                foreach (var item in qry)
                {
                    AuditLogsModel obj = new AuditLogsModel();

                    obj.No = count;
                    obj.Id = item.Id;
                    obj.Module = item.Module;
                    obj.SubModule = item.SubModule;
                    obj.ChildModule = item.ChildModule;
                    obj.TableName = item.TableName;
                    obj.TableId = item.TableId;
                    obj.Action = item.Action;
                    obj.GroupDept = item.GroupDept;
                    obj.UserRole = item.UserRole;
                    obj.PreviousData = item.PreviousData;
                    obj.NewData = item.NewData;
                    obj.ModifiedBy = item.ModifiedBy;
                    obj.DateModified = item.DateModified;
                    obj.IP = item.IP;
                    obj.EmployeeName = item.EmployeeName;
                    obj.Details = GetColumnDetails(obj);
                    data.Add(obj);
                    count++;
                }

                return data;
            }
            catch (Exception ex)
            {
                return new List<AuditLogsModel>();
            }
        }

        public List<DPUStatusModel> GetDPUStatusReport(DateTime? DateFrom, DateTime? DateTo, string? LocationCode, string? BeneficiaryName, string? AccountNumber, string? Status)
        {
            try
            {
                List<DPUStatusModel> qry = new List<DPUStatusModel>();

                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        DateFrom = DateFrom,
                        DateTo = DateTo,
                        LocationCode = LocationCode,
                        BeneficiaryName = BeneficiaryName,
                        AccountNumber = AccountNumber,
                        Status = Status
                    };
                    qry = con.Query<DPUStatusModel>("sp_getDPUStatus", parameters, commandType: CommandType.StoredProcedure).ToList();
                }

                List<DPUStatusModel> data = new List<DPUStatusModel>();

                int count = 1;
                foreach (var item in qry)
                {
                    DPUStatusModel obj = new DPUStatusModel();

                    obj.No = count;
                    obj.Id = item.Id;
                    obj.LocationCode = item.LocationCode?.ToUpper();
                    obj.LocationName = item.LocationName?.ToUpper();
                    obj.TransactionDate = item.TransactionDate;
                    obj.TransactionTime = item.TransactionTime;
                    obj.CardNumber = item.CardNumber?.ToUpper();
                    obj.AccountNumber = item.AccountNumber?.ToUpper();
                    obj.BeneficiaryName = item.BeneficiaryName?.ToUpper();
                    obj.Amount = item.Amount;
                    obj.CreditDescription = item.CreditDescription?.ToUpper();
                    obj.ExternalReference = item.ExternalReference?.ToUpper();
                    obj.Status = item.Status;
                    data.Add(obj);
                    count++;
                }
                return data;
            }
            catch (Exception ex)
            {
                return new List<DPUStatusModel>();
            }
        }

        public List<ApprovalUpdatesModel> GetApprovalUpdates()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<ApprovalUpdatesModel> data = con.Query<ApprovalUpdatesModel>("sp_getApprovalUpdates", commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ApprovalUpdatesModel>();
            }
        }

        public bool IsStrongPassword(string password)
        {
            // Define criteria for a strong password
            int minLength = 8; // Minimum length
            int minDigitCount = 1; // Minimum number of digits
            int minUpperCount = 1; // Minimum number of uppercase letters
            int minLowerCount = 1; // Minimum number of lowercase letters
            int minSpecialCount = 1; // Minimum number of special characters
            string specialCharacters = @"!@#$%^&*()_+[]{}|;:,.<>?";

            // Check if the password meets the criteria
            if (password.Length < minLength) return false;
            if (password.Count(char.IsDigit) < minDigitCount) return false;
            if (password.Count(char.IsUpper) < minUpperCount) return false;
            if (password.Count(char.IsLower) < minLowerCount) return false;
            if (password.Count(c => specialCharacters.Contains(c)) < minSpecialCount) return false;

            // You can add more complex checks as needed, such as checking for common passwords, dictionary words, or patterns.

            return true;
        }

        public bool CheckUserStatus(int UserId)
        {
            var user = GetUserInformation().Where(x => x.Id == UserId).FirstOrDefault();

            return user == null ? false : user.UserStatus;
        }

        public string GetLocalIPAddress()
        {
            string localIp = "?";
            try
            {
                // Get the host name of the local machine
                string hostName = Dns.GetHostName();

                // Get the IP addresses associated with the host name
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                // Find the first IPv4 address (assuming IPv4 is used)
                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIp = address.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting local IP address: {ex.Message}");
            }
            return localIp;
        }

        public bool SaveAuditLogs(AuditLogsModel model)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        Module = model.Module,
                        SubModule = model.SubModule,
                        ChildModule = model.ChildModule,
                        TableName = model.TableName,
                        TableId = model.TableId,
                        Action = model.Action,
                        PreviousData = model.PreviousData,
                        NewData = model.NewData,
                        ModifiedBy = model.ModifiedBy,
                        DateModified = model.DateModified,
                        IP = model.IP
                    };

                    con.Execute("sp_saveAuditLogs", parameters, commandType: CommandType.StoredProcedure);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<KeyValuePair<string, Tuple<object, object>>> FindChanges(Dictionary<string, object> obj1, Dictionary<string, object> obj2)
        {
            List<KeyValuePair<string, Tuple<object, object>>> changes = new List<KeyValuePair<string, Tuple<object, object>>>();

            foreach (var property in obj1)
            {
                if (obj2.ContainsKey(property.Key) && !object.Equals(obj2[property.Key], property.Value))
                {
                    changes.Add(new KeyValuePair<string, Tuple<object, object>>(property.Key, new Tuple<object, object>(property.Value, obj2[property.Key])));
                }
            }

            foreach (var property in obj2)
            {
                if (!obj1.ContainsKey(property.Key))
                {
                    changes.Add(new KeyValuePair<string, Tuple<object, object>>(property.Key, new Tuple<object, object>(null, property.Value)));
                }
            }

            return changes;
        }

        public List<DataChangesModel> GetChangesDetails(int Id, string TableName)
        {
            var logs = GetAuditLogs().Where(x => x.TableName.Contains(TableName) && x.TableId == Id).LastOrDefault();

            List<DataChangesModel> data = new List<DataChangesModel>();

            if (logs != null)
            {
                if (logs.PreviousData != null && logs.NewData != null)
                {
                    var PreviousData = JsonConvert.DeserializeObject<Dictionary<string, object>>(logs.PreviousData);
                    var NewData = JsonConvert.DeserializeObject<Dictionary<string, object>>(logs.NewData);

                    var changes = FindChanges(PreviousData, NewData);

                    foreach (var change in changes)
                    {
                        DataChangesModel changesModel = new DataChangesModel();
                        changesModel.Property = change.Key;
                        changesModel.OldValue = change.Value.Item1 == null ? null : change.Value.Item1.ToString();
                        changesModel.NewValue = change.Value.Item2 == null ? null : change.Value.Item2.ToString();

                        data.Add(changesModel);
                    }
                }
            }
            return data;
        }

        public DashboardModel GetDashboardDetails(string GroupDescription, string UserRole)
        {
            try
            {
                int NoOfUsers = 0;
                int ForApproval = 0;
                int Approved = 0;
                int Rejected = 0;
                int UsersForApproval = 0;
                int ClientsForApproval = 0;
                int PickupForApproval = 0;
                int PartnerForApproval = 0;
                int EmailsForApproval = 0;
                int SystemForApproval = 0;
                int ReconForApproval = 0;

                if (GroupDescription.Contains("UAM/SISD") && UserRole.Contains("Maker"))
                {
                    NoOfUsers = GetUserInformation().Count();
                    ForApproval = GetUserInformation().Where(x => x.IsApproved == null).Count();
                    Approved = GetUserInformation().Where(x => x.IsApproved == true).Count();
                    Rejected = GetUserInformation().Where(x => x.IsApproved == false).Count();
                }
                else if (GroupDescription.Contains("GTB") && UserRole.Contains("Maker"))
                {
                    NoOfUsers = GetCorporateClient().Count();
                    ForApproval = GetCorporateClient().Where(x => x.IsApproved == null).Count();
                    Approved = GetCorporateClient().Where(x => x.IsApproved == true).Count();
                    Rejected = GetCorporateClient().Where(x => x.IsApproved == false).Count();
                }
                else if (UserRole.Contains("Approver"))
                {
                    UsersForApproval = GetUserInformation().Where(x => x.IsApproved == null).Count();
                    ClientsForApproval = GetCorporateClient().Where(x => x.IsApproved == null).Count();
                    PickupForApproval = GetPickupLocation().Where(x => x.IsApproved == null).Count();
                    PartnerForApproval = GetPartnerVendor().Where(x => x.IsApproved == null).Count();
                    EmailsForApproval = GetEmailType().Count();
                    SystemForApproval = GetEmailType().Count();
                    ReconForApproval = GetEmailType().Count();
                }
                else if (GroupDescription.Contains("RSC"))
                {
                    NoOfUsers = GetUserInformation().Count();
                    ForApproval = GetUserInformation().Where(x => x.IsApproved == null).Count();
                    Approved = GetUserInformation().Where(x => x.IsApproved == true).Count();
                    Rejected = GetUserInformation().Where(x => x.IsApproved == false).Count();
                }
                else // IT
                {
                    NoOfUsers = GetUserInformation().Count();
                    ForApproval = GetUserInformation().Where(x => x.IsApproved == null).Count();
                    Approved = GetUserInformation().Where(x => x.IsApproved == true).Count();
                    Rejected = GetUserInformation().Where(x => x.IsApproved == false).Count();
                }

                DashboardModel dashboard = new DashboardModel();
                dashboard.GroupDescription = GroupDescription;
                dashboard.UserRole = UserRole;
                dashboard.NoOfUsers = NoOfUsers;
                dashboard.ForApproval = ForApproval;
                dashboard.Approved = Approved;
                dashboard.Rejected = Rejected;
                dashboard.UsersForApproval = UsersForApproval;
                dashboard.ClientsForApproval = ClientsForApproval;
                dashboard.PickupForApproval = PickupForApproval;
                dashboard.PartnerForApproval = PartnerForApproval;
                dashboard.EmailsForApproval = EmailsForApproval;
                dashboard.SystemForApproval = SystemForApproval;
                dashboard.ReconForApproval = ReconForApproval;

                return dashboard;
            }
            catch (Exception ex)
            {
                return new DashboardModel();
            }
        }

        public bool SendEmail(string password, string username, string email, string type)
        {
            try
            {
                string bodyMsg = "";

                if (type == "create")
                {
                    bodyMsg = "<head>" +
                              "<style>" +
                              "body{" +
                              "font-family: calibri;" +
                              "}" +
                              "</style>" +
                              "</head>" +
                              "<body>" +
                              "<p>Hi " + username + "<br>" +
                              "<br>" +
                              "Please refer below for your temporary password to access the DPU Tellerless Portal<br>" +
                              "<font color=red>" + password + "</font><br>" +
                              "You will be asked to change your password upon initial login.<br>" +
                              "<br>" +
                              "Regards,<br>" +
                              "DPU System Administrator" +
                              "</p>" +
                              "</body>";
                }
                else if (type == "reset")
                {
                    bodyMsg = "<head>" +
                              "<style>" +
                              "body{" +
                              "font-family: calibri;" +
                              "}" +
                              "</style>" +
                              "</head>" +
                              "<body>" +
                              "<p>Hi " + username + "<br>" +
                              "<br>" +
                              "Please reset your password here: <a href='" + password + "'>Reset Password here</a><br>" +
                              "<font color=red>*Note: This is a system generated e-mail.Please do not reply.</font><br>" +
                              "<br>" +
                              "Regards,<br>" +
                              "DPU System Administrator" +
                              "</p>" +
                              "</body>";
                }
                else if (type == "forgot")
                {
                    bodyMsg = "<head>" +
                              "<style>" +
                              "body{" +
                              "font-family: calibri;" +
                              "}" +
                              "</style>" +
                              "</head>" +
                              "<body>" +
                              "<p>Hi " + username + "<br>" +
                              "<br>" +
                              "Please refer below for your newly generated password to access the DPU Tellerless Portal<br>" +
                              "<font color=red>" + password + "</font><br>" +
                              "<font color=red>*Note: This is a system generated e-mail.Please do not reply.</font><br>" +
                              "<br>" +
                              "Regards,<br>" +
                              "DPU System Administrator" +
                              "</p>" +
                              "</body>";
                }

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("arlene@yuna.somee.com");
                mailMessage.To.Add(email);
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

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void UpdateLoginAttempt(LoginAttemptModel model)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new LoginAttemptModel
                    {
                        UserId = model.UserId,
                        Attempt = model.Attempt,
                    };
                    con.Execute("sp_updateLoginAttempt", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool UpdateApprovalStatus(int Id, string tableName, bool? status, string? reason)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        Id = Id,
                        tableName = tableName,
                        status = status,
                        reason = reason,
                    };
                    con.Execute("sp_updateApproval", parameters, commandType: CommandType.StoredProcedure);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string GeneratePassword()
        {
            string password = string.Empty;
            try
            {
                var random = new Random();
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789./<>";
                password = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
            }
            catch (Exception ex)
            {
                throw;
            }
            return password;
        }

        public bool SaveUserInformation(UserModel model)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var usersInfoParam = new
                    {
                        Id = model.Id,
                        HashPassword = model.HashPassword,
                        Salt = model.Salt,
                        Username = model.Username,
                        EmployeeName = model.EmployeeName,
                        Email = model.Email,
                        MobileNumber = model.MobileNumber,
                        GroupDept = model.GroupDept,
                        UserRole = model.UserRole,
                        Active = true,
                        LoginAttempt = model.LoginAttempt,
                        IsApproved = true,
                        IsFirstLogged = model.IsFirstLogged
                    };

                    con.Execute("sp_updateUsersInformation", usersInfoParam, commandType: CommandType.StoredProcedure);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string GetColumnDetails(AuditLogsModel model)
        {
            string details = string.Empty;
            try
            {
                if (model.TableName != null)
                {
                    if (model.TableName.Contains("UsersInformation"))
                    {
                        details = "User ID = " + model.ModifiedBy.ToString();
                    }
                    else if (model.TableName.Contains("PartnerVendor"))
                    {
                        if (model.NewData is string newDataString)
                        {
                            string vendorCode = JObject.Parse(newDataString)["VendorCode"]?.ToString();
                            details = "Partner Code = " + vendorCode;
                        }
                    }
                    else if (model.TableName.Contains("CorporateClient"))
                    {
                        if (model.NewData is string newDataString)
                        {
                            string corporateName = JObject.Parse(newDataString)["CorporateName"]?.ToString();
                            details = "Corp. Name = " + corporateName;
                        }
                    }
                    else if (model.TableName.Contains("PickupLocation"))
                    {
                        if (model.NewData is string newDataString)
                        {
                            string location = JObject.Parse(newDataString)["Location"]?.ToString();
                            details = "Location ID = " + location;
                        }
                    }
                    else if (model.TableName.Contains("Accounts"))
                    {
                        details = "UserID = " + model.ModifiedBy.ToString();
                    }
                    else if (model.TableName.Contains("Contacts"))
                    {
                        details = "UserID = " + model.ModifiedBy.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return details;
        }

    }
}
