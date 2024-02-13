namespace RCBC.Models
{
    public class TransmittalDetailModel
    {
        public int Id { get; set; }    
        public int TransmittalId { get; set; }    
        public string? LocationCode { get; set; }    
        public string? TransRefNo { get; set; }    
        public decimal? Amount { get; set; }    
        public int Denom1000Count { get; set; }    
        public int Denom500Count { get; set; }    
        public int Denom200Count { get; set; }    
        public int Denom100Count { get; set; }    
        public int Denom50Count { get; set; }    
        public int Denom20Count { get; set; }    
        public int Denom10Count { get; set; }    
        public int Denom5Count { get; set; }
        public decimal? DenomOther { get; set; }
        public decimal? Denom1000Amount { get; set; }
        public decimal? Denom500Amount { get; set; }
        public decimal? Denom200Amount { get; set; }
        public decimal? Denom100Amount { get; set; }
        public decimal? Denom50Amount { get; set; }
        public decimal? Denom20Amount { get; set; }
        public decimal? Denom10Amount { get; set; }
        public decimal? Denom5Amount { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool? Processed { get; set; }
        public bool? Adjusted { get; set; }
        public bool? WithDiscrepancy { get; set; }
        public string? TranCode { get; set; }
        public int Status { get; set; }
    }
}
