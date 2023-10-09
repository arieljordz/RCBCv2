namespace RCBC.Models
{
    public class UpdatePasswordModel
    {
        public string Username { get; set; }
        public string OldPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
