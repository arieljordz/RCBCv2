using RCBC.Interface;
using RCBC.Models;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

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
            using IDbConnection con = new SqlConnection(GetConnectionString());

            List<ModuleModel> modules = new List<ModuleModel>();

            var qry = @"SELECT
                        a.Id as UserRoleId,
                        a.UserRole,
                        c.Id as ModuleId,
                        c.Description as Module,
						c.Icon as ModuleIcon
                    FROM [RCBC].[dbo].[UserRole] a
                    INNER JOIN [RCBC].[dbo].[UserAccessModules] b ON a.Id = b.RoleId
                    INNER JOIN [RCBC].[dbo].[Module] c ON b.ModuleId = c.Id
					WHERE b.UserId = @UserId AND b.Active = 1
					GROUP BY a.Id, a.UserRole, c.Id, c.Description,c.Icon
					ORDER BY c.Id";

            modules = con.Query<ModuleModel>(qry, new { UserId }).ToList();

            return modules;
        }

        public List<SubModuleModel> GetSubModulesByUserId(int UserId)
        {
            using IDbConnection con = new SqlConnection(GetConnectionString());

            List<SubModuleModel> modules = new List<SubModuleModel>();

            var qry = @"SELECT
                        a.Id as UserRoleId,
                        a.UserRole,
                        c.Id as ModuleId,
                        c.Description as Module,
						c.Icon as ModuleIcon,
						b.SubModuleId,
						d.SubModule,
						d.Icon as SubModuleIcon,
						d.Link,
						d.DivId
                    FROM [RCBC].[dbo].[UserRole] a
                    INNER JOIN [RCBC].[dbo].[UserAccessModules] b ON a.Id = b.RoleId
                    INNER JOIN [RCBC].[dbo].[Module] c ON b.ModuleId = c.Id
					INNER JOIN [RCBC].[dbo].[SubModule] d ON b.SubModuleId = d.Id
					WHERE b.UserId = @UserId AND b.Active = 1
					ORDER BY c.Id";

            modules = con.Query<SubModuleModel>(qry, new { UserId }).ToList();
            //modules = con.Query<SubModuleModel>(qry).ToList();

            return modules;
        }

        public List<ChildModuleModel> GetChildModulesByUserId(int UserId)
        {
            using IDbConnection con = new SqlConnection(GetConnectionString());

            List<ChildModuleModel> modules = new List<ChildModuleModel>();

            var qry = @"SELECT c.[Id] as ChildModuleId
                            ,c.[SubModuleId]
                            ,c.[ChildModule]
                            ,c.[Link]
                            ,c.[Icon] as ChildModuleIcon
                            ,c.[DivId]
                            ,c.[Sequence] as ChildModuleOrder
                            ,c.[Active]
					FROM [RCBC].[dbo].[UserRole] a
                    INNER JOIN [RCBC].[dbo].[UserAccessModules] b ON a.Id = b.RoleId
					INNER JOIN [RCBC].[dbo].[ChildModule] c ON b.SubModuleId = c.SubModuleId
					WHERE b.UserId = @UserId AND b.Active = 1
					ORDER BY c.Sequence";

            modules = con.Query<ChildModuleModel>(qry, new { UserId }).ToList();
            //modules = con.Query<ChildModuleModel>(qry).ToList();

            return modules;
        }

        public List<AccessModuleModel> GetUserAccessModules()
        {
            using IDbConnection con = new SqlConnection(GetConnectionString());

            List<AccessModuleModel> modules = new List<AccessModuleModel>();

            var qry = @"SELECT 
                            a.Description as Module, 
                            b.SubModule,
                            b.Id as SubModuleId,
                            c.UserId,
                            a.Sequence as ModuleOrder, 
                            b.Link,
                            c.Active as IsActive
                        FROM [RCBC].[dbo].[Module] a
                        INNER JOIN [RCBC].[dbo].[SubModule] b
                        ON a.Id = b.ModuleId
				        INNER JOIN [RCBC].[dbo].[UserAccessModules] c
				        ON b.Id = c.SubModuleId
				        INNER JOIN [RCBC].[dbo].[UserRole] d
				        ON c.RoleId = d.Id
                        ORDER BY a.Sequence";

            modules = con.Query<AccessModuleModel>(qry).ToList();

            return modules;
        }

        public List<AccessModuleModel> GetAccessModules()
        {
            using IDbConnection con = new SqlConnection(GetConnectionString());

            List<AccessModuleModel> modules = new List<AccessModuleModel>();

            var qry = @"SELECT 
                            a.Description as Module, 
                            b.SubModule,
                            b.Id as SubModuleId,
                            a.Sequence as ModuleOrder, 
                            b.Link
                        FROM [RCBC].[dbo].[Module] a
                        INNER JOIN [RCBC].[dbo].[SubModule] b
                        ON a.Id = b.ModuleId
                        ORDER BY a.Sequence";

            modules = con.Query<AccessModuleModel>(qry).ToList();

            return modules;
        }

        public List<AccessModuleModel> GetUserAccessById(int UserId)
        {
            List<AccessModuleModel> modules = new List<AccessModuleModel>();

            var UserAccessModules = GetUserAccessModules();
            var AllModules = GetAccessModules();

            foreach (var module in AllModules)
            {
                if (UserId != 0)
                {
                    var IsActive = UserAccessModules.Where(x => x.SubModuleId == module.SubModuleId && x.UserId == UserId).FirstOrDefault();

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

        public List<ModuleModel> GetAllModules()
        {
            List<ModuleModel> data = new List<ModuleModel>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                string qry = @"SELECT Id as ModuleId, Description as Module, Icon as ModuleIcon, Sequence as ModuleOrder, Active FROM [RCBC].[dbo].[Module]";
                data = con.Query<ModuleModel>(qry).ToList();

                con.Close();
            }
            return data;
        }

        public List<SubModuleModel> GetAllSubModules()
        {
            List<SubModuleModel> data = new List<SubModuleModel>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                string qry = @"SELECT Id as SubModuleId, SubModule, ModuleId, Icon as SubModuleIcon, Link, DivId, Sequence as SubModuleOrder, Active FROM [RCBC].[dbo].[SubModule]";
                data = con.Query<SubModuleModel>(qry).ToList();

                con.Close();
            }
            return data;
        }

        public List<ChildModuleModel> GetAllChildModules()
        {
            List<ChildModuleModel> data = new List<ChildModuleModel>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                string qry = @"SELECT Id as ChildModuleId, ChildModule, SubModuleId, Icon as ChildModuleIcon, Link, DivId, Sequence as ChildModuleOrder, Active FROM [RCBC].[dbo].[ChildModule]";
                data = con.Query<ChildModuleModel>(qry).ToList();

                con.Close();
            }
            return data;
        }

        public List<UserRoleModel> GetUserRoles()
        {
            List<UserRoleModel> data = new List<UserRoleModel>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                string qry = @"SELECT * FROM [RCBC].[dbo].[UserRole]";
                data = con.Query<UserRoleModel>(qry).ToList();

                con.Close();
            }
            return data;
        }

        public List<UserRoleModel> GetRoleIdByRole(string UserRole)
        {
            List<UserRoleModel> data = new List<UserRoleModel>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                string qry = @"SELECT * FROM [RCBC].[dbo].[UserRole] WHERE UserRole LIKE '%' + @UserRole + '%'";
                data = con.Query<UserRoleModel>(qry, new { UserRole = UserRole }).ToList();

                con.Close();
            }
            return data;
        }

        public List<DepartmentModel> GetDepartments()
        {
            List<DepartmentModel> data = new List<DepartmentModel>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                string qry = @"SELECT * FROM [RCBC].[dbo].[Department]";
                data = con.Query<DepartmentModel>(qry).ToList();

                con.Close();
            }
            return data;
        }

    }
}
