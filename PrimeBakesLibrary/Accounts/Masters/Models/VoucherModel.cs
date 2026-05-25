
namespace PrimeBakesLibrary.Accounts.Masters.Models;

public class VoucherModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string? Remarks { get; set; }
    public bool Status { get; set; }
}
