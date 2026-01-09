namespace PrimeBakesLibrary.Models.Sales.Masters;

public class PaymentModeModel
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class PaymentItem
{
    public int Id { get; set; }
    public string Method { get; set; }
    public decimal Amount { get; set; }
}