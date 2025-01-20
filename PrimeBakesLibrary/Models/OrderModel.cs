namespace PrimeBakesLibrary.Models;

public class OrderModel
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public int CustomerId { get; set; }
	public DateTime DateTime { get; set; }
	public bool Status { get; set; }
}

public class OrderDetailModel
{
	public int Id { get; set; }
	public int OrderId { get; set; }
	public int ItemId { get; set; }
	public int Quantity { get; set; }
}

public class ViewOrderModel
{
	public int OrderId { get; set; }
	public string UserName { get; set; }
	public string CustomerName { get; set; }
	public DateTime OrderDateTime { get; set; }
}

public class ViewOrderDetailModel
{
	public int OrderId { get; set; }
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public string ItemCode { get; set; }
	public int CategoryId { get; set; }
	public string CategoryName { get; set; }
	public string CategoryCode { get; set; }
	public int Quantity { get; set; }
}