namespace RCBC.Models
{
    public class CompositeModel
    {
        public CorporateClientModel CorporateClient { get; set; }
        public AccountModel Account { get; set; }
        public ContactModel Contact { get; set; }
    }
}
