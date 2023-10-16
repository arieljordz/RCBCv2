using RCBC.Models;

namespace RCBC.Interface
{
    public interface IGlobalRepository
    {
        List<ModuleModel> GetModulesByRole(string UserRole);

        List<SubModuleModel> GetSubModulesByRole(string UserRole);

        List<ChildModuleModel> GetChildModulesRole(string UserRole);

        List<AccessModuleModel> GetAccessModules();

        List<AccessModuleModel> GetAccessByRole(string UserRole);

        List<AccessModuleModel> GetActiveAccess(string UserRole);

        bool IsStrongPassword(string password);

        List<ModuleModel> GetAllModules();

        List<SubModuleModel> GetAllSubModules();

        List<ChildModuleModel> GetAllChildModules();

        List<UserRoleModel> GetUserRoles();

        List<DepartmentModel> GetDepartments();
    }
}
