namespace RCBC.Models
{
    public class UserRoleModel
    {
        public int Id { get; set; }
        public string UserRole { get; set; }
        public DateTime? DateCreated { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateApproved { get; set; }
        public int ApprovedBy { get; set; }
    }
}
