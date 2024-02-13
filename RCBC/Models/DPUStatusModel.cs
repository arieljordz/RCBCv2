namespace RCBC.Models
{
    public class DPUStatusModel
    {
        public int? Id { get; set; }
        public int? No { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? LocationCode { get; set; }
        public string? LocationName { get; set; }
        public DateTime? TransactionDate { get; set; }
        public DateTime? TransactionTime { get; set; }
        public string? CardNumber { get; set; }
        public string? AccountNumber { get; set; }
        public string? BeneficiaryName { get; set; }
        public decimal? Amount { get; set; }
        public string? CreditDescription { get; set; }
        public string? ExternalReference { get; set; }
        public string? Status { get; set; }
    }

}
