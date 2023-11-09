namespace RCBC.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Salt { get; set; }
        public string HashPassword { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }

        public string Password { get; set; }
        public string MobileNumber { get; set; }
        public string GroupDept { get; set; }
        public string UserRole { get; set; }
        public bool UserStatus { get; set; }
        public int LoginAttempt { get; set; }
        public bool Deactivated { get; set; }
        public string ModuleIds { get; set; }
    }
}
