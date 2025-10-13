using Digital_Mall_API.Models.Entities.User___Authentication;

namespace Digital_Mall_API.Models.Entities.Orders___Shopping
{
    public class RefundTransaction
    {
        public int Id { get; set; }
        public int RefundRequestId { get; set; }
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string Status { get; set; }

        public virtual RefundRequest RefundRequest { get; set; }
        public virtual Customer Customer { get; set; }
    }
}