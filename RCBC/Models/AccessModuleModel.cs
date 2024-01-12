namespace RCBC.Models
{
    public class AccessModuleModel
    {
        public int ModuleId { get; set; }
        public string? Module { get; set; }
        public string? ModuleIcon { get; set; }

        public int SubModuleId { get; set; }
        public string? SubModule { get; set; }
        public string? SubModuleIcon { get; set; }
        public string? SubModuleLink { get; set; }
        public string? SubModuleDivId { get; set; }

        public int ChildModuleId { get; set; }
        public string? ChildModule { get; set; }
        public string? ChildModuleIcon { get; set; }
        public string? ChildModuleLink { get; set; }
        public string? ChildModuleDivId { get; set; }

        public int ModuleOrder { get; set; }
        public int SubModuleOrder { get; set; }
        public int ChildModuleOrder { get; set; }

        public int UserId { get; set; }
        public int RoleId { get; set; }

        public string? Link { get; set; }
        public bool? IsActive { get; set;}
    }
}
