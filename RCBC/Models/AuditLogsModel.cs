namespace RCBC.Models
{
    public class AuditLogsModel
    {
        public int Id { get; set; }
        public string? SystemName { get; set; }
        public string? Module { get; set; }
        public string? SubModule { get; set; }
        public string? ChildModule { get; set; }
        public string? TableName { get; set; }
        public int TableId { get; set; }
        public string? Action { get; set; }
        public string? GroupDept { get; set; }
        public string? UserRole { get; set; }
        public string? PreviousData { get; set; }

        public string? NewData { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime DateModified { get; set; }
        public string? IP { get; set; }
        public string? EmployeeName { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
