namespace RCBC.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }

        public string Password { get; set; }
        public string MobileNumber { get; set; }
        public string GroupDept { get; set; }
        public string UserRole { get; set; }
    }
}
