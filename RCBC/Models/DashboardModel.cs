namespace RCBC.Models
{
    public class DashboardModel
    {
        public string? GroupDescription { get; set; }
        public int NoOfUsers { get; set; }
        public int ForApproval { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
       
    }
}
