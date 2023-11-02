namespace RCBC.Models
{
    public class PartnerVendorModel
    {
        public int Id { get; set; }    
        public string VendorName { get; set; }    
        public string VendorCode { get; set; }    
        public string AssignedGL { get; set; }    
        public string Email { get; set; }    
        public bool Active { get; set; }
        public bool IsApproved { get; set; }
    }
}
