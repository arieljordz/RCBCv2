using RCBC.Models;

namespace RCBC.Interface
{
    public interface IGlobalRepository
    {
      
        List<AccessModuleModel> GetModulesByUserId(int UserId);

        List<AccessModuleModel> GetSubModulesByUserId(int UserId);

        List<AccessModuleModel> GetChildModulesByUserId(int UserId);

        List<AccessModuleModel> GetModulesAndSubModules();

        List<AccessModuleModel> GetUserAccessById(int UserId);

        public List<UserModel> GetUserInformation();
       
        public List<UserRoleModel> GetUserRole();
       
        public List<AccessModuleModel> GetUserAccessModules();
       
        public List<SubModuleModel> GetSubModule();
       
        public List<PickupLocationModel> GetPickupLocation();
       
        public List<PartnerVendorModel> GetPartnerVendor();
       
        public List<ModuleModel> GetModule();
       
        public List<EmailTypeModel> GetEmailType();
       
        public List<DepartmentModel> GetDepartment();
       
        public List<CorporateClientModel> GetCorporateClient();
       
        public List<ContactModel> GetContacts();
       
        public List<ChildModuleModel> GetChildModule();
       
        public List<AuditLogsModel> GetAuditLogs();
        
        public List<AccountModel> GetAccounts();

        bool IsStrongPassword(string password);
        
        public bool CheckUserStatus(int UserId);

        public string GetLocalIPAddress();

        public bool SaveAuditLogs(AuditLogsModel model);

        public List<DataChangesModel> GetChangesDetails(int Id, string TableName);

        public DashboardModel GetDashboardDetails(string GroupDescription, string UserRole);

        public bool SendEmail(string password, string username, string email);

        public void UpdateLoginAttempt(LoginAttemptModel model);
    }
}
