

namespace ILLVentApp.Domain.Models
{
	public class Payment
	{
		public int PaymentId { get; set; }
		public decimal Amount { get; set; }
		public string PaymentMethod { get; set; } // "Credit Card", "Debit Card", "Cash"
		public DateTime PaymentDate { get; set; }

		// Foreign keys
		public string UserId { get; set; }
		public int? DealId { get; set; } // Optional (if linked to a deal)

		// Navigation properties
		public User User { get; set; }
		public Deals Deal { get; set; }
	}
}
