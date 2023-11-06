using RCBC.Models;

namespace RCBC.Interface
{
    public interface IGlobalRepository
    {
        List<ModuleModel> GetModulesByUserId(int UserId);

        List<SubModuleModel> GetSubModulesByUserId(int UserId);

        List<ChildModuleModel> GetChildModulesByUserId(int UserId);

        List<AccessModuleModel> GetAccessModules();

        List<AccessModuleModel> GetUserAccessModules();

        List<AccessModuleModel> GetUserAccessById(int UserId);

        bool IsStrongPassword(string password);

        List<ModuleModel> GetAllModules();

        List<SubModuleModel> GetAllSubModules();

        List<ChildModuleModel> GetAllChildModules();

        List<UserRoleModel> GetUserRoles();

        List<DepartmentModel> GetDepartments();

        List<EmailTypeModel> GetEmailTypes();
    }
}
