namespace RCBC.Models
{
    public class DPUStatusModel
    {
        public int? Id { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? MachineID { get; set; }
        public string? BeneficiaryName { get; set; }
        public string? AccountNumber { get; set; }
        public string? Status { get; set; }
    }

}
