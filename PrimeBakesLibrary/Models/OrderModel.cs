namespace PrimeBakesLibrary.Models;

public class OrderModel
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public int CustomerId { get; set; }
	public DateTime DateTime { get; set; }
}

public class OrderDetailModel
{
	public int Id { get; set; }
	public int OrderId { get; set; }
	public int ItemId { get; set; }
	public int Quantity { get; set; }
}