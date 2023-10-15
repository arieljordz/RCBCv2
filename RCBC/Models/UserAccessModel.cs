namespace RCBC.Models
{
    public class UserAccessModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public int ModuleId { get; set; }
        public int Active { get; set; }
    }
}
