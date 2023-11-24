namespace RCBC.Models
{
    public class AccessModuleModel
    {
        public string Module { get; set; }
        public string SubModule { get; set; }
        public string ChildModule { get; set; }
        public string Link { get; set; }
        public int ModuleId { get; set; }
        public int SubModuleId { get; set; }
        public int UserId { get; set; }
        public int ModuleOrder { get; set; }
        public bool? IsActive { get; set;}
    }
}
