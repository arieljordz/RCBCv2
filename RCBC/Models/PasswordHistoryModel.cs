namespace RCBC.Models
{
    public class PasswordHistoryModel
    {
        public int Id { get; set; }    
        public int UserId { get; set; }    
        public string? HashPassword { get; set; }    
        public string? Salt { get; set; }    
        public DateTime? DateUpdated { get; set; }    
    }
}
