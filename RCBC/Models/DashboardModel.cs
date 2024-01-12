namespace RCBC.Models
{
    public class DashboardModel
    {
        public string? GroupDescription { get; set; }
        public string? UserRole { get; set; }
        public int NoOfUsers { get; set; }
        public int ForApproval { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }

        public int UsersForApproval { get; set; }
        public int ClientsForApproval { get; set; }
        public int PickupForApproval { get; set; }
        public int PartnerForApproval { get; set; }
        public int EmailsForApproval { get; set; }
        public int SystemForApproval { get; set; }
        public int ReconForApproval { get; set; }
       
    }
}
