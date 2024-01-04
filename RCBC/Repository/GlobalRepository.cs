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

        public List<ModuleModel> GetModulesByUserId(int UserId)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        UserId = UserId,
                    };
                    List<ModuleModel> data = con.Query<ModuleModel>("sp_getModulesByUserId", parameters, commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ModuleModel>();
            }
        }

        public List<SubModuleModel> GetSubModulesByUserId(int UserId)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        UserId = UserId,
                    };
                    List<SubModuleModel> data = con.Query<SubModuleModel>("sp_getSubModulesByUserId", parameters, commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<SubModuleModel>();
            }
        }

        public List<ChildModuleModel> GetChildModulesByUserId(int UserId)
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    var parameters = new
                    {
                        UserId = UserId,
                    };
                    List<ChildModuleModel> data = con.Query<ChildModuleModel>("sp_getChildModulesByUserId", parameters, commandType: CommandType.StoredProcedure).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ChildModuleModel>();
            }
        }

        public List<AccessModuleModel> GetUserAccess()
        {
            try
            {
                using (IDbConnection con = new SqlConnection(GetConnectionString()))
                {
                    List<AccessModuleModel> data = con.Query<AccessModuleModel>("sp_getUserAccess", commandType: CommandType.StoredProcedure).ToList();
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

            var UserAccess = GetUserAccess();
            var AllModules = GetModulesAndSubModules();

            foreach (var module in AllModules)
            {
                if (UserId != 0)
                {
                    var IsActive = UserAccess.Where(x => x.SubModuleId == module.SubModuleId && x.UserId == UserId).FirstOrDefault();

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
                    access.SubModuleId = module.SubModuleId;
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
                    access.SubModuleId = module.SubModuleId;
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
                        SystemName = model.SystemName,
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

        public DashboardModel GetDashboardDetails(string GroupDescription)
        {
            try
            {
                int NoOfUsers = 0;
                int ForApproval = 0;
                int Approved = 0;
                int Rejected = 0;

                if (GroupDescription.Contains("UAM/SISD"))
                {
                    NoOfUsers = GetUserInformation().Count();
                    ForApproval = GetUserInformation().Where(x => x.IsApproved == null).Count();
                    Approved = GetUserInformation().Where(x => x.IsApproved == true).Count();
                    Rejected = GetUserInformation().Where(x => x.IsApproved == false).Count();
                }
                else if (GroupDescription.Contains("GTB"))
                {
                    NoOfUsers = GetUserInformation().Count();
                    ForApproval = GetUserInformation().Where(x => x.IsApproved == null).Count();
                    Approved = GetUserInformation().Where(x => x.IsApproved == true).Count();
                    Rejected = GetUserInformation().Where(x => x.IsApproved == false).Count();
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
                dashboard.NoOfUsers = NoOfUsers;
                dashboard.ForApproval = ForApproval;
                dashboard.Approved = Approved;
                dashboard.Rejected = Rejected;

                return dashboard;
            }
            catch (Exception ex)
            {
                return new DashboardModel();
            }
        }

    }
}
