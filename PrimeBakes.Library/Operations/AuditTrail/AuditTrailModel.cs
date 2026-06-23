namespace PrimeBakes.Library.Operations.AuditTrail;

public class AuditTrailModel
{
	public int Id { get; set; }
	public string Action { get; set; }
	public string TableName { get; set; }
	public string RecordNo { get; set; }
	public string? RecordValue { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime TransactionDateTime { get; set; } = DateTime.Now;
	public string CreatedFromPlatform { get; set; }
}

public enum AuditTrailActionTypes
{
	Insert,
	Update,
	Delete,
	Recover
}
