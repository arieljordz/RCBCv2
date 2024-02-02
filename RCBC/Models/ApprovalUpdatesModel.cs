namespace RCBC.Models
{
    public class ApprovalUpdatesModel
    {
        public int Id { get; set; }
        public string JsonData { get; set; }
        public int TableId { get; set; }
        public string TableName { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime? DateModified { get; set; }
    }
}
