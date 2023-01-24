namespace CommunicationAppApi.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public double Amount { get; set; }
        public int UserId { get; set; }
        public string ReceiptUrl { get; set; }
        public string Description { get; set; }
        public string Currency { get; set; }
        public bool IsPaid { get; set; }
    }
}
