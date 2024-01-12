namespace RCBC.Models
{
    public class DepartmentModel
    {
        public int Id { get; set; }
        public string? GroupDept { get; set; }
        public DateTime? DateCreated { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateApproved { get; set; }
        public int ApprovedBy { get; set; }
    }
}

