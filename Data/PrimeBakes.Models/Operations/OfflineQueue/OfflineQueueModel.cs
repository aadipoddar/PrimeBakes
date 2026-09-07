namespace PrimeBakes.Models.Operations.OfflineQueue;

public class OfflineQueueModel
{
	public int Id { get; set; }
	public string TableName { get; set; }
	public string TransactionNo { get; set; }
	public string Payload { get; set; }
}