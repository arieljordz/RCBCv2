namespace RCBC.Models
{
    public class CorporateClientModel
    {
        public int Id { get; set; }    
        public string? CorporateGroup { get; set; }    
        public string? PartnerCode { get; set; }    
        public string? CorporateName { get; set; }    
        public string? ContactPerson { get; set; }    
        public string? Email { get; set; }    
        public string? MobileNumber { get; set; }    
        public bool GlobalAccount { get; set; }    
        public bool Active { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime? DateCreated { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateApproved { get; set; }
        public int ApprovedBy { get; set; }

        public int AccountId { get; set; }
        public int CorporateClientId { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public int CurrencyId { get; set; }
        public int AccountTypeId { get; set; }

        public int ContactId { get; set; }
        public bool ForApproval { get; set; }
    }
}
