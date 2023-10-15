namespace RCBC.Models
{
    public class ChildModuleModel
    {
        public int ChildModuleId { get; set; }
        public int SubModuleId { get; set; }
        public string ChildModule { get; set; }
        public string Link { get; set; }
        public string ChildModuleIcon { get; set; }
        public string DivId { get; set; }
        public int ChildModuleOrder { get; set; }
        public bool Active { get; set; }
    }
}
