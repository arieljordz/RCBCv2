namespace RCBC.Models
{
    public class AccountModel
    {
        public int Id { get; set; }    
        public int CorporateClientId { get; set; }    
        public int LocationId { get; set; }    
        public string AccountNumber { get; set; }    
        public string AccountName { get; set; }    
        public string Currency { get; set; }    
        public string AccountType { get; set; }    
        public int CurrencyId { get; set; }    
        public int AccountTypeId { get; set; }    
    }
}
