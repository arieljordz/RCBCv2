namespace RCBC.Models
{
    public class SubModuleModel
    {
        public string Module { get; set; }
        public string SubModule { get; set; }
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public int SubModuleId { get; set; }
        public string ChildModule { get; set; }
        public string ModuleIcon { get; set; }
        public string SubModuleIcon { get; set; }
        public string Link { get; set; }
        public string DivId { get; set; }
        public int ModuleOrder { get; set; }
        public int SubModuleOrder { get; set; }

        public int UserId { get; set; }
        public int UserRoleId { get; set; }
        public string UserRole { get; set; }
    }
}
