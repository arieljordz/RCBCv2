using RCBC.Models;

namespace RCBC.Interface
{
    public interface IGlobalRepository
    {
        List<ModuleModel> GetModules(string UserRole);

        List<SubModuleModel> GetSubModules(string UserRole);

        List<ChildModuleModel> GetChildModules(string UserRole);

        List<AccessModuleModel> GetAccessModules();

        bool IsStrongPassword(string password);

        List<UserRoleModel> GetUserRoles();

        List<DepartmentModel> GetDepartments();
    }
}
