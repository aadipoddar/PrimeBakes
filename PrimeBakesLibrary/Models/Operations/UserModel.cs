namespace PrimeBakesLibrary.Models.Operations;

public class UserModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int Passcode { get; set; }
	public int LocationId { get; set; }
	public bool Accounts { get; set; }
	public bool Inventory { get; set; }
	public bool Store { get; set; }
	public bool Restaurant { get; set; }
	public bool Reports { get; set; }
	public bool Admin { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public enum UserRoles
{
	Accounts,
	Inventory,
	Store,
	Restaurant,
	Reports,
	Admin,
}