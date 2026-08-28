namespace PrimeBakes.Models.Operations.WebPush;

public class WebPushSubscriptionModel
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public string Endpoint { get; set; }
	public string P256dh { get; set; }
	public string Auth { get; set; }
	public DateTime TransactionDateTime { get; set; } = DateTime.Now;
}
