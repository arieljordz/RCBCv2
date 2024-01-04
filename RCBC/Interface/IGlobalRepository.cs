using RCBC.Models;

namespace RCBC.Interface
{
    public interface IGlobalRepository
    {
      
        List<ModuleModel> GetModulesByUserId(int UserId);

        List<SubModuleModel> GetSubModulesByUserId(int UserId);

        List<ChildModuleModel> GetChildModulesByUserId(int UserId);

        List<AccessModuleModel> GetModulesAndSubModules();

        List<AccessModuleModel> GetUserAccess();

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

        public DashboardModel GetDashboardDetails(string GroupDescription);
    }
}
