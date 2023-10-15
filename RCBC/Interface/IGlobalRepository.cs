using RCBC.Models;

namespace RCBC.Interface
{
    public interface IGlobalRepository
    {
        List<ModuleModel> GetModules(string UserRole);

        List<SubModuleModel> GetSubModules(string UserRole);

        List<ChildModuleModel> GetChildModules(string UserRole);

        List<AccessModuleModel> GetAccessModules();

        List<AccessModuleModel> GetAccessPerRole(string UserRole);

        List<AccessModuleModel> GetActiveAccess(string UserRole);

        bool IsStrongPassword(string password);

        List<ModuleModel> GetAllModules();

        List<SubModuleModel> GetAllSubModules();

        List<ChildModuleModel> GetAllChildModules();

        List<UserRoleModel> GetUserRoles();

        List<DepartmentModel> GetDepartments();
    }
}
